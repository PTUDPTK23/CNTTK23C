using System.ComponentModel.DataAnnotations;
using Auction.Domain.Enums;

namespace Auction.Application.Dtos;

public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), MaxLength(256, ErrorMessage = "Email tối đa 256 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự."), MaxLength(72, ErrorMessage = "Mật khẩu tối đa 72 ký tự.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên hiển thị."), MinLength(2, ErrorMessage = "Tên hiển thị phải có ít nhất 2 ký tự."), MaxLength(80, ErrorMessage = "Tên hiển thị tối đa 80 ký tự.")]
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    public string Password { get; set; } = string.Empty;
}

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    DateTime CreatedAtUtc);

public sealed record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    UserDto User);

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    DateTime CreatedAtUtc,
    int TotalBids,
    int AuctionsJoined,
    int AuctionsWon);

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive,
    DateTime CreatedAtUtc,
    int BidCount);

public sealed class UpdateUserRoleRequest
{
    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    public string Role { get; set; } = "bidder";
}

public sealed class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}

public static class RoleNames
{
    public static string ToApi(this UserRole role) => role == UserRole.Admin ? "admin" : "bidder";

    public static UserRole Parse(string value) => value.Trim().ToLowerInvariant() switch
    {
        "admin" => UserRole.Admin,
        "bidder" => UserRole.Bidder,
        _ => throw Common.AppException.BadRequest("INVALID_ROLE", "Vai trò phải là 'admin' hoặc 'bidder'.")
    };
}
