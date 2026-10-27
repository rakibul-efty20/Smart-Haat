using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SmartHaat.Infrastructure.Identity;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/development/admin-bootstrap")]
[AllowAnonymous]
public sealed class DevelopmentAdminController(IWebHostEnvironment environment, IConfiguration configuration, UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Create([FromHeader(Name = "X-Bootstrap-Key")] string? key, [FromBody] BootstrapRequest request)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var expected = configuration["AdminBootstrap:Key"];
        var email = configuration["AdminBootstrap:Email"];
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(email) || !CryptographicEquals(key, expected)) return NotFound();
        if (await users.GetUsersInRoleAsync("Admin") is { Count: > 0 }) return Conflict(new { message = "An administrator already exists." });
        if (!await roles.RoleExistsAsync("Admin")) await roles.CreateAsync(new IdentityRole("Admin"));
        if (await users.Users.AnyAsync(u => u.Email == email)) return Conflict(new { message = "The configured bootstrap email already belongs to an account." });
        var user = new ApplicationUser { Email = email, UserName = email, DisplayName = request.DisplayName.Trim(), EmailConfirmed = true };
        var create = await users.CreateAsync(user, request.Password);
        if (!create.Succeeded) return BadRequest(new { errors = create.Errors.Select(e => e.Description) });
        var assign = await users.AddToRoleAsync(user, "Admin");
        if (!assign.Succeeded) { await users.DeleteAsync(user); return Problem("Admin role setup failed.", statusCode: 500); }
        return StatusCode(StatusCodes.Status201Created, new { message = "Development administrator is ready." });
    }

    private static bool CryptographicEquals(string? provided, string expected)
    {
        if (provided is null) return false;
        var a = System.Text.Encoding.UTF8.GetBytes(provided); var b = System.Text.Encoding.UTF8.GetBytes(expected);
        return a.Length == b.Length && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b);
    }
}

public sealed record BootstrapRequest([System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(160, MinimumLength = 2)] string DisplayName,
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MinLength(10)] string Password);
