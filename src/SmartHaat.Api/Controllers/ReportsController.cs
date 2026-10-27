using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHaat.Application.Reporting;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public sealed class ReportsController(IReportingService reports) : ControllerBase
{
    [HttpGet("sales")]
    public async Task<IActionResult> Sales([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, [FromQuery] int topCount = 10, CancellationToken ct = default)
    {
        if (fromUtc == default || toUtc <= fromUtc || toUtc - fromUtc > TimeSpan.FromDays(366) || topCount is < 1 or > 100)
            return BadRequest(new { message = "Provide a valid UTC period no longer than 366 days and topCount between 1 and 100." });
        return Ok(await reports.GetSalesAsync(AsUtc(fromUtc), AsUtc(toUtc), topCount, ct));
    }
    private static DateTime AsUtc(DateTime value) => value.Kind switch { DateTimeKind.Utc => value, DateTimeKind.Local => value.ToUniversalTime(), _ => DateTime.SpecifyKind(value, DateTimeKind.Utc) };
}
