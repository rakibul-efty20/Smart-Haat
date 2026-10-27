using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartHaat.Application.Commerce;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/pos/orders")]
[Authorize(Roles = "Salesman")]
public sealed class PosController(ICommerceService commerce) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Create(PosSaleRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken ct)
    {
        var result = await commerce.CreatePosSaleAsync(User.FindFirstValue("sub")!, request, idempotencyKey, ct);
        return Created($"/api/orders/{result.Order.Id}", result);
    }
}
