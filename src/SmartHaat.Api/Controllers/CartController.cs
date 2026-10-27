using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHaat.Application.Commerce;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/cart")]
public sealed class CartController(ICommerceService commerce) : ControllerBase
{
    private const string CookieName = "SmartHaat.Cart";

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => View(await commerce.GetCartAsync(UserId(), Request.Cookies[CookieName], ct));

    [HttpPost("items")]
    public async Task<IActionResult> Add(CartItemRequest request, CancellationToken ct) => View(await commerce.SetCartItemAsync(UserId(), Request.Cookies[CookieName], request.ProductId, request.Quantity, ct));

    [HttpPut("items/{productId:guid}")]
    public async Task<IActionResult> Set(Guid productId, CartItemQuantity request, CancellationToken ct) => View(await commerce.SetCartItemAsync(UserId(), Request.Cookies[CookieName], productId, request.Quantity, ct));

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> Remove(Guid productId, CancellationToken ct) => View(await commerce.RemoveCartItemAsync(UserId(), Request.Cookies[CookieName], productId, ct));

    [HttpPost("merge")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Merge(CancellationToken ct)
    {
        var token = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(token)) return Ok(await commerce.GetCartAsync(UserId(), null, ct));
        var result = await commerce.MergeGuestCartAsync(UserId()!, token, ct);
        Response.Cookies.Delete(CookieName, CookieOptions());
        return Ok(result);
    }

    private IActionResult View(CartView result)
    {
        if (result.NewGuestToken is not null) Response.Cookies.Append(CookieName, result.NewGuestToken, CookieOptions(DateTimeOffset.UtcNow.AddDays(30)));
        return Ok(result with { NewGuestToken = null });
    }
    private string? UserId() => User.FindFirstValue("sub");
    private static CookieOptions CookieOptions(DateTimeOffset? expires = null) => new()
    // Checkout and coupon validation also read this cookie under /api/*.
    { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Path = "/api", IsEssential = true, Expires = expires };
}

public sealed record CartItemRequest([System.ComponentModel.DataAnnotations.Required] Guid ProductId,
    [System.ComponentModel.DataAnnotations.Range(typeof(decimal), "0.001", "999")] decimal Quantity);
public sealed record CartItemQuantity([System.ComponentModel.DataAnnotations.Range(typeof(decimal), "0.001", "999")] decimal Quantity);
