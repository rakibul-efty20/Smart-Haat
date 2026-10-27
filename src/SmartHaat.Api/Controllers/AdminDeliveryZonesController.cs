using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHaat.Application.Commerce;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/admin/delivery-zones")]
[Authorize(Roles = "Admin")]
public sealed class AdminDeliveryZonesController(ICommerceService commerce) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await commerce.GetZonesAsync(false, ct));

    [HttpPost]
    public async Task<IActionResult> Create(DeliveryZoneRequest request, CancellationToken ct)
    { var zone = await commerce.CreateZoneAsync(request, ct); return Created("/api/delivery-zones", zone); }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, DeliveryZoneRequest request, CancellationToken ct)
    { var zone = await commerce.UpdateZoneAsync(id, request, ct); return zone is null ? NotFound() : Ok(zone); }
    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] bool isActive, CancellationToken ct) =>
        await commerce.SetZoneActiveAsync(id, isActive, ct) ? NoContent() : NotFound();
}
