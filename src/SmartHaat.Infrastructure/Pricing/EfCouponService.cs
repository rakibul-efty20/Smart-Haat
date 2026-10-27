using Microsoft.EntityFrameworkCore;
using SmartHaat.Application.Catalog;
using SmartHaat.Application.Pricing;
using SmartHaat.Domain.Pricing;
using SmartHaat.Infrastructure.Persistence;

namespace SmartHaat.Infrastructure.Pricing;

public sealed class EfCouponService(SmartHaatDbContext db) : ICouponService
{
    public async Task<CouponQuote> QuoteAsync(string code, string? customerUserId, decimal subtotal, CancellationToken ct)
    {
        var coupon = await db.Coupons.AsNoTracking().SingleOrDefaultAsync(c => c.Code == code.Trim().ToUpperInvariant(), ct);
        await EnsureEligibleAsync(coupon, customerUserId, subtotal, ct);
        var discount = coupon!.CalculateDiscount(subtotal);
        if (discount <= 0) throw new CatalogValidationException("Coupon is not valid for this cart.");
        return new CouponQuote(coupon.Code, discount, decimal.Round(subtotal - discount, 2, MidpointRounding.AwayFromZero));
    }

    public async Task ApplyAsync(string code, string? customerUserId, decimal subtotal, Guid orderId, CancellationToken ct)
    {
        var coupon = await db.Coupons.SingleOrDefaultAsync(c => c.Code == code.Trim().ToUpperInvariant(), ct);
        await EnsureEligibleAsync(coupon, customerUserId, subtotal, ct);
        var discount = coupon!.CalculateDiscount(subtotal);
        if (discount <= 0) throw new CatalogValidationException("Coupon is not valid for this cart.");
        db.CouponRedemptions.Add(new CouponRedemption(coupon.Id, orderId, customerUserId, discount));
    }

    public async Task<IReadOnlyList<CouponView>> GetAsync(CancellationToken ct) =>
        await db.Coupons.AsNoTracking().OrderBy(c => c.Code).Select(c => new CouponView(c.Id, c.Code, c.DiscountType, c.Value,
            c.MinimumOrderAmount, c.MaximumDiscount, c.StartsAtUtc, c.EndsAtUtc, c.UsageLimit, c.PerCustomerLimit, c.IsActive)).ToListAsync(ct);

    public async Task<CouponView> CreateAsync(CouponRequest r, CancellationToken ct)
    {
        var coupon = new Coupon(r.Code, r.DiscountType, r.Value, r.MinimumOrderAmount, r.MaximumDiscount, AsUtc(r.StartsAtUtc), AsUtc(r.EndsAtUtc), r.UsageLimit, r.PerCustomerLimit);
        db.Coupons.Add(coupon); await SaveAsync(ct); return ToView(coupon);
    }

    public async Task<CouponView?> UpdateAsync(Guid id, CouponRequest r, CancellationToken ct)
    {
        var coupon = await db.Coupons.FindAsync([id], ct); if (coupon is null) return null;
        coupon.Update(r.Code, r.DiscountType, r.Value, r.MinimumOrderAmount, r.MaximumDiscount, AsUtc(r.StartsAtUtc), AsUtc(r.EndsAtUtc), r.UsageLimit, r.PerCustomerLimit);
        await SaveAsync(ct); return ToView(coupon);
    }

    public async Task<bool> SetActiveAsync(Guid id, bool active, CancellationToken ct)
    { var coupon = await db.Coupons.FindAsync([id], ct); if (coupon is null) return false; coupon.SetActive(active); await db.SaveChangesAsync(ct); return true; }

    private async Task EnsureEligibleAsync(Coupon? coupon, string? customerUserId, decimal subtotal, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (coupon is null || !coupon.IsActive || coupon.StartsAtUtc > now || coupon.EndsAtUtc <= now || subtotal < coupon.MinimumOrderAmount)
            throw new CatalogValidationException("Coupon is not valid for this cart.");
        var totalRedemptions = await db.CouponRedemptions.CountAsync(r => r.CouponId == coupon.Id &&
            db.Orders.Any(o => o.Id == r.OrderId && o.Status != SmartHaat.Domain.Orders.OrderStatus.Cancelled), ct);
        if (coupon.UsageLimit.HasValue && totalRedemptions >= coupon.UsageLimit.Value) throw new CatalogValidationException("Coupon is not valid for this cart.");
        if (customerUserId is not null && coupon.PerCustomerLimit.HasValue &&
            await db.CouponRedemptions.CountAsync(r => r.CouponId == coupon.Id && r.CustomerUserId == customerUserId &&
                db.Orders.Any(o => o.Id == r.OrderId && o.Status != SmartHaat.Domain.Orders.OrderStatus.Cancelled), ct) >= coupon.PerCustomerLimit.Value)
            throw new CatalogValidationException("Coupon is not valid for this cart.");
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true || e.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
        { throw new CatalogConflictException("Coupon code is already in use."); }
    }
    private static CouponView ToView(Coupon c) => new(c.Id, c.Code, c.DiscountType, c.Value, c.MinimumOrderAmount, c.MaximumDiscount, c.StartsAtUtc, c.EndsAtUtc, c.UsageLimit, c.PerCustomerLimit, c.IsActive);
    private static DateTime AsUtc(DateTime value) => value.Kind switch { DateTimeKind.Utc => value, DateTimeKind.Local => value.ToUniversalTime(), _ => DateTime.SpecifyKind(value, DateTimeKind.Utc) };
}
