using Auction.Domain.Rules;

namespace Auction.Application.Abstractions;

public sealed record BidValidationOutcome(
    bool IsValid,
    string Code,
    string Message,
    decimal CurrentPrice,
    decimal MinimumNextBid,
    DateTime ServerTimeUtc,
    string AuctionStatus = "")
{
    public static BidValidationOutcome FromRule(BidValidationResult result, decimal currentPrice, DateTime serverTime) =>
        new(result.IsValid, result.Code, result.Message, currentPrice, result.MinimumNextBid, serverTime);
}

/// <summary>
/// Client abstraction for the gRPC Bid Validation Service (never implemented with HTTP in production).
/// </summary>
public interface IBidValidationClient
{
    Task<BidValidationOutcome> ValidateAsync(
        Guid auctionId,
        Guid bidderId,
        decimal amount,
        CancellationToken cancellationToken = default);
}

/// <summary>Wakes the outbox publisher right after a committed business transaction.</summary>
public interface IOutboxSignal
{
    void Notify();
}
