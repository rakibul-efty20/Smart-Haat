using System.ComponentModel.DataAnnotations;

namespace SmartHaat.Api.Identity;

public sealed record RegisterRequest([Required, StringLength(160, MinimumLength = 2)] string DisplayName,
    [Required, EmailAddress, StringLength(256)] string Email, [Required, MinLength(10)] string Password);
public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public sealed record TokenResponse(string AccessToken, DateTime ExpiresAtUtc, string TokenType = "Bearer");
