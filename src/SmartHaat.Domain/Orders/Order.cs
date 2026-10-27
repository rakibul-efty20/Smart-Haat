namespace SmartHaat.Domain.Orders;

public enum OrderChannel { Online = 1, Pos = 2 }
public enum OrderStatus { Pending = 1, Confirmed = 2, Packed = 3, OutForDelivery = 4, Delivered = 5, Completed = 6, Cancelled = 7 }
public enum PaymentMethod { CashOnDelivery = 1, Cash = 2 }
public enum PaymentStatus { Pending = 1, Paid = 2, Failed = 3, Refunded = 4 }

public sealed class Order
{
    private Order() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string OrderNumber { get; private set; } = string.Empty;
    public string? IdempotencyKey { get; private set; }
    public string? UserId { get; private set; }
    public string? GuestTrackingTokenHash { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;
    public string? CustomerEmail { get; private set; }
    public string AddressLine { get; private set; } = string.Empty;
    public string Area { get; private set; } = string.Empty;
    public string? DeliveryNotes { get; private set; }
    public OrderChannel Channel { get; private set; }
    public OrderStatus Status { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public string? CouponCodeSnapshot { get; private set; }
    public decimal DeliveryFee { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal Total { get; private set; }
    public string Currency { get; private set; } = "BDT";
    public string? CreatedByUserId { get; private set; }
    public string? SalesmanUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;
    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> History { get; private set; } = new List<OrderStatusHistory>();

    public static Order CreateOnline(string number, string? userId, string? trackingHash, string name, string phone, string? email,
        string address, string area, string? notes, decimal subtotal, decimal discountTotal, string? couponCode, decimal deliveryFee, string? idempotencyKey)
    {
        var order = new Order { OrderNumber = number, UserId = userId, GuestTrackingTokenHash = trackingHash, CustomerName = name.Trim(),
            CustomerPhone = phone.Trim(), CustomerEmail = email?.Trim(), AddressLine = address.Trim(), Area = area.Trim(), DeliveryNotes = Clean(notes),
            Channel = OrderChannel.Online, Status = OrderStatus.Pending, PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Pending, Subtotal = Money(subtotal), DiscountTotal = Money(discountTotal), CouponCodeSnapshot = Clean(couponCode),
            DeliveryFee = Money(deliveryFee), TaxTotal = 0, Total = Money(subtotal - discountTotal + deliveryFee), IdempotencyKey = idempotencyKey };
        order.History.Add(new OrderStatusHistory(order.Id, null, order.Status, userId, "Order placed"));
        return order;
    }

    public static Order CreatePos(string number, string name, string phone, decimal subtotal, string salesmanId, string? idempotencyKey)
    {
        var order = new Order { OrderNumber = number, CustomerName = name.Trim(), CustomerPhone = phone.Trim(), Area = "In store", AddressLine = "In store",
            Channel = OrderChannel.Pos, Status = OrderStatus.Completed, PaymentMethod = PaymentMethod.Cash, PaymentStatus = PaymentStatus.Paid,
            Subtotal = Money(subtotal), Total = Money(subtotal), TaxTotal = 0, SalesmanUserId = salesmanId, CreatedByUserId = salesmanId, IdempotencyKey = idempotencyKey };
        order.History.Add(new OrderStatusHistory(order.Id, null, order.Status, salesmanId, "POS cash sale"));
        return order;
    }

    public void AddItem(Guid? productId, string sku, string name, string unit, decimal unitPrice, decimal quantity)
    {
        if (quantity <= 0 || unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        Items.Add(new OrderItem(Id, productId, sku, name, unit, Money(unitPrice), quantity, Money(unitPrice * quantity)));
    }

    public void ChangeStatus(OrderStatus next, string? actorId, string? reason)
    {
        var allowed = Status switch
        {
            OrderStatus.Pending => next is OrderStatus.Confirmed or OrderStatus.Cancelled,
            OrderStatus.Confirmed => next is OrderStatus.Packed or OrderStatus.Cancelled,
            OrderStatus.Packed => next is OrderStatus.OutForDelivery or OrderStatus.Delivered or OrderStatus.Cancelled,
            OrderStatus.OutForDelivery => next is OrderStatus.Delivered or OrderStatus.Cancelled,
            _ => false
        };
        if (!allowed) throw new OrderStateException($"Order cannot move from {Status} to {next}.");
        var old = Status; Status = next; UpdatedAtUtc = DateTime.UtcNow;
        History.Add(new OrderStatusHistory(Id, old, next, actorId, Clean(reason)));
    }

    public void MarkPaid(string? actorId, string? reason)
    {
        if (PaymentStatus != PaymentStatus.Pending) throw new OrderStateException("Only pending payments can be marked paid.");
        PaymentStatus = PaymentStatus.Paid; UpdatedAtUtc = DateTime.UtcNow;
    }
    private static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}

public sealed class OrderItem
{
    private OrderItem() { }
    internal OrderItem(Guid orderId, Guid? productId, string sku, string name, string unit, decimal unitPrice, decimal quantity, decimal lineTotal)
    { OrderId = orderId; ProductId = productId; SkuSnapshot = sku; NameSnapshot = name; UnitSnapshot = unit; UnitPrice = unitPrice; Quantity = quantity; LineTotal = lineTotal; }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid? ProductId { get; private set; }
    public string SkuSnapshot { get; private set; } = string.Empty;
    public string NameSnapshot { get; private set; } = string.Empty;
    public string UnitSnapshot { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal LineTotal { get; private set; }
}

public sealed class OrderStatusHistory
{
    private OrderStatusHistory() { }
    internal OrderStatusHistory(Guid orderId, OrderStatus? oldStatus, OrderStatus newStatus, string? actorId, string? reason)
    { OrderId = orderId; OldStatus = oldStatus; NewStatus = newStatus; ActorUserId = actorId; Reason = reason; CreatedAtUtc = DateTime.UtcNow; }
    public long Id { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderStatus? OldStatus { get; private set; }
    public OrderStatus NewStatus { get; private set; }
    public string? ActorUserId { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}

public sealed class InventoryReservation
{
    private InventoryReservation() { }
    public InventoryReservation(Guid orderId, Guid orderItemId, Guid batchId, decimal quantity, DateTime expiresAtUtc)
    { OrderId = orderId; OrderItemId = orderItemId; BatchId = batchId; Quantity = quantity; ExpiresAtUtc = expiresAtUtc; }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public Guid BatchId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? ReleasedAtUtc { get; private set; }
    public bool IsReleased => ReleasedAtUtc.HasValue;
    public void Release() => ReleasedAtUtc ??= DateTime.UtcNow;
}
