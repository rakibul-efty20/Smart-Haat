using Microsoft.EntityFrameworkCore;
using SmartHaat.Application.Catalog;
using SmartHaat.Application.Inventory;
using SmartHaat.Domain.Inventory;
using SmartHaat.Infrastructure.Persistence;

namespace SmartHaat.Infrastructure.Inventory;

public sealed class EfInventoryService(SmartHaatDbContext db) : IInventoryService
{
    public async Task<IReadOnlyList<InventoryBatchDto>> GetBatchesAsync(Guid? productId, bool includeEmpty, CancellationToken ct)
    {
        var q = db.InventoryBatches.AsNoTracking().Include(x => x.Product).Where(x => includeEmpty || x.RemainingQuantity > 0);
        if (productId.HasValue) q = q.Where(x => x.ProductId == productId.Value);
        return await q.OrderBy(x => x.ExpiryDate == null).ThenBy(x => x.ExpiryDate).ThenBy(x => x.Product!.Name)
            .Select(x => new InventoryBatchDto(x.Id, x.ProductId, x.Product!.Name, x.Product.Sku, x.BatchNumber, x.ReceivedAtUtc, x.ExpiryDate, x.InitialQuantity, x.RemainingQuantity, x.ReservedQuantity, x.RemainingQuantity - x.ReservedQuantity, x.UnitCost)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<InventoryBatchDto>> GetExpiryBatchesAsync(int days, bool expiredOnly, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow); var cutoff = today.AddDays(days);
        var q = db.InventoryBatches.AsNoTracking().Include(x => x.Product).Where(x => x.RemainingQuantity > 0 && x.ExpiryDate != null);
        q = expiredOnly ? q.Where(x => x.ExpiryDate < today) : q.Where(x => x.ExpiryDate >= today && x.ExpiryDate <= cutoff);
        return await q.OrderBy(x => x.ExpiryDate).ThenBy(x => x.Product!.Name)
            .Select(x => new InventoryBatchDto(x.Id, x.ProductId, x.Product!.Name, x.Product.Sku, x.BatchNumber, x.ReceivedAtUtc, x.ExpiryDate, x.InitialQuantity, x.RemainingQuantity, x.ReservedQuantity, x.RemainingQuantity - x.ReservedQuantity, x.UnitCost)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<InventorySummaryDto>> GetSummaryAsync(bool lowStockOnly, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var q = db.Products.AsNoTracking().Where(p => p.IsActive && p.Category!.IsActive);
        if (lowStockOnly) q = q.Where(p => (db.InventoryBatches.Where(b => b.ProductId == p.Id && (b.ExpiryDate == null || b.ExpiryDate >= today)).Sum(b => (decimal?)(b.RemainingQuantity - b.ReservedQuantity)) ?? 0) <= p.ReorderLevel);
        return await q.OrderBy(p => p.Name).Select(p => new InventorySummaryDto(p.Id, p.Name, p.Sku,
            db.InventoryBatches.Where(b => b.ProductId == p.Id).Sum(b => (decimal?)b.RemainingQuantity) ?? 0,
            db.InventoryBatches.Where(b => b.ProductId == p.Id && (b.ExpiryDate == null || b.ExpiryDate >= today)).Sum(b => (decimal?)(b.RemainingQuantity - b.ReservedQuantity)) ?? 0,
            p.ReorderLevel,
            db.InventoryBatches.Where(b => b.ProductId == p.Id && b.RemainingQuantity > 0 && b.ExpiryDate >= DateOnly.FromDateTime(DateTime.UtcNow)).OrderBy(b => b.ExpiryDate).Select(b => b.ExpiryDate).FirstOrDefault()))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(Guid productId, int page, int pageSize, CancellationToken ct) =>
        await db.StockMovements.AsNoTracking().Where(x => x.ProductId == productId).OrderByDescending(x => x.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new StockMovementDto(x.Id, x.ProductId, x.BatchId, x.QuantityChange, x.Reason, x.ActorUserId, x.CreatedAtUtc, x.Notes)).ToListAsync(ct);

    public async Task<InventoryBatchDto> ReceiveAsync(ReceiptRequest r, string? actorId, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(x => x.Id == r.ProductId, ct)) throw new CatalogValidationException("Product does not exist.");
        var batch = new InventoryBatch(r.ProductId, r.BatchNumber, DateTime.UtcNow, r.ExpiryDate, r.Quantity, r.UnitCost);
        db.InventoryBatches.Add(batch);
        db.StockMovements.Add(new StockMovement(r.ProductId, batch.Id, r.Quantity, StockMovementReason.Receipt, actorId, r.Notes));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true || e.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
        { throw new CatalogConflictException("This batch number already exists for the product."); }
        var p = await db.Products.AsNoTracking().SingleAsync(x => x.Id == r.ProductId, ct);
        return new(batch.Id, batch.ProductId, p.Name, p.Sku, batch.BatchNumber, batch.ReceivedAtUtc, batch.ExpiryDate, batch.InitialQuantity, batch.RemainingQuantity, batch.ReservedQuantity, batch.AvailableQuantity, batch.UnitCost);
    }

    public async Task AdjustAsync(AdjustmentRequest r, string? actorId, CancellationToken ct)
    {
        if (r.QuantityChange == 0) throw new CatalogValidationException("Quantity change cannot be zero.");
        if (r.Reason == StockMovementReason.Receipt) throw new CatalogValidationException("Use the stock receipt operation to receive inventory.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        if (!await db.Products.AnyAsync(x => x.Id == r.ProductId, ct)) throw new CatalogValidationException("Product does not exist.");
        if (r.QuantityChange > 0)
        {
            if (r.Reason is StockMovementReason.Damage or StockMovementReason.ExpiredDisposal) throw new CatalogValidationException("Damage and expiry disposal must decrease stock.");
            var batch = new InventoryBatch(r.ProductId, $"ADJ-{Guid.NewGuid():N}", DateTime.UtcNow, null, r.QuantityChange);
            db.InventoryBatches.Add(batch);
            db.StockMovements.Add(new StockMovement(r.ProductId, batch.Id, r.QuantityChange, r.Reason, actorId, r.Notes));
        }
        else
        {
            var remaining = -r.QuantityChange;
            var batches = await db.InventoryBatches.Where(b => b.ProductId == r.ProductId && b.RemainingQuantity > 0)
                .OrderBy(b => b.ExpiryDate == null).ThenBy(b => b.ExpiryDate).ThenBy(b => b.ReceivedAtUtc).ToListAsync(ct);
            if (batches.Sum(x => x.AvailableQuantity) < remaining) throw new CatalogValidationException("Adjustment exceeds current available quantity.");
            foreach (var batch in batches)
            {
                var qty = Math.Min(batch.RemainingQuantity, remaining);
                if (qty <= 0) break;
                batch.Remove(qty); remaining -= qty;
                db.StockMovements.Add(new StockMovement(r.ProductId, batch.Id, -qty, r.Reason, actorId, r.Notes));
            }
        }
        try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        catch (DbUpdateConcurrencyException) { await tx.RollbackAsync(ct); throw new CatalogConflictException("Inventory changed while this adjustment was being saved. Refresh and try again."); }
    }
}
