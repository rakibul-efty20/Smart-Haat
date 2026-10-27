using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartHaat.Infrastructure.Identity;

namespace SmartHaat.Api.Identity;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; init; } = "SmartHaat";
    public string Audience { get; init; } = "SmartHaat.Client";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
}

public interface ITokenIssuer
{
    Task<(string Token, DateTime ExpiresAtUtc)> CreateAccessTokenAsync(ApplicationUser user);
    string CreateRefreshToken();
    string HashRefreshToken(string token);
}

public sealed class TokenIssuer(IOptions<JwtOptions> options, UserManager<ApplicationUser> users) : ITokenIssuer
{
    public async Task<(string Token, DateTime ExpiresAtUtc)> CreateAccessTokenAsync(ApplicationUser user)
    {
        var cfg = options.Value; var expires = DateTime.UtcNow.AddMinutes(cfg.AccessTokenMinutes);
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id), new(JwtRegisteredClaimNames.Email, user.Email ?? ""), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), new("name", user.DisplayName) };
        claims.AddRange((await users.GetRolesAsync(user)).Select(role => new Claim(ClaimTypes.Role, role)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg.SigningKey));
        var jwt = new JwtSecurityToken(cfg.Issuer, cfg.Audience, claims, notBefore: DateTime.UtcNow, expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
    public string CreateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    public string HashRefreshToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
