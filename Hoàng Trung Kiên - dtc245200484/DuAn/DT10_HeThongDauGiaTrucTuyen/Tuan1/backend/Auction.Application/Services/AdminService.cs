using Auction.Application.Abstractions;
using Auction.Application.Common;
using Auction.Application.Dtos;
using Auction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Auction.Application.Services;

public sealed class AdminService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public AdminService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var since = now.Date.AddDays(-13);

        var totalProducts = await _db.Products.CountAsync(ct);
        var totalAuctions = await _db.Auctions.CountAsync(ct);
        var activeAuctions = await _db.Auctions.CountAsync(a => a.Status == AuctionStatus.Active && a.EndTimeUtc > now, ct);
        var scheduledAuctions = await _db.Auctions.CountAsync(a => a.Status == AuctionStatus.Scheduled, ct);
        var endedAuctions = await _db.Auctions.CountAsync(a => a.Status == AuctionStatus.Ended, ct);
        var totalUsers = await _db.Users.CountAsync(ct);
        var totalBids = await _db.Bids.CountAsync(ct);
        var totalBidders = await _db.Participants.Select(p => p.UserId).Distinct().CountAsync(ct);
        var volume = await _db.Bids.Select(b => (decimal?)b.Amount).SumAsync(ct) ?? 0m;

        var rawBidsPerDay = await _db.Bids.AsNoTracking()
            .Where(b => b.PlacedAtUtc >= since)
            .GroupBy(b => b.PlacedAtUtc.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var bidsPerDay = new List<TimePointDto>();
        for (var day = 0; day < 14; day++)
        {
            var date = since.AddDays(day);
            var count = rawBidsPerDay.FirstOrDefault(x => x.Date.Date == date)?.Count ?? 0;
            bidsPerDay.Add(new TimePointDto(date.ToString("yyyy-MM-dd"), count));
        }

        var statusCounts = await _db.Auctions.AsNoTracking()
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var auctionsByStatus = new List<StatusCountDto>
        {
            new("active", statusCounts.FirstOrDefault(s => s.Status == AuctionStatus.Active)?.Count ?? 0),
            new("scheduled", statusCounts.FirstOrDefault(s => s.Status == AuctionStatus.Scheduled)?.Count ?? 0),
            new("ended", statusCounts.FirstOrDefault(s => s.Status == AuctionStatus.Ended)?.Count ?? 0),
            new("cancelled", statusCounts.FirstOrDefault(s => s.Status == AuctionStatus.Cancelled)?.Count ?? 0)
        };

        var topAuctions = await _db.Auctions.AsNoTracking()
            .OrderByDescending(a => a.CurrentPrice)
            .Take(5)
            .Select(a => new TopAuctionDto(
                a.Id, a.Title,
                a.Status == AuctionStatus.Ended ? "ended" : a.Status == AuctionStatus.Active ? "active" : "scheduled",
                a.CurrentPrice, a.Bids.Count, a.Winner != null ? a.Winner.DisplayName : null))
            .ToListAsync(ct);

        var recentBids = await _db.Bids.AsNoTracking()
            .OrderByDescending(b => b.PlacedAtUtc)
            .Take(8)
            .Select(b => new BidDto(b.Id, b.AuctionId, b.Auction.Title, b.Amount, b.BidderId,
                b.Bidder.DisplayName, b.PlacedAtUtc, b.Sequence, b.IsWinning))
            .ToListAsync(ct);

        return new DashboardDto(
            totalProducts, totalAuctions, activeAuctions, scheduledAuctions, endedAuctions,
            totalUsers, totalBidders, totalBids, volume,
            bidsPerDay, auctionsByStatus, topAuctions, recentBids, now);
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Pagination.Normalize(page, 20, 100);
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(term) || u.DisplayName.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto(u.Id, u.Email, u.DisplayName, u.Role == UserRole.Admin ? "admin" : "bidder",
                u.IsActive, u.CreatedAtUtc, u.Bids.Count))
            .ToListAsync(ct);

        return new PagedResult<AdminUserDto>(items, page, pageSize, total);
    }

    public async Task<AdminUserDto> UpdateUserRoleAsync(Guid userId, string role, Guid actorId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw AppException.NotFound("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        var newRole = RoleNames.Parse(role);

        if (user.Role == UserRole.Admin && newRole != UserRole.Admin)
        {
            if (user.Id == actorId)
            {
                throw AppException.Conflict("CANNOT_DEMOTE_SELF", "Bạn không thể tự gỡ quyền quản trị của chính mình.");
            }

            var adminCount = await _db.Users.CountAsync(u => u.Role == UserRole.Admin, ct);
            if (adminCount <= 1)
            {
                throw AppException.Conflict("LAST_ADMIN", "Phải còn ít nhất một quản trị viên.");
            }
        }

        user.Role = newRole;
        user.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        var bidCount = await _db.Bids.CountAsync(b => b.BidderId == userId, ct);
        return new AdminUserDto(user.Id, user.Email, user.DisplayName,
            user.Role == UserRole.Admin ? "admin" : "bidder", user.IsActive, user.CreatedAtUtc, bidCount);
    }

    public async Task<AdminUserDto> UpdateUserStatusAsync(Guid userId, bool isActive, Guid actorId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw AppException.NotFound("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        if (user.Id == actorId && !isActive)
        {
            throw AppException.Conflict("CANNOT_DISABLE_SELF", "Bạn không thể tự khóa tài khoản của chính mình.");
        }

        user.IsActive = isActive;
        user.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        var bidCount = await _db.Bids.CountAsync(b => b.BidderId == userId, ct);
        return new AdminUserDto(user.Id, user.Email, user.DisplayName,
            user.Role == UserRole.Admin ? "admin" : "bidder", user.IsActive, user.CreatedAtUtc, bidCount);
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        string? eventType, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Pagination.Normalize(page, 25, 100);
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType) && !eventType.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(a => a.EventType == eventType);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(a.Id, a.EventId, a.EventType, a.AuctionId, a.UserId,
                a.Payload, a.OccurredAtUtc, a.ProcessedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }
}
