using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Auction.Application.Abstractions;
using Auction.Application.Dtos;
using Auction.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auction.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "OnlineAuction";
    public string Audience { get; set; } = "OnlineAuction.Client";
    public string Key { get; set; } = "dev-only-signing-key-change-me-online-auction-2026";
    public int ExpiryMinutes { get; set; } = 480;
}

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password) => global::BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return global::BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (global::BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        if (Encoding.UTF8.GetByteCount(_options.Key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long.");
        }
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new("role", user.Role.ToApi()),
            new(ClaimTypes.Role, user.Role == Domain.Enums.UserRole.Admin ? "Admin" : "Bidder")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
