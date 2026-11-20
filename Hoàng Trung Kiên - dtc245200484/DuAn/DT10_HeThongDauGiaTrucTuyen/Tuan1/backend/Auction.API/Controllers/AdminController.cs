using Auction.API.Infrastructure;
using Auction.Application.Common;
using Auction.Application.Dtos;
using Auction.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auction.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminController : ControllerBase
{
    private readonly AdminService _admin;
    private readonly ProductService _products;

    public AdminController(AdminService admin, ProductService products)
    {
        _admin = admin;
        _products = products;
    }

    /// <summary>Dashboard totals, 14-day bid series, auction status split and recent activity.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DashboardDto>> Dashboard(CancellationToken ct) =>
        Ok(await _admin.GetDashboardAsync(ct));

    /// <summary>List / search user accounts with bid counts.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> Users(
        [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await _admin.GetUsersAsync(q, page, pageSize, ct));

    /// <summary>Change a user's role (admin/bidder).</summary>
    [HttpPut("users/{id:guid}/role")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminUserDto>> UpdateRole(Guid id, [FromBody] UpdateUserRoleRequest request,
        CancellationToken ct) =>
        Ok(await _admin.UpdateUserRoleAsync(id, request.Role, User.GetRequiredUserId(), ct));

    /// <summary>Enable or disable a user account.</summary>
    [HttpPut("users/{id:guid}/status")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminUserDto>> UpdateStatus(Guid id, [FromBody] UpdateUserStatusRequest request,
        CancellationToken ct) =>
        Ok(await _admin.UpdateUserStatusAsync(id, request.IsActive, User.GetRequiredUserId(), ct));

    /// <summary>Audit trail written by the independent Audit Worker from RabbitMQ events.</summary>
    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> AuditLogs(
        [FromQuery] string? eventType, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        CancellationToken ct = default) =>
        Ok(await _admin.GetAuditLogsAsync(eventType, page, pageSize, ct));

    /// <summary>Product catalogue for admin tables (same data as /api/products).</summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductDto>>> Products(
        [FromQuery] string? q, [FromQuery] string? category, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await _products.ListAsync(q, category, page, pageSize, ct));
}
