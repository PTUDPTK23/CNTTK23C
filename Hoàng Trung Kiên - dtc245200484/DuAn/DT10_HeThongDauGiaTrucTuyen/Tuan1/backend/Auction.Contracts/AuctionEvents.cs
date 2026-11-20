namespace Auction.Contracts;

/// <summary>Routing/topology constants shared by the publisher and every consumer.</summary>
public static class RabbitTopology
{
    public const string Exchange = "auction.events";
    public const string DeadLetterExchange = "auction.events.dlx";

    public const string BidAcceptedRoutingKey = "bid.accepted";
    public const string AuctionEndedRoutingKey = "auction.ended";

    public const string HubQueue = "auction.hub";
    public const string AuditQueue = "auction.audit";
    public const string HubDeadLetterQueue = "auction.hub.dlq";
    public const string AuditDeadLetterQueue = "auction.audit.dlq";
}

public sealed record BidAcceptedEvent(
    Guid EventId,
    Guid AuctionId,
    Guid BidId,
    Guid BidderId,
    string BidderDisplayName,
    decimal Amount,
    decimal PreviousPrice,
    decimal CurrentPrice,
    decimal MinimumNextBid,
    string AuctionTitle,
    DateTime OccurredAtUtc,
    int Sequence);

public sealed record AuctionEndedEvent(
    Guid EventId,
    Guid AuctionId,
    string AuctionTitle,
    Guid? WinnerId,
    string? WinnerDisplayName,
    Guid? WinningBidId,
    decimal? WinningAmount,
    int TotalBids,
    DateTime EndedAtUtc);

public static class EventTypes
{
    public const string BidAccepted = "BidAccepted";
    public const string AuctionEnded = "AuctionEnded";
}
