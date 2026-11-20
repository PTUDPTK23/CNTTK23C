using Auction.Application.Abstractions;
using Auction.Application.Common;
using Auction.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Auction.Application.Services;

public sealed class UserService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public UserService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw AppException.NotFound("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        var totalBids = await _db.Bids.CountAsync(b => b.BidderId == userId, ct);
        var auctionsJoined = await _db.Participants.CountAsync(p => p.UserId == userId, ct);
        var auctionsWon = await _db.Auctions.CountAsync(a => a.WinnerId == userId, ct);

        return new UserProfileDto(user.Id, user.Email, user.DisplayName, user.Role.ToApi(),
            user.CreatedAtUtc, totalBids, auctionsJoined, auctionsWon);
    }

    public async Task<PagedResult<BidDto>> GetMyBidsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Pagination.Normalize(page, 20);
        var query = _db.Bids.AsNoTracking().Where(b => b.BidderId == userId);
        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(b => b.PlacedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BidDto(b.Id, b.AuctionId, b.Auction.Title, b.Amount, b.BidderId,
                b.Bidder.DisplayName, b.PlacedAtUtc, b.Sequence, b.IsWinning))
            .ToListAsync(ct);

        return new PagedResult<BidDto>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<AuctionSummaryDto>> GetMyAuctionsAsync(Guid userId, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        return await _db.Participants.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.Auction)
            .OrderByDescending(a => a.EndTimeUtc)
            .Select(AuctionQueries.Summary(now))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AuctionSummaryDto>> GetMyWinsAsync(Guid userId, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        return await _db.Auctions.AsNoTracking()
            .Where(a => a.WinnerId == userId)
            .OrderByDescending(a => a.EndedAtUtc)
            .Select(AuctionQueries.Summary(now))
            .ToListAsync(ct);
    }
}
