using SmartHaat.Domain.Catalog;

namespace SmartHaat.Domain.Inventory;

public sealed class InventoryBatch
{
    private InventoryBatch() { }
    public InventoryBatch(Guid productId, string batchNumber, DateTime receivedAtUtc, DateOnly? expiryDate, decimal quantity, decimal? unitCost = null)
    {
        if (productId == Guid.Empty) throw new ArgumentException("Product is required.");
        if (string.IsNullOrWhiteSpace(batchNumber) || batchNumber.Trim().Length > 80) throw new ArgumentException("Batch number is required and must be 80 characters or fewer.");
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Received quantity must be positive.");
        if (expiryDate.HasValue && expiryDate.Value < DateOnly.FromDateTime(receivedAtUtc)) throw new ArgumentException("Expiry cannot be before receipt date.");
        if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost));
        ProductId = productId; BatchNumber = batchNumber.Trim(); ReceivedAtUtc = receivedAtUtc; ExpiryDate = expiryDate;
        InitialQuantity = quantity; RemainingQuantity = quantity; UnitCost = unitCost;
    }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public string BatchNumber { get; private set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public decimal InitialQuantity { get; private set; }
    public decimal RemainingQuantity { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal AvailableQuantity => RemainingQuantity - ReservedQuantity;
    public decimal? UnitCost { get; private set; }
    public byte[] Version { get; private set; } = [];
    public void Remove(decimal quantity)
    {
        if (quantity <= 0 || quantity > AvailableQuantity) throw new ArgumentOutOfRangeException(nameof(quantity));
        RemainingQuantity -= quantity;
    }
    public void Add(decimal quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        RemainingQuantity += quantity; InitialQuantity += quantity;
    }
    public void Reserve(decimal quantity)
    { if (quantity <= 0 || quantity > AvailableQuantity) throw new ArgumentOutOfRangeException(nameof(quantity)); ReservedQuantity += quantity; }
    public void Release(decimal quantity)
    { if (quantity <= 0 || quantity > ReservedQuantity) throw new ArgumentOutOfRangeException(nameof(quantity)); ReservedQuantity -= quantity; }
    public void ConsumeReserved(decimal quantity)
    { if (quantity <= 0 || quantity > ReservedQuantity || quantity > RemainingQuantity) throw new ArgumentOutOfRangeException(nameof(quantity)); ReservedQuantity -= quantity; RemainingQuantity -= quantity; }
}
