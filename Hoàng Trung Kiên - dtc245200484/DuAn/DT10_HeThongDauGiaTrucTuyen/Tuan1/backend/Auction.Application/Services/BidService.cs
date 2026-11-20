using System.Text.Json;
using Auction.Application.Abstractions;
using Auction.Application.Common;
using Auction.Application.Dtos;
using Auction.Contracts;
using Auction.Domain.Entities;
using Auction.Domain.Enums;
using Auction.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Auction.Application.Services;

public sealed class BidService
{
    private readonly IAppDbContext _db;
    private readonly IBidValidationClient _validator;
    private readonly IOutboxSignal _outbox;
    private readonly IClock _clock;

    public BidService(IAppDbContext db, IBidValidationClient validator, IOutboxSignal outbox, IClock clock)
    {
        _db = db;
        _validator = validator;
        _outbox = outbox;
        _clock = clock;
    }

    public async Task<PlaceBidResponse> PlaceBidAsync(
        Guid auctionId, Guid bidderId, decimal amount, CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            throw AppException.BadRequest(BidErrorCodes.InvalidAmount, "Giá đặt phải lớn hơn 0.");
        }

        amount = BidRules.RoundMoney(amount);

        // 1) Mandatory pre-validation through the independent gRPC Bid Validation Service.
        var preValidation = await _validator.ValidateAsync(auctionId, bidderId, amount, ct);
        if (!preValidation.IsValid)
        {
            throw ToApiException(preValidation, auctionId);
        }

        // 2) Authoritative write transaction. The auction row is locked FOR UPDATE so two
        //    concurrent bidders can never both be accepted against the same current price.
        await using var tx = await _db.BeginTransactionAsync(ct);

        await _db.LockAuctionAsync(auctionId, ct);

        var auction = await _db.Auctions
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.Id == auctionId, ct)
            ?? throw AppException.NotFound("AUCTION_NOT_FOUND", "Không tìm thấy phiên đấu giá.");

        var now = _clock.UtcNow;

        // Scheduler may not have flipped the status yet (1s tick); do it opportunistically.
        if (auction.Status == AuctionStatus.Scheduled && auction.StartTimeUtc <= now && auction.EndTimeUtc > now)
        {
            auction.Status = AuctionStatus.Active;
        }

        var bidder = await _db.Users.FirstOrDefaultAsync(u => u.Id == bidderId, ct);
        var highestAmount = await _db.Bids
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.Amount)
            .Select(b => (decimal?)b.Amount)
            .FirstOrDefaultAsync(ct);

        var currentPrice = Math.Max(auction.StartPrice, Math.Max(auction.CurrentPrice, highestAmount ?? 0m));

        var snapshot = new AuctionSnapshot(
            auction.Id, true, auction.Status, auction.StartTimeUtc, auction.EndTimeUtc,
            auction.StartPrice, currentPrice, auction.MinIncrement, auction.CreatedByUserId);

        var bidderSnapshot = bidder is null
            ? null
            : new BidderSnapshot(bidder.Id, true, bidder.IsActive, bidder.Role, bidder.DisplayName);

        var rules = BidRules.Validate(snapshot, bidderSnapshot, amount, now);
        if (!rules.IsValid)
        {
            throw ToApiException(
                new BidValidationOutcome(false, rules.Code, rules.Message, currentPrice, rules.MinimumNextBid, now),
                auctionId);
        }

        var lastSequence = await _db.Bids
            .Where(b => b.AuctionId == auctionId)
            .MaxAsync(b => (int?)b.Sequence, ct);

        var sequence = (lastSequence ?? 0) + 1;
        var bid = new Bid
        {
            AuctionId = auction.Id,
            BidderId = bidder!.Id,
            Amount = amount,
            Sequence = sequence,
            PlacedAtUtc = now,
            EventId = Guid.NewGuid()
        };

        auction.CurrentPrice = amount;
        auction.Version += 1;
        auction.UpdatedAtUtc = now;

        _db.Bids.Add(bid);

        var participant = await _db.Participants
            .FirstOrDefaultAsync(p => p.AuctionId == auctionId && p.UserId == bidder.Id, ct);

        if (participant is null)
        {
            participant = new AuctionParticipant
            {
                AuctionId = auctionId,
                UserId = bidder.Id,
                JoinedAtUtc = now,
                LastBidAtUtc = now,
                BidCount = 1
            };
            _db.Participants.Add(participant);
        }
        else
        {
            participant.LastBidAtUtc = now;
            participant.BidCount += 1;
        }

        var acceptedEvent = new BidAcceptedEvent(
            bid.EventId,
            auction.Id,
            bid.Id,
            bidder.Id,
            bidder.DisplayName,
            amount,
            currentPrice,
            amount,
            amount + auction.MinIncrement,
            auction.Title,
            now,
            sequence);

        _db.OutboxMessages.Add(new OutboxMessage
        {
            EventId = acceptedEvent.EventId,
            EventType = EventTypes.BidAccepted,
            RoutingKey = RabbitTopology.BidAcceptedRoutingKey,
            Payload = EventSerialization.Serialize(acceptedEvent),
            OccurredAtUtc = now
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Wake the outbox dispatcher; the bid is already durable even if the broker is down.
        _outbox.Notify();

        return new PlaceBidResponse(
            bid.Id,
            auction.Id,
            amount,
            amount,
            amount + auction.MinIncrement,
            bidder.DisplayName,
            now,
            "Lượt đặt giá của bạn đã được chấp nhận.");
    }

    private static AppException ToApiException(BidValidationOutcome outcome, Guid auctionId)
    {
        var details = new
        {
            auctionId,
            outcome.Code,
            currentPrice = outcome.CurrentPrice,
            minimumNextBid = outcome.MinimumNextBid,
            serverTimeUtc = outcome.ServerTimeUtc
        };

        return outcome.Code switch
        {
            BidErrorCodes.AuctionNotFound => AppException.NotFound(outcome.Code, outcome.Message),
            BidErrorCodes.BidderNotFound => AppException.NotFound(outcome.Code, outcome.Message),
            BidErrorCodes.InvalidAmount => AppException.BadRequest(outcome.Code, outcome.Message),
            BidErrorCodes.BidderInactive or BidErrorCodes.SelfBidNotAllowed =>
                new AppException(403, outcome.Code, outcome.Message, details),
            _ => new AppException(409, outcome.Code, outcome.Message, details)
        };
    }
}
