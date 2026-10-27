namespace SmartHaat.Domain.Inventory;

public enum StockMovementReason { Receipt = 1, Sale = 2, Damage = 3, ExpiredDisposal = 4, StocktakeCorrection = 5, ManualAdjustment = 6 }

public sealed class StockMovement
{
    private StockMovement() { }
    public StockMovement(Guid productId, Guid? batchId, decimal quantityChange, StockMovementReason reason, string? actorUserId, string? notes = null)
    {
        if (productId == Guid.Empty || quantityChange == 0) throw new ArgumentException("Product and non-zero quantity change are required.");
        ProductId = productId; BatchId = batchId; QuantityChange = quantityChange; Reason = reason;
        ActorUserId = actorUserId; Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(); CreatedAtUtc = DateTime.UtcNow;
    }
    public long Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? BatchId { get; private set; }
    public decimal QuantityChange { get; private set; }
    public StockMovementReason Reason { get; private set; }
    public string? ActorUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? Notes { get; private set; }
}
