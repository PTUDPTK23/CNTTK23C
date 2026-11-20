using Auction.Application.Abstractions;
using Auction.Domain.Enums;
using Auction.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Auction.Application.Services;

/// <summary>
/// Read-only evaluation used by the gRPC Bid Validation Service.
/// It is intentionally independent from the API write path; the API re-checks inside its lock.
/// </summary>
public sealed class BidValidationEvaluator
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public BidValidationEvaluator(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<BidValidationOutcome> EvaluateAsync(
        Guid auctionId, Guid bidderId, decimal amount, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        var auction = await _db.Auctions.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == auctionId, ct);

        if (auction is null)
        {
            return new BidValidationOutcome(false, BidErrorCodes.AuctionNotFound,
                "Phiên đấu giá không tồn tại.", 0m, 0m, now);
        }

        var bidder = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == bidderId, ct);

        var highest = await _db.Bids.AsNoTracking()
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.Amount)
            .Select(b => (decimal?)b.Amount)
            .FirstOrDefaultAsync(ct);

        var currentPrice = Math.Max(auction.StartPrice, Math.Max(auction.CurrentPrice, highest ?? 0m));

        var snapshot = new AuctionSnapshot(
            auction.Id, true, auction.Status, auction.StartTimeUtc, auction.EndTimeUtc,
            auction.StartPrice, currentPrice, auction.MinIncrement, auction.CreatedByUserId);

        var bidderSnapshot = bidder is null
            ? new BidderSnapshot(bidderId, false, false, UserRole.Bidder, "")
            : new BidderSnapshot(bidder.Id, true, bidder.IsActive, bidder.Role, bidder.DisplayName);

        var result = BidRules.Validate(snapshot, bidderSnapshot, amount, now);
        return BidValidationOutcome.FromRule(result, currentPrice, now)
            with { AuctionStatus = auction.Status.ToString().ToLowerInvariant() };
    }
}
