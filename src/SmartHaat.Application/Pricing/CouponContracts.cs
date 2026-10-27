using System.ComponentModel.DataAnnotations;
using SmartHaat.Domain.Pricing;

namespace SmartHaat.Application.Pricing;

public sealed record CouponRequest([Required, StringLength(40)] string Code, CouponDiscountType DiscountType,
    [Range(typeof(decimal), "0.01", "10000000000000000")] decimal Value,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal MinimumOrderAmount,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal? MaximumDiscount,
    DateTime StartsAtUtc, DateTime EndsAtUtc, [Range(1, int.MaxValue)] int? UsageLimit, [Range(1, int.MaxValue)] int? PerCustomerLimit);
public sealed record CouponView(Guid Id, string Code, CouponDiscountType DiscountType, decimal Value, decimal MinimumOrderAmount,
    decimal? MaximumDiscount, DateTime StartsAtUtc, DateTime EndsAtUtc, int? UsageLimit, int? PerCustomerLimit, bool IsActive);
public sealed record CouponQuote(string Code, decimal DiscountAmount, decimal PayableBeforeDelivery);
public sealed record CouponValidationRequest([Required, StringLength(40)] string Code);

public interface ICouponService
{
    Task<CouponQuote> QuoteAsync(string code, string? customerUserId, decimal subtotal, CancellationToken ct);
    Task ApplyAsync(string code, string? customerUserId, decimal subtotal, Guid orderId, CancellationToken ct);
    Task<IReadOnlyList<CouponView>> GetAsync(CancellationToken ct);
    Task<CouponView> CreateAsync(CouponRequest request, CancellationToken ct);
    Task<CouponView?> UpdateAsync(Guid id, CouponRequest request, CancellationToken ct);
    Task<bool> SetActiveAsync(Guid id, bool active, CancellationToken ct);
}
