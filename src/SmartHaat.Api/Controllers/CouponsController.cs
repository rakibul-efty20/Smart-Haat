using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartHaat.Application.Commerce;
using SmartHaat.Application.Pricing;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/coupons")]
public sealed class CouponsController(ICommerceService commerce, ICouponService coupons) : ControllerBase
{
    [HttpPost("validate")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Validate(CouponValidationRequest request, CancellationToken ct)
    {
        var cart = await commerce.GetCartAsync(User.FindFirstValue("sub"), Request.Cookies["SmartHaat.Cart"], ct);
        if (cart.NewGuestToken is not null) Response.Cookies.Append("SmartHaat.Cart", cart.NewGuestToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Path = "/api", IsEssential = true, Expires = DateTimeOffset.UtcNow.AddDays(30) });
        if (cart.Items.Count == 0) return BadRequest(new { message = "Add an item to your cart before validating a coupon." });
        return Ok(await coupons.QuoteAsync(request.Code, User.FindFirstValue("sub"), cart.Subtotal, ct));
    }
}
