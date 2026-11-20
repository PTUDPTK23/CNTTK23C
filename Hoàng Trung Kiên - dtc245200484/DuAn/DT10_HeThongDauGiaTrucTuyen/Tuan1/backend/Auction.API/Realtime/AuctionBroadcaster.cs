using Auction.API.Hubs;
using Auction.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Auction.API.Realtime;

public sealed record BidAcceptedMessage(
    Guid AuctionId,
    Guid BidId,
    Guid BidderId,
    string BidderName,
    decimal Amount,
    decimal PreviousPrice,
    decimal CurrentPrice,
    decimal MinimumNextBid,
    DateTime PlacedAtUtc,
    int Sequence,
    string AuctionTitle);

public sealed record AuctionEndedMessage(
    Guid AuctionId,
    string AuctionTitle,
    Guid? WinnerId,
    string? WinnerName,
    Guid? WinningBidId,
    decimal? WinningAmount,
    int TotalBids,
    DateTime EndedAtUtc);

public sealed class AuctionBroadcaster
{
    private readonly IHubContext<AuctionHub> _hub;

    public AuctionBroadcaster(IHubContext<AuctionHub> hub)
    {
        _hub = hub;
    }

    public Task BroadcastBidAcceptedAsync(BidAcceptedEvent evt) =>
        _hub.Clients.Group(AuctionHub.GroupName(evt.AuctionId)).SendAsync("BidAccepted",
            new BidAcceptedMessage(
                evt.AuctionId,
                evt.BidId,
                evt.BidderId,
                evt.BidderDisplayName,
                evt.Amount,
                evt.PreviousPrice,
                evt.CurrentPrice,
                evt.MinimumNextBid,
                evt.OccurredAtUtc,
                evt.Sequence,
                evt.AuctionTitle));

    public Task BroadcastAuctionEndedAsync(AuctionEndedEvent evt) =>
        _hub.Clients.Group(AuctionHub.GroupName(evt.AuctionId)).SendAsync("AuctionEnded",
            new AuctionEndedMessage(
                evt.AuctionId,
                evt.AuctionTitle,
                evt.WinnerId,
                evt.WinnerDisplayName,
                evt.WinningBidId,
                evt.WinningAmount,
                evt.TotalBids,
                evt.EndedAtUtc));
}
