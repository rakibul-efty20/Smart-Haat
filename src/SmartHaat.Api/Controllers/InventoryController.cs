using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHaat.Application.Inventory;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/admin/inventory")]
[Authorize(Roles = "Admin,Salesman")]
public sealed class InventoryController(IInventoryService inventory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Summary([FromQuery] bool lowStockOnly = false, CancellationToken ct = default) =>
        Ok(await inventory.GetSummaryAsync(lowStockOnly, ct));

    [HttpGet("low-stock")]
    public async Task<IActionResult> LowStock(CancellationToken ct) => Ok(await inventory.GetSummaryAsync(true, ct));

    [HttpGet("near-expiry")]
    public async Task<IActionResult> NearExpiry([FromQuery] int days = 30, CancellationToken ct = default)
    { if (days is < 1 or > 365) return BadRequest(new { message = "days must be between 1 and 365." }); return Ok(await inventory.GetExpiryBatchesAsync(days, false, ct)); }

    [HttpGet("expired")]
    public async Task<IActionResult> Expired(CancellationToken ct) => Ok(await inventory.GetExpiryBatchesAsync(0, true, ct));

    [HttpGet("batches")]
    public async Task<IActionResult> Batches([FromQuery] Guid? productId, [FromQuery] bool includeEmpty = false, CancellationToken ct = default) =>
        Ok(await inventory.GetBatchesAsync(productId, includeEmpty, ct));

    [HttpGet("products/{productId:guid}/movements")]
    public async Task<IActionResult> Movements(Guid productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 200) return BadRequest(new { message = "Page must be >= 1 and pageSize must be between 1 and 200." });
        return Ok(await inventory.GetMovementsAsync(productId, page, pageSize, ct));
    }

    [HttpPost("receipts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Receive(ReceiptRequest request, CancellationToken ct)
    {
        var batch = await inventory.ReceiveAsync(request, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), ct);
        return CreatedAtAction(nameof(Batches), new { productId = batch.ProductId }, batch);
    }

    [HttpPost("adjustments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Adjust(AdjustmentRequest request, CancellationToken ct)
    {
        await inventory.AdjustAsync(request, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), ct);
        return NoContent();
    }
}
