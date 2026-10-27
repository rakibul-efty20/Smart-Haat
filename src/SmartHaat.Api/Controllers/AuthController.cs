using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using SmartHaat.Api.Identity;
using SmartHaat.Infrastructure.Identity;
using SmartHaat.Infrastructure.Persistence;

namespace SmartHaat.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(UserManager<ApplicationUser> users, SmartHaatDbContext db, ITokenIssuer issuer, IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new ApplicationUser { UserName = request.Email.Trim(), Email = request.Email.Trim(), DisplayName = request.DisplayName.Trim() };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => new { e.Code, e.Description }) });
        var roleResult = await users.AddToRoleAsync(user, "Customer");
        if (!roleResult.Succeeded) { await users.DeleteAsync(user); return Problem("Account setup could not be completed.", statusCode: 500); }
        return StatusCode(StatusCodes.Status201Created, new { user.Id, user.Email, user.DisplayName });
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user) || !await users.CheckPasswordAsync(user, request.Password))
        {
            if (user is not null && user.IsActive && !await users.IsLockedOutAsync(user)) await users.AccessFailedAsync(user);
            return Unauthorized(new { message = "Email or password is incorrect." });
        }
        await users.ResetAccessFailedCountAsync(user);
        return Ok(await IssueAsync(user));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh()
    {
        if (!HasAllowedOrigin()) return Forbid();
        var refreshToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrWhiteSpace(refreshToken)) return Unauthorized(new { message = "Refresh token is invalid or expired." });
        var hash = issuer.HashRefreshToken(refreshToken);
        var old = await db.RefreshTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.TokenHash == hash);
        if (old is null || old.RevokedAtUtc != null || old.ExpiresAtUtc <= DateTime.UtcNow || !old.User.IsActive)
            return Unauthorized(new { message = "Refresh token is invalid or expired." });
        var replacement = issuer.CreateRefreshToken();
        old.RevokedAtUtc = DateTime.UtcNow;
        old.ReplacedByTokenHash = issuer.HashRefreshToken(replacement);
        db.RefreshTokens.Add(new RefreshToken { UserId = old.UserId, TokenHash = issuer.HashRefreshToken(replacement), ExpiresAtUtc = DateTime.UtcNow.AddDays(14) });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Unauthorized(new { message = "Refresh token is invalid or expired." }); }
        var (access, expires) = await issuer.CreateAccessTokenAsync(old.User);
        SetRefreshCookie(replacement);
        return Ok(new TokenResponse(access, expires));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!HasAllowedOrigin()) return Forbid();
        var refreshToken = Request.Cookies[RefreshCookieName];
        var token = string.IsNullOrWhiteSpace(refreshToken) ? null : await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == issuer.HashRefreshToken(refreshToken) && x.RevokedAtUtc == null);
        if (token is not null) { token.RevokedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(); }
        Response.Cookies.Delete(RefreshCookieName, RefreshCookieOptions());
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        // Inbound JWT claim mapping is disabled, so the user ID remains the standard `sub` claim.
        // UserManager.GetUserAsync expects ClaimTypes.NameIdentifier, which is not present here.
        var userId = User.FindFirstValue("sub");
        var user = string.IsNullOrWhiteSpace(userId) ? null : await users.FindByIdAsync(userId);
        return user is null ? Unauthorized() : Ok(new { user.Id, user.Email, user.DisplayName, Roles = await users.GetRolesAsync(user) });
    }

    private async Task<TokenResponse> IssueAsync(ApplicationUser user)
    {
        var (access, expires) = await issuer.CreateAccessTokenAsync(user);
        var refresh = issuer.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = issuer.HashRefreshToken(refresh), ExpiresAtUtc = DateTime.UtcNow.AddDays(14) });
        await db.SaveChangesAsync();
        SetRefreshCookie(refresh);
        return new TokenResponse(access, expires);
    }

    private const string RefreshCookieName = "SmartHaat.Refresh";
    private void SetRefreshCookie(string token) => Response.Cookies.Append(RefreshCookieName, token, RefreshCookieOptions(DateTimeOffset.UtcNow.AddDays(14)));
    private static CookieOptions RefreshCookieOptions(DateTimeOffset? expires = null) => new()
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Path = "/api/auth", IsEssential = true, Expires = expires
    };
    private bool HasAllowedOrigin()
    {
        var origin = Request.Headers["Origin"].ToString();
        var allowed = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        return !string.IsNullOrWhiteSpace(origin) && allowed.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }
}
