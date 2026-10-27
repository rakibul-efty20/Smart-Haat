using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartHaat.Application.Catalog;
using SmartHaat.Application.Commerce;
using SmartHaat.Domain.Inventory;
using SmartHaat.Domain.Orders;
using SmartHaat.Application.Pricing;
using SmartHaat.Infrastructure.Persistence;

namespace SmartHaat.Infrastructure.Commerce;

public sealed class EfCommerceService(SmartHaatDbContext db, ICouponService coupons) : ICommerceService
{
    private static readonly TimeSpan ReservationLifetime = TimeSpan.FromMinutes(30);

    public async Task<CartView> GetCartAsync(string? userId, string? guestToken, CancellationToken ct)
    { var (cart, newToken) = await GetOrCreateCartAsync(userId, guestToken, ct); return await BuildCartAsync(cart.Id, newToken, ct); }

    public async Task<CartView> SetCartItemAsync(string? userId, string? guestToken, Guid productId, decimal quantity, CancellationToken ct)
    {
        if (quantity <= 0 || quantity > 999) throw new CatalogValidationException("Quantity must be greater than zero and no more than 999.");
        var product = await db.Products.Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == productId && x.IsActive && x.Category!.IsActive, ct)
            ?? throw new CatalogValidationException("Product is not available.");
        var available = await AvailableAsync(productId, ct);
        if (quantity > available) throw new CatalogValidationException("Requested quantity exceeds available stock.");
        var (cart, token) = await GetOrCreateCartAsync(userId, guestToken, ct);
        cart.AddOrSet(product.Id, quantity); await db.SaveChangesAsync(ct);
        return await BuildCartAsync(cart.Id, token, ct);
    }

    public async Task<CartView> RemoveCartItemAsync(string? userId, string? guestToken, Guid productId, CancellationToken ct)
    {
        var (cart, token) = await GetOrCreateCartAsync(userId, guestToken, ct);
        cart.Remove(productId); await db.SaveChangesAsync(ct);
        return await BuildCartAsync(cart.Id, token, ct);
    }

    public async Task<CartView> MergeGuestCartAsync(string userId, string guestToken, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var guest = await db.Carts.Include(c => c.Items).SingleOrDefaultAsync(c => c.GuestTokenHash == Hash(guestToken) && c.ExpiresAtUtc > DateTime.UtcNow, ct);
        var customer = await db.Carts.Include(c => c.Items).SingleOrDefaultAsync(c => c.UserId == userId, ct);
        if (customer is null) { customer = Cart.ForCustomer(userId); db.Carts.Add(customer); }
        if (guest is not null)
        {
            foreach (var item in guest.Items.ToArray())
            {
                var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == item.ProductId && p.IsActive && p.Category!.IsActive, ct);
                if (product is null) continue;
                var existing = customer.Items.SingleOrDefault(x => x.ProductId == item.ProductId)?.Quantity ?? 0;
                var available = await AvailableAsync(item.ProductId, ct);
                var merged = existing + item.Quantity;
                if (merged > available) throw new CatalogValidationException($"The merged quantity for {product.Name} exceeds available stock.");
                customer.AddOrSet(item.ProductId, merged);
            }
            db.Carts.Remove(guest);
        }
        customer.Touch(); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return await BuildCartAsync(customer.Id, null, ct);
    }

    public async Task<CreatedOrder> CheckoutAsync(string? userId, string? guestToken, CheckoutRequest request, string? idempotencyKey, CancellationToken ct)
    {
        ValidateIdempotencyKey(idempotencyKey);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey, ct);
            if (existing is not null)
            {
                if (userId is null || existing.UserId != userId) throw new CatalogConflictException("Idempotency key was already used for a different order.");
                return new(ToView(existing), null);
            }
        }
        if (string.IsNullOrWhiteSpace(guestToken) && userId is null) throw new CatalogValidationException("A guest cart is required before checkout.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var cart = userId is not null
            ? await db.Carts.Include(c => c.Items).SingleOrDefaultAsync(c => c.UserId == userId, ct)
            : await db.Carts.Include(c => c.Items).SingleOrDefaultAsync(c => c.GuestTokenHash == Hash(guestToken!), ct);
        if (cart is null || cart.Items.Count == 0) throw new CatalogValidationException("Your cart is empty.");
        var zone = await db.DeliveryZones.SingleOrDefaultAsync(x => x.Code == request.DeliveryZoneCode.Trim().ToUpperInvariant() && x.IsActive, ct)
            ?? throw new CatalogValidationException("Delivery zone is not available.");
        var ids = cart.Items.Select(x => x.ProductId).ToArray();
        var products = await db.Products.Include(p => p.Category).Where(p => ids.Contains(p.Id) && p.IsActive && p.Category!.IsActive).ToDictionaryAsync(p => p.Id, ct);
        if (products.Count != ids.Length) throw new CatalogValidationException("One or more products in the cart are no longer available.");
        var subtotal = decimal.Round(cart.Items.Sum(i => products[i.ProductId].Price * i.Quantity), 2, MidpointRounding.AwayFromZero);
        if (subtotal <= 0) throw new CatalogValidationException("Your cart is empty.");
        var couponQuote = string.IsNullOrWhiteSpace(request.CouponCode) ? null : await coupons.QuoteAsync(request.CouponCode, userId, subtotal, ct);
        var discount = couponQuote?.DiscountAmount ?? 0;
        var trackingToken = userId is null ? CreateToken() : null;
        var order = Order.CreateOnline(CreateOrderNumber(), userId, trackingToken is null ? null : Hash(trackingToken), request.CustomerName,
            request.CustomerPhone, request.CustomerEmail, request.AddressLine, zone.Name, request.DeliveryNotes, subtotal, discount, couponQuote?.Code, zone.Fee, idempotencyKey);
        foreach (var cartItem in cart.Items) { var p = products[cartItem.ProductId]; order.AddItem(p.Id, p.Sku, p.Name, p.Unit, p.Price, cartItem.Quantity); }
        db.Orders.Add(order);
        if (couponQuote is not null) await coupons.ApplyAsync(couponQuote.Code, userId, subtotal, order.Id, ct);
        var expires = DateTime.UtcNow.Add(ReservationLifetime);
        foreach (var orderItem in order.Items)
        {
            var remaining = orderItem.Quantity;
            var batches = await EligibleBatchesAsync(orderItem.ProductId!.Value, ct);
            foreach (var batch in batches)
            {
                var qty = Math.Min(batch.AvailableQuantity, remaining);
                if (qty <= 0) continue;
                batch.Reserve(qty); db.InventoryReservations.Add(new InventoryReservation(order.Id, orderItem.Id, batch.Id, qty, expires));
                remaining -= qty; if (remaining == 0) break;
            }
            if (remaining > 0) throw new CatalogValidationException($"Insufficient stock for {orderItem.NameSnapshot}.");
        }
        db.Carts.Remove(cart);
        await SaveCommerceAsync(ct); await tx.CommitAsync(ct);
        return new(ToView(order), trackingToken);
    }

    public async Task<CreatedOrder> CreatePosSaleAsync(string salesmanUserId, PosSaleRequest request, string? idempotencyKey, CancellationToken ct)
    {
        ValidateIdempotencyKey(idempotencyKey);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey, ct);
            if (existing is not null)
            {
                if (existing.CreatedByUserId != salesmanUserId) throw new CatalogConflictException("Idempotency key was already used for a different order.");
                return new(ToView(existing), null);
            }
        }
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var lines = request.Items.GroupBy(x => x.ProductId).Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) }).ToArray();
        var productIds = lines.Select(x => x.ProductId).ToArray();
        var products = await db.Products.Include(p => p.Category).Where(p => productIds.Contains(p.Id) && p.IsActive && p.Category!.IsActive).ToDictionaryAsync(p => p.Id, ct);
        if (products.Count != productIds.Length) throw new CatalogValidationException("One or more products are not available.");
        var subtotal = decimal.Round(lines.Sum(x => products[x.ProductId].Price * x.Quantity), 2, MidpointRounding.AwayFromZero);
        var order = Order.CreatePos(CreateOrderNumber(), request.CustomerName, request.CustomerPhone, subtotal, salesmanUserId, idempotencyKey);
        foreach (var line in lines) { var p = products[line.ProductId]; order.AddItem(p.Id, p.Sku, p.Name, p.Unit, p.Price, line.Quantity); }
        db.Orders.Add(order);
        foreach (var item in order.Items)
        {
            var remaining = item.Quantity;
            foreach (var batch in await EligibleBatchesAsync(item.ProductId!.Value, ct))
            {
                var qty = Math.Min(batch.AvailableQuantity, remaining); if (qty <= 0) continue;
                batch.Remove(qty); db.StockMovements.Add(new StockMovement(item.ProductId.Value, batch.Id, -qty, StockMovementReason.Sale, salesmanUserId, $"POS {order.OrderNumber}"));
                remaining -= qty; if (remaining == 0) break;
            }
            if (remaining > 0) throw new CatalogValidationException($"Insufficient stock for {item.NameSnapshot}.");
        }
        await SaveCommerceAsync(ct); await tx.CommitAsync(ct);
        return new(ToView(order), null);
    }

    public async Task<Page<OrderView>> GetOrdersAsync(string? userId, bool allOrders, bool salesperson, OrderStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var q = db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();
        if (!allOrders) q = salesperson ? q.Where(o => o.SalesmanUserId == userId || o.CreatedByUserId == userId) : q.Where(o => o.UserId == userId);
        if (status.HasValue) q = q.Where(o => o.Status == status.Value);
        var count = await q.CountAsync(ct);
        var items = await q.OrderByDescending(o => o.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(items.Select(ToView).ToList(), page, pageSize, count);
    }

    public async Task<OrderView?> GetOrderAsync(Guid id, string? userId, bool allOrders, bool salesperson, CancellationToken ct)
    {
        var q = db.Orders.AsNoTracking().Include(o => o.Items).Where(o => o.Id == id);
        if (!allOrders) q = salesperson ? q.Where(o => o.SalesmanUserId == userId || o.CreatedByUserId == userId) : q.Where(o => o.UserId == userId);
        var order = await q.SingleOrDefaultAsync(ct); return order is null ? null : ToView(order);
    }

    public async Task<OrderView?> TrackGuestOrderAsync(string orderNumber, string trackingToken, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Items).SingleOrDefaultAsync(o => o.OrderNumber == orderNumber && o.UserId == null, ct);
        if (order?.GuestTrackingTokenHash is null || !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(order.GuestTrackingTokenHash), Convert.FromHexString(Hash(trackingToken)))) return null;
        return ToView(order);
    }

    public async Task<OrderView?> ChangeOrderStatusAsync(Guid id, string? userId, bool allOrders, OrderStatusRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var q = db.Orders.Include(o => o.Items).Where(o => o.Id == id);
        if (!allOrders) q = q.Where(o => o.SalesmanUserId == userId || o.CreatedByUserId == userId);
        var order = await q.SingleOrDefaultAsync(ct); if (order is null) return null;
        order.ChangeStatus(request.Status, userId, request.Reason);
        if (request.Status == OrderStatus.Cancelled)
            await ReleaseOrderReservationsAsync(order.Id, ct);
        if (request.Status == OrderStatus.Delivered)
        {
            var reservations = await db.InventoryReservations.Where(r => r.OrderId == order.Id && r.ReleasedAtUtc == null).ToListAsync(ct);
            foreach (var reservation in reservations)
            {
                var batch = await db.InventoryBatches.SingleAsync(x => x.Id == reservation.BatchId, ct);
                batch.ConsumeReserved(reservation.Quantity); reservation.Release();
                var orderItem = order.Items.Single(x => x.Id == reservation.OrderItemId);
                db.StockMovements.Add(new StockMovement(orderItem.ProductId!.Value, batch.Id, -reservation.Quantity, StockMovementReason.Sale, userId, $"Order {order.OrderNumber}"));
            }
            if (order.PaymentMethod == PaymentMethod.CashOnDelivery) order.MarkPaid(userId, "Paid on delivery");
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return ToView(order);
    }

    public async Task<int> ExpireReservationsAsync(CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var expired = await db.InventoryReservations.Where(r => r.ReleasedAtUtc == null && r.ExpiresAtUtc <= DateTime.UtcNow).ToListAsync(ct);
        var orderIds = expired.Select(x => x.OrderId).Distinct().ToArray();
        foreach (var reservation in expired)
        {
            var batch = await db.InventoryBatches.SingleAsync(x => x.Id == reservation.BatchId, ct);
            batch.Release(reservation.Quantity); reservation.Release();
        }
        foreach (var orderId in orderIds)
        {
            var order = await db.Orders.SingleAsync(x => x.Id == orderId, ct);
            if (order.Status is OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.Packed or OrderStatus.OutForDelivery)
                order.ChangeStatus(OrderStatus.Cancelled, null, "Inventory reservation expired");
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return orderIds.Length;
    }

    public async Task<IReadOnlyList<DeliveryZoneView>> GetZonesAsync(bool activeOnly, CancellationToken ct) =>
        await db.DeliveryZones.AsNoTracking().Where(z => !activeOnly || z.IsActive).OrderBy(z => z.Name).Select(z => new DeliveryZoneView(z.Id, z.Code, z.Name, z.Fee, z.IsActive)).ToListAsync(ct);
    public async Task<DeliveryZoneView> CreateZoneAsync(DeliveryZoneRequest request, CancellationToken ct)
    {
        var zone = new DeliveryZone(request.Code, request.Name, request.Fee); db.DeliveryZones.Add(zone);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException e) when (IsUniqueViolation(e)) { throw new CatalogConflictException("Delivery zone code is already in use."); }
        return ToView(zone);
    }
    public async Task<DeliveryZoneView?> UpdateZoneAsync(Guid id, DeliveryZoneRequest request, CancellationToken ct)
    {
        var zone = await db.DeliveryZones.FindAsync([id], ct); if (zone is null) return null;
        zone.Update(request.Code, request.Name, request.Fee);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException e) when (IsUniqueViolation(e)) { throw new CatalogConflictException("Delivery zone code is already in use."); }
        return ToView(zone);
    }
    public async Task<bool> SetZoneActiveAsync(Guid id, bool active, CancellationToken ct)
    { var zone = await db.DeliveryZones.FindAsync([id], ct); if (zone is null) return false; zone.SetActive(active); await db.SaveChangesAsync(ct); return true; }

    private async Task<(Cart Cart, string? NewGuestToken)> GetOrCreateCartAsync(string? userId, string? guestToken, CancellationToken ct)
    {
        Cart? cart; string? newToken = null;
        if (userId is not null) cart = await db.Carts.Include(x => x.Items).SingleOrDefaultAsync(x => x.UserId == userId && x.ExpiresAtUtc > DateTime.UtcNow, ct);
        else if (!string.IsNullOrWhiteSpace(guestToken)) cart = await db.Carts.Include(x => x.Items).SingleOrDefaultAsync(x => x.GuestTokenHash == Hash(guestToken) && x.ExpiresAtUtc > DateTime.UtcNow, ct);
        else cart = null;
        if (cart is null)
        {
            if (userId is not null) cart = Cart.ForCustomer(userId);
            else { newToken = CreateToken(); cart = Cart.ForGuest(Hash(newToken)); }
            db.Carts.Add(cart); await db.SaveChangesAsync(ct);
        }
        return (cart, newToken);
    }

    private async Task<CartView> BuildCartAsync(Guid cartId, string? newToken, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var items = await (from i in db.CartItems.AsNoTracking()
                           join p in db.Products.AsNoTracking() on i.ProductId equals p.Id
                           where i.CartId == cartId && p.IsActive && p.Category!.IsActive
                           select new CartItemView(p.Id, p.Name, p.Sku, p.Unit, p.Price, i.Quantity,
                               db.InventoryBatches.Where(b => b.ProductId == p.Id && (b.ExpiryDate == null || b.ExpiryDate >= today)).Sum(b => (decimal?)(b.RemainingQuantity - b.ReservedQuantity)) ?? 0,
                               decimal.Round(p.Price * i.Quantity, 2), p.ImageKey == null ? null : "/media/products/" + p.ImageKey)).ToListAsync(ct);
        return new CartView(items, decimal.Round(items.Sum(i => i.LineTotal), 2), "BDT", newToken);
    }

    private async Task<decimal> AvailableAsync(Guid productId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.InventoryBatches.Where(x => x.ProductId == productId && (x.ExpiryDate == null || x.ExpiryDate >= today)).SumAsync(x => (decimal?)(x.RemainingQuantity - x.ReservedQuantity), ct) ?? 0;
    }

    private async Task<List<InventoryBatch>> EligibleBatchesAsync(Guid productId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.InventoryBatches.Where(x => x.ProductId == productId && x.RemainingQuantity > x.ReservedQuantity && (x.ExpiryDate == null || x.ExpiryDate >= today))
            .OrderBy(x => x.ExpiryDate == null).ThenBy(x => x.ExpiryDate).ThenBy(x => x.ReceivedAtUtc).ToListAsync(ct);
    }

    private async Task ReleaseOrderReservationsAsync(Guid orderId, CancellationToken ct)
    {
        var reservations = await db.InventoryReservations.Where(r => r.OrderId == orderId && r.ReleasedAtUtc == null).ToListAsync(ct);
        foreach (var reservation in reservations)
        { var batch = await db.InventoryBatches.SingleAsync(x => x.Id == reservation.BatchId, ct); batch.Release(reservation.Quantity); reservation.Release(); }
    }

    private async Task SaveCommerceAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new CatalogConflictException("Inventory changed while the order was being saved. Refresh and retry."); }
        catch (DbUpdateException e) when (IsUniqueViolation(e)) { throw new CatalogConflictException("This request was already processed or the identifier is already in use."); }
    }

    private static bool IsUniqueViolation(DbUpdateException e) => e.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true || e.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true;
    private static void ValidateIdempotencyKey(string? key) { if (key is { Length: > 100 }) throw new CatalogValidationException("Idempotency-Key must be 100 characters or fewer."); }
    private static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string CreateOrderNumber() => $"SH-{DateTime.UtcNow:yyyyMMdd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";
    private static OrderView ToView(Order o) => new(o.Id, o.OrderNumber, o.CustomerName, o.CustomerPhone, o.CustomerEmail, o.AddressLine, o.Area,
        o.Channel, o.Status, o.PaymentMethod, o.PaymentStatus, o.CouponCodeSnapshot, o.Subtotal, o.DiscountTotal, o.DeliveryFee, o.TaxTotal, o.Total, o.Currency,
        o.CreatedAtUtc, o.Items.Select(i => new OrderLineView(i.SkuSnapshot, i.NameSnapshot, i.UnitSnapshot, i.UnitPrice, i.Quantity, i.LineTotal)).ToList());
    private static DeliveryZoneView ToView(DeliveryZone zone) => new(zone.Id, zone.Code, zone.Name, zone.Fee, zone.IsActive);
}
