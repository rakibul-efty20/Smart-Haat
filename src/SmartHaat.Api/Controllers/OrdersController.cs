using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartHaat.Application.Commerce;
using SmartHaat.Domain.Orders;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(ICommerceService commerce) : ControllerBase
{
    private const string CartCookie = "SmartHaat.Cart";

    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Checkout(CheckoutRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken ct)
    {
        var result = await commerce.CheckoutAsync(UserId(), Request.Cookies[CartCookie], request, idempotencyKey, ct);
        Response.Cookies.Delete(CartCookie, new CookieOptions { Path = "/api", Secure = true, HttpOnly = true, SameSite = SameSiteMode.None });
        return CreatedAtAction(nameof(Get), new { id = result.Order.Id }, result);
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> Mine([FromQuery] OrderStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!ValidPage(page, pageSize)) return BadRequest(new { message = "Page must be >= 1 and pageSize must be between 1 and 100." });
        return Ok(await commerce.GetOrdersAsync(UserId(), false, User.IsInRole("Salesman"), status, page, pageSize, ct));
    }

    [HttpPost("track")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Track(TrackingRequest request, CancellationToken ct)
    {
        var order = await commerce.TrackGuestOrderAsync(request.OrderNumber, request.TrackingToken, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var order = await commerce.GetOrderAsync(id, UserId(), User.IsInRole("Admin"), User.IsInRole("Salesman"), ct);
        return order is null ? NotFound() : Ok(order);
    }

    private string? UserId() => User.FindFirstValue("sub");
    private static bool ValidPage(int page, int pageSize) => page >= 1 && pageSize is >= 1 and <= 100 && page <= int.MaxValue / pageSize;
}
