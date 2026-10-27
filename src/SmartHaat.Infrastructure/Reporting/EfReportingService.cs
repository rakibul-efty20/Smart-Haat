using Microsoft.EntityFrameworkCore;
using SmartHaat.Application.Reporting;
using SmartHaat.Domain.Orders;
using SmartHaat.Infrastructure.Persistence;

namespace SmartHaat.Infrastructure.Reporting;

public sealed class EfReportingService(SmartHaatDbContext db) : IReportingService
{
    public async Task<SalesReport> GetSalesAsync(DateTime fromUtc, DateTime toUtc, int topCount, CancellationToken ct)
    {
        var q = db.Orders.AsNoTracking().Where(o => o.CreatedAtUtc >= fromUtc && o.CreatedAtUtc < toUtc && o.PaymentStatus == PaymentStatus.Paid);
        var paidCount = await q.CountAsync(ct);
        var total = await q.SumAsync(o => (decimal?)o.Total, ct) ?? 0;
        var discounts = await q.SumAsync(o => (decimal?)o.DiscountTotal, ct) ?? 0;
        var delivery = await q.SumAsync(o => (decimal?)o.DeliveryFee, ct) ?? 0;
        var channelRows = await q.GroupBy(o => o.Channel).Select(g => new { Channel = g.Key, Count = g.Count(), Sales = g.Sum(o => o.Total) }).ToListAsync(ct);
        var byChannel = channelRows.Select(x => new ChannelSales(x.Channel.ToString(), x.Count, x.Sales)).ToList();
        var dayRows = await q.GroupBy(o => o.CreatedAtUtc.Date).OrderBy(g => g.Key)
            .Select(g => new { Day = g.Key, Count = g.Count(), Sales = g.Sum(o => o.Total) }).ToListAsync(ct);
        var byDay = dayRows.Select(x => new DailySales(DateOnly.FromDateTime(x.Day), x.Count, x.Sales)).ToList();
        var topProducts = await (from item in db.OrderItems.AsNoTracking()
                                 join order in q on item.OrderId equals order.Id
                                 group item by new { item.SkuSnapshot, item.NameSnapshot } into g
                                 orderby g.Sum(x => x.Quantity) descending
                                 select new TopProduct(g.Key.SkuSnapshot, g.Key.NameSnapshot, g.Sum(x => x.Quantity), g.Sum(x => x.LineTotal)))
            .Take(topCount).ToListAsync(ct);
        var byStaff = await q.Where(o => o.SalesmanUserId != null).GroupBy(o => o.SalesmanUserId!)
            .Select(g => new StaffSales(g.Key, g.Count(), g.Sum(o => o.Total))).OrderByDescending(x => x.SalesTotal).ToListAsync(ct);
        return new(fromUtc, toUtc, paidCount, total, discounts, delivery, byChannel, byDay, topProducts, byStaff);
    }
}
