using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHaat.Application.Commerce;
using SmartHaat.Domain.Orders;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin,Salesman")]
public sealed class AdminOrdersController(ICommerceService commerce) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] OrderStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize) return BadRequest(new { message = "Page must be >= 1 and pageSize must be between 1 and 100." });
        return Ok(await commerce.GetOrdersAsync(UserId(), User.IsInRole("Admin"), User.IsInRole("Salesman"), status, page, pageSize, ct));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, OrderStatusRequest request, CancellationToken ct)
    { var order = await commerce.ChangeOrderStatusAsync(id, UserId(), User.IsInRole("Admin"), request, ct); return order is null ? NotFound() : Ok(order); }

    private string? UserId() => User.FindFirstValue("sub");
}
