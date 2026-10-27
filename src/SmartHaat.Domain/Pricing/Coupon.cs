namespace SmartHaat.Domain.Pricing;

public enum CouponDiscountType { Percentage = 1, FixedAmount = 2 }

public sealed class Coupon
{
    private Coupon() { }
    public Coupon(string code, CouponDiscountType type, decimal value, decimal minimumOrderAmount, decimal? maximumDiscount,
        DateTime startsAtUtc, DateTime endsAtUtc, int? usageLimit, int? perCustomerLimit)
    { Update(code, type, value, minimumOrderAmount, maximumDiscount, startsAtUtc, endsAtUtc, usageLimit, perCustomerLimit); }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; } = string.Empty;
    public CouponDiscountType DiscountType { get; private set; }
    public decimal Value { get; private set; }
    public decimal MinimumOrderAmount { get; private set; }
    public decimal? MaximumDiscount { get; private set; }
    public DateTime StartsAtUtc { get; private set; }
    public DateTime EndsAtUtc { get; private set; }
    public int? UsageLimit { get; private set; }
    public int? PerCustomerLimit { get; private set; }
    public bool IsActive { get; private set; } = true;
    public void Update(string code, CouponDiscountType type, decimal value, decimal minimumOrderAmount, decimal? maximumDiscount,
        DateTime startsAtUtc, DateTime endsAtUtc, int? usageLimit, int? perCustomerLimit)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 40) throw new ArgumentException("Coupon code is required and must be no more than 40 characters.");
        if (value <= 0 || (type == CouponDiscountType.Percentage && value > 100)) throw new ArgumentOutOfRangeException(nameof(value));
        if (minimumOrderAmount < 0 || maximumDiscount < 0 || usageLimit <= 0 || perCustomerLimit <= 0) throw new ArgumentOutOfRangeException(nameof(minimumOrderAmount));
        if (endsAtUtc <= startsAtUtc) throw new ArgumentException("Coupon end time must be after its start time.");
        Code = code.Trim().ToUpperInvariant(); DiscountType = type; Value = value; MinimumOrderAmount = minimumOrderAmount;
        MaximumDiscount = maximumDiscount; StartsAtUtc = startsAtUtc; EndsAtUtc = endsAtUtc; UsageLimit = usageLimit; PerCustomerLimit = perCustomerLimit;
    }
    public void SetActive(bool active) => IsActive = active;
    public decimal CalculateDiscount(decimal subtotal)
    {
        if (subtotal < MinimumOrderAmount) return 0;
        var amount = DiscountType == CouponDiscountType.Percentage ? subtotal * Value / 100m : Value;
        if (MaximumDiscount.HasValue) amount = Math.Min(amount, MaximumDiscount.Value);
        return decimal.Round(Math.Min(amount, subtotal), 2, MidpointRounding.AwayFromZero);
    }
}

public sealed class CouponRedemption
{
    private CouponRedemption() { }
    public CouponRedemption(Guid couponId, Guid orderId, string? customerUserId, decimal discountAmount)
    { CouponId = couponId; OrderId = orderId; CustomerUserId = customerUserId; DiscountAmount = discountAmount; RedeemedAtUtc = DateTime.UtcNow; }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CouponId { get; private set; }
    public Guid OrderId { get; private set; }
    public string? CustomerUserId { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public DateTime RedeemedAtUtc { get; private set; }
}
