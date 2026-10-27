using System.ComponentModel.DataAnnotations;
using SmartHaat.Domain.Orders;
using SmartHaat.Application.Catalog;

namespace SmartHaat.Application.Commerce;

public sealed record CartItemView(Guid ProductId, string Name, string Sku, string Unit, decimal UnitPrice, decimal Quantity, decimal AvailableQuantity, decimal LineTotal, string? ImageUrl);
public sealed record CartView(IReadOnlyList<CartItemView> Items, decimal Subtotal, string Currency, string? NewGuestToken = null);
public sealed record CheckoutRequest([Required, StringLength(160, MinimumLength = 2)] string CustomerName,
    [Required, Phone, StringLength(40)] string CustomerPhone, [EmailAddress, StringLength(256)] string? CustomerEmail,
    [Required, StringLength(400, MinimumLength = 5)] string AddressLine, [Required, StringLength(40)] string DeliveryZoneCode,
    [StringLength(500)] string? DeliveryNotes, [StringLength(40)] string? CouponCode);
public sealed record PosLineRequest([Required] Guid ProductId, [Range(typeof(decimal), "0.001", "999")] decimal Quantity);
public sealed record PosSaleRequest([Required, StringLength(160)] string CustomerName, [Required, Phone, StringLength(40)] string CustomerPhone,
    [Required, MinLength(1), MaxLength(100)] IReadOnlyList<PosLineRequest> Items);
public sealed record OrderLineView(string Sku, string Name, string Unit, decimal UnitPrice, decimal Quantity, decimal LineTotal);
public sealed record OrderView(Guid Id, string OrderNumber, string CustomerName, string CustomerPhone, string? CustomerEmail,
    string AddressLine, string Area, OrderChannel Channel, OrderStatus Status, PaymentMethod PaymentMethod, PaymentStatus PaymentStatus, string? CouponCode,
    decimal Subtotal, decimal DiscountTotal, decimal DeliveryFee, decimal TaxTotal, decimal Total, string Currency,
    DateTime CreatedAtUtc, IReadOnlyList<OrderLineView> Items);
public sealed record CreatedOrder(OrderView Order, string? GuestTrackingToken);
public sealed record TrackingRequest([Required, StringLength(40)] string OrderNumber, [Required, StringLength(128)] string TrackingToken);
public sealed record OrderStatusRequest(OrderStatus Status, [StringLength(500)] string? Reason);
public sealed record DeliveryZoneRequest([Required, StringLength(40)] string Code, [Required, StringLength(120)] string Name,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal Fee);
public sealed record DeliveryZoneView(Guid Id, string Code, string Name, decimal Fee, bool IsActive);

public interface ICommerceService
{
    Task<CartView> GetCartAsync(string? userId, string? guestToken, CancellationToken ct);
    Task<CartView> SetCartItemAsync(string? userId, string? guestToken, Guid productId, decimal quantity, CancellationToken ct);
    Task<CartView> RemoveCartItemAsync(string? userId, string? guestToken, Guid productId, CancellationToken ct);
    Task<CartView> MergeGuestCartAsync(string userId, string guestToken, CancellationToken ct);
    Task<CreatedOrder> CheckoutAsync(string? userId, string? guestToken, CheckoutRequest request, string? idempotencyKey, CancellationToken ct);
    Task<CreatedOrder> CreatePosSaleAsync(string salesmanUserId, PosSaleRequest request, string? idempotencyKey, CancellationToken ct);
    Task<Page<OrderView>> GetOrdersAsync(string? userId, bool allOrders, bool salesperson, OrderStatus? status, int page, int pageSize, CancellationToken ct);
    Task<OrderView?> GetOrderAsync(Guid id, string? userId, bool allOrders, bool salesperson, CancellationToken ct);
    Task<OrderView?> TrackGuestOrderAsync(string orderNumber, string trackingToken, CancellationToken ct);
    Task<OrderView?> ChangeOrderStatusAsync(Guid id, string? userId, bool allOrders, OrderStatusRequest request, CancellationToken ct);
    Task<int> ExpireReservationsAsync(CancellationToken ct);
    Task<IReadOnlyList<DeliveryZoneView>> GetZonesAsync(bool activeOnly, CancellationToken ct);
    Task<DeliveryZoneView> CreateZoneAsync(DeliveryZoneRequest request, CancellationToken ct);
    Task<DeliveryZoneView?> UpdateZoneAsync(Guid id, DeliveryZoneRequest request, CancellationToken ct);
    Task<bool> SetZoneActiveAsync(Guid id, bool active, CancellationToken ct);
}
