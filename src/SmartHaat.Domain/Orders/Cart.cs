namespace SmartHaat.Domain.Orders;

public sealed class Cart
{
    private Cart() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string? UserId { get; private set; }
    public string? GuestTokenHash { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; private set; }
    public ICollection<CartItem> Items { get; private set; } = new List<CartItem>();
    public static Cart ForCustomer(string userId) => new() { UserId = userId, ExpiresAtUtc = DateTime.UtcNow.AddDays(90) };
    public static Cart ForGuest(string tokenHash) => new() { GuestTokenHash = tokenHash, ExpiresAtUtc = DateTime.UtcNow.AddDays(30) };
    public void AddOrSet(Guid productId, decimal quantity)
    {
        if (productId == Guid.Empty || quantity <= 0 || quantity > 999) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero and at most 999.");
        var item = Items.SingleOrDefault(x => x.ProductId == productId);
        if (item is null) Items.Add(new CartItem(Id, productId, quantity)); else item.SetQuantity(quantity);
        Touch();
    }
    public void Remove(Guid productId) { var item = Items.SingleOrDefault(x => x.ProductId == productId); if (item is not null) Items.Remove(item); Touch(); }
    public void Clear() { Items.Clear(); Touch(); }
    public void Touch() { UpdatedAtUtc = DateTime.UtcNow; ExpiresAtUtc = UserId is null ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddDays(90); }
}

public sealed class CartItem
{
    private CartItem() { }
    internal CartItem(Guid cartId, Guid productId, decimal quantity) { CartId = cartId; ProductId = productId; SetQuantity(quantity); }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public void SetQuantity(decimal quantity)
    { if (quantity <= 0 || quantity > 999) throw new ArgumentOutOfRangeException(nameof(quantity)); Quantity = decimal.Round(quantity, 3, MidpointRounding.AwayFromZero); }
}
