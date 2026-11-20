using Auction.Application.Abstractions;
using Auction.Application.Common;
using Auction.Application.Dtos;
using Auction.Domain.Entities;
using Auction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Auction.Application.Services;

public sealed class AuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IClock _clock;

    public AuthService(IAppDbContext db, IPasswordHasher hasher, ITokenService tokens, IClock clock)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _clock = clock;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var normalized = Normalize(request.Email);
        var exists = await _db.Users.AnyAsync(u => u.NormalizedEmail == normalized, ct);
        if (exists)
        {
            throw AppException.Conflict("EMAIL_ALREADY_REGISTERED", "Email này đã được đăng ký tài khoản.");
        }

        var user = new User
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalized,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            Role = UserRole.Bidder,
            CreatedAtUtc = _clock.UtcNow,
            UpdatedAtUtc = _clock.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var normalized = Normalize(request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            throw AppException.Unauthorized("Email hoặc mật khẩu không chính xác.");
        }

        if (!user.IsActive)
        {
            throw AppException.Forbidden("Tài khoản này đã bị khóa.");
        }

        return BuildAuthResponse(user);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var (token, expiresAt) = _tokens.CreateToken(user);
        return new AuthResponse(token, expiresAt, ToDto(user));
    }

    public static UserDto ToDto(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.Role.ToApi(), user.CreatedAtUtc);

    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
