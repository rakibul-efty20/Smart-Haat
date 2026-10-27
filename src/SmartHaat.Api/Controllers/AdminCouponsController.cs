using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHaat.Application.Pricing;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/admin/coupons")]
[Authorize(Roles = "Admin")]
public sealed class AdminCouponsController(ICouponService coupons) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await coupons.GetAsync(ct));
    [HttpPost]
    public async Task<IActionResult> Create(CouponRequest request, CancellationToken ct)
    { var coupon = await coupons.CreateAsync(request, ct); return CreatedAtAction(nameof(List), coupon); }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CouponRequest request, CancellationToken ct)
    { var coupon = await coupons.UpdateAsync(id, request, ct); return coupon is null ? NotFound() : Ok(coupon); }
    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] bool isActive, CancellationToken ct) =>
        await coupons.SetActiveAsync(id, isActive, ct) ? NoContent() : NotFound();
}
