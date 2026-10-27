using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHaat.Application.Commerce;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/delivery-zones")]
public sealed class DeliveryZonesController(ICommerceService commerce) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await commerce.GetZonesAsync(true, ct));

}
