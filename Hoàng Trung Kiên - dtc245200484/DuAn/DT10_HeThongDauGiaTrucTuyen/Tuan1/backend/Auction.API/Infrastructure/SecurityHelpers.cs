using Auction.Application.Common;

namespace Auction.API.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this System.Security.Claims.ClaimsPrincipal user)
    {
        var value = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(value, out var id)
            ? id
            : throw AppException.Unauthorized();
    }

    public static bool IsAdmin(this System.Security.Claims.ClaimsPrincipal user) =>
        user.IsInRole("Admin");
}

public static class InternalRequestValidator
{
    public static bool HasValidKey(HttpRequest request, InternalApiOptions options)
    {
        if (!request.Headers.TryGetValue("X-Internal-Api-Key", out var provided) || provided.Count == 0)
        {
            return false;
        }

        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(options.Key);
        var actualBytes = System.Text.Encoding.UTF8.GetBytes(provided.ToString());

        return expectedBytes.Length == actualBytes.Length &&
               System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
