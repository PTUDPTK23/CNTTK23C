using Auction.Application.Abstractions;
using Auction.Application.Common;
using Auction.Contracts;
using Auction.Domain.Entities;
using Auction.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auction.Application.Services;

/// <summary>
/// Activates scheduled auctions and closes expired ones. Every close is a single
/// serialized transaction that determines the winner and emits AuctionEnded.
/// </summary>
public sealed class AuctionLifecycleService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IOutboxSignal _outbox;
    private readonly ILogger<AuctionLifecycleService> _logger;

    public AuctionLifecycleService(IAppDbContext db, IClock clock, IOutboxSignal outbox,
        ILogger<AuctionLifecycleService> logger)
    {
        _db = db;
        _clock = clock;
        _outbox = outbox;
        _logger = logger;
    }

    public async Task<int> ProcessDueAuctionsAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        var activated = await _db.Auctions
            .Where(a => a.Status == AuctionStatus.Scheduled && a.StartTimeUtc <= now && a.EndTimeUtc > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Status, AuctionStatus.Active)
                .SetProperty(a => a.UpdatedAtUtc, now), ct);

        if (activated > 0)
        {
            _logger.LogInformation("Activated {Count} scheduled auction(s).", activated);
        }

        var dueIds = await _db.Auctions.AsNoTracking()
            .Where(a => (a.Status == AuctionStatus.Active || a.Status == AuctionStatus.Scheduled)
                        && a.EndTimeUtc <= now)
            .OrderBy(a => a.EndTimeUtc)
            .Take(50)
            .Select(a => a.Id)
            .ToListAsync(ct);

        var ended = 0;
        foreach (var auctionId in dueIds)
        {
            if (await CloseAuctionAsync(auctionId, now, force: false, ct))
            {
                ended++;
            }
        }

        if (ended > 0)
        {
            _logger.LogInformation("Closed {Count} expired auction(s).", ended);
        }

        return ended;
    }

    public async Task EndAuctionManuallyAsync(Guid auctionId, CancellationToken ct = default)
    {
        var closed = await CloseAuctionAsync(auctionId, _clock.UtcNow, force: true, ct);
        if (!closed)
        {
            throw AppException.Conflict("AUCTION_NOT_ENDABLE", "Phiên đấu giá đã kết thúc hoặc đã bị hủy.");
        }
    }

    private async Task<bool> CloseAuctionAsync(Guid auctionId, DateTime now, bool force, CancellationToken ct)
    {
        await using var tx = await _db.BeginTransactionAsync(ct);

        await _db.LockAuctionAsync(auctionId, ct);

        var auction = await _db.Auctions.FirstOrDefaultAsync(a => a.Id == auctionId, ct);
        if (auction is null || auction.Status is AuctionStatus.Ended or AuctionStatus.Cancelled)
        {
            return false;
        }

        if (!force && auction.EndTimeUtc > now)
        {
            return false;
        }

        var winningBid = await _db.Bids
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.Amount)
            .ThenBy(b => b.Sequence)
            .FirstOrDefaultAsync(ct);

        var totalBids = await _db.Bids.CountAsync(b => b.AuctionId == auctionId, ct);

        string? winnerName = null;
        if (winningBid is not null)
        {
            winnerName = await _db.Users.AsNoTracking()
                .Where(u => u.Id == winningBid.BidderId)
                .Select(u => u.DisplayName)
                .FirstOrDefaultAsync(ct);

            await _db.Bids
                .Where(b => b.AuctionId == auctionId && b.Id != winningBid.Id && b.IsWinning)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsWinning, false), ct);

            winningBid.IsWinning = true;
            auction.WinnerId = winningBid.BidderId;
            auction.WinningBidId = winningBid.Id;
            auction.WinningAmount = winningBid.Amount;
            auction.CurrentPrice = winningBid.Amount;
        }

        auction.Status = AuctionStatus.Ended;
        auction.EndedAtUtc = now;
        auction.UpdatedAtUtc = now;
        auction.Version += 1;

        var endedAtUtc = AuctionService.NormalizeUtc(now);
        var endedEvent = new AuctionEndedEvent(
            Guid.NewGuid(),
            auction.Id,
            auction.Title,
            auction.WinnerId,
            winnerName,
            auction.WinningBidId,
            auction.WinningAmount,
            totalBids,
            endedAtUtc);

        _db.OutboxMessages.Add(new OutboxMessage
        {
            EventId = endedEvent.EventId,
            EventType = EventTypes.AuctionEnded,
            RoutingKey = RabbitTopology.AuctionEndedRoutingKey,
            Payload = EventSerialization.Serialize(endedEvent),
            OccurredAtUtc = endedAtUtc
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        _outbox.Notify();

        _logger.LogInformation("Auction {AuctionId} ended. Winner: {Winner}, Amount: {Amount}",
            auctionId, winnerName ?? "none", auction.WinningAmount);

        return true;
    }
}
