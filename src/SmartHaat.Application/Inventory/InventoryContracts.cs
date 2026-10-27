using System.ComponentModel.DataAnnotations;
using SmartHaat.Domain.Inventory;

namespace SmartHaat.Application.Inventory;

public sealed record ReceiptRequest([Required] Guid ProductId, [Required, StringLength(80)] string BatchNumber,
    DateOnly? ExpiryDate, [Range(typeof(decimal), "0.001", "999999999999999.999")] decimal Quantity,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal? UnitCost, [StringLength(500)] string? Notes);
public sealed record AdjustmentRequest([Required] Guid ProductId, [Range(typeof(decimal), "-999999999999999.999", "999999999999999.999")] decimal QuantityChange,
    StockMovementReason Reason, [StringLength(500)] string? Notes);
public sealed record InventoryBatchDto(Guid Id, Guid ProductId, string ProductName, string Sku, string BatchNumber,
    DateTime ReceivedAtUtc, DateOnly? ExpiryDate, decimal InitialQuantity, decimal RemainingQuantity, decimal ReservedQuantity, decimal AvailableQuantity, decimal? UnitCost);
public sealed record InventorySummaryDto(Guid ProductId, string ProductName, string Sku, decimal OnHand, decimal Available, decimal ReorderLevel, DateOnly? NearestExpiry);
public sealed record StockMovementDto(long Id, Guid ProductId, Guid? BatchId, decimal QuantityChange, StockMovementReason Reason, string? ActorUserId, DateTime CreatedAtUtc, string? Notes);

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryBatchDto>> GetBatchesAsync(Guid? productId, bool includeEmpty, CancellationToken ct);
    Task<IReadOnlyList<InventoryBatchDto>> GetExpiryBatchesAsync(int days, bool expiredOnly, CancellationToken ct);
    Task<IReadOnlyList<InventorySummaryDto>> GetSummaryAsync(bool lowStockOnly, CancellationToken ct);
    Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(Guid productId, int page, int pageSize, CancellationToken ct);
    Task<InventoryBatchDto> ReceiveAsync(ReceiptRequest request, string? actorId, CancellationToken ct);
    Task AdjustAsync(AdjustmentRequest request, string? actorId, CancellationToken ct);
}
