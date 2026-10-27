namespace SmartHaat.Application.Reporting;

public sealed record ChannelSales(string Channel, int OrderCount, decimal SalesTotal);
public sealed record DailySales(DateOnly Date, int OrderCount, decimal SalesTotal);
public sealed record TopProduct(string Sku, string Name, decimal QuantitySold, decimal SalesTotal);
public sealed record StaffSales(string StaffUserId, int OrderCount, decimal SalesTotal);
public sealed record SalesReport(DateTime FromUtc, DateTime ToUtc, int PaidOrderCount, decimal SalesTotal,
    decimal DiscountTotal, decimal DeliveryTotal, IReadOnlyList<ChannelSales> ByChannel, IReadOnlyList<DailySales> ByDay,
    IReadOnlyList<TopProduct> TopProducts, IReadOnlyList<StaffSales> ByStaff);

public interface IReportingService
{
    Task<SalesReport> GetSalesAsync(DateTime fromUtc, DateTime toUtc, int topCount, CancellationToken ct);
}
