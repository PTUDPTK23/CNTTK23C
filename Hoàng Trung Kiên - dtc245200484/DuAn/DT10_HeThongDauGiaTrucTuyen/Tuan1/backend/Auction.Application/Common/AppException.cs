namespace Auction.Application.Common;

public class AppException : Exception
{
    public AppException(int statusCode, string code, string message, object? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Details = details;
    }

    public int StatusCode { get; }
    public string Code { get; }
    public object? Details { get; }

    public static AppException BadRequest(string code, string message) => new(400, code, message);
    public static AppException Unauthorized(string message = "Cần đăng nhập để thực hiện thao tác này.") => new(401, "UNAUTHORIZED", message);
    public static AppException Forbidden(string message = "Bạn không có quyền thực hiện thao tác này.") => new(403, "FORBIDDEN", message);
    public static AppException NotFound(string code, string message) => new(404, code, message);
    public static AppException Conflict(string code, string message, object? details = null) => new(409, code, message, details);
}

public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int total)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        Total = total;
        TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
    }

    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int Total { get; }
    public int TotalPages { get; }
}

public static class Pagination
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize, int maxPageSize = 100)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 12;
        if (pageSize > maxPageSize) pageSize = maxPageSize;
        return (page, pageSize);
    }
}
