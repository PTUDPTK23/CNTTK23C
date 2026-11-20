using System.Text.Json;
using Auction.Contracts;

namespace Auction.Tests;

public sealed class EventContractTests
{
    [Fact]
    public void BidAccepted_event_round_trips_with_camel_case_json()
    {
        var original = new BidAcceptedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Alice Bidder",
            1_250.50m,
            1_200m,
            1_250.50m,
            1_300.50m,
            "Midnight Noir",
            new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc),
            7);

        var json = EventSerialization.Serialize(original);

        Assert.Contains("\"eventId\"", json);
        Assert.Contains("\"bidderDisplayName\"", json);
        Assert.DoesNotContain("\"EventId\"", json);

        var roundTripped = EventSerialization.Deserialize<BidAcceptedEvent>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(original.EventId, roundTripped!.EventId);
        Assert.Equal(original.AuctionId, roundTripped.AuctionId);
        Assert.Equal(original.Amount, roundTripped.Amount);
        Assert.Equal(original.MinimumNextBid, roundTripped.MinimumNextBid);
        Assert.Equal(original.Sequence, roundTripped.Sequence);
        Assert.Equal(original.OccurredAtUtc, roundTripped.OccurredAtUtc);
    }

    [Fact]
    public void AuctionEnded_event_round_trips_including_null_winner()
    {
        var original = new AuctionEndedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Unsold lot",
            null,
            null,
            null,
            null,
            0,
            new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc));

        var roundTripped = EventSerialization.Deserialize<AuctionEndedEvent>(
            EventSerialization.Serialize(original));

        Assert.NotNull(roundTripped);
        Assert.Null(roundTripped!.WinnerId);
        Assert.Equal(0, roundTripped.TotalBids);
        Assert.Equal(original.AuctionId, roundTripped.AuctionId);
    }

    [Fact]
    public void Routing_keys_match_the_declared_topology()
    {
        Assert.Equal("auction.events", RabbitTopology.Exchange);
        Assert.Equal("auction.events.dlx", RabbitTopology.DeadLetterExchange);
        Assert.Equal("bid.accepted", RabbitTopology.BidAcceptedRoutingKey);
        Assert.Equal("auction.ended", RabbitTopology.AuctionEndedRoutingKey);
        Assert.Equal("auction.hub", RabbitTopology.HubQueue);
        Assert.Equal("auction.audit", RabbitTopology.AuditQueue);
    }

    [Fact]
    public void Event_types_are_stable_wire_contracts()
    {
        Assert.Equal("BidAccepted", EventTypes.BidAccepted);
        Assert.Equal("AuctionEnded", EventTypes.AuctionEnded);
    }

    [Fact]
    public void Case_insensitive_deserialization_supports_legacy_payloads()
    {
        var json = JsonSerializer.Serialize(new
        {
            EventId = Guid.NewGuid(),
            AuctionId = Guid.NewGuid(),
            AuctionTitle = "Legacy lot",
            WinnerId = (Guid?)null,
            WinnerDisplayName = (string?)null,
            WinningBidId = (Guid?)null,
            WinningAmount = (decimal?)null,
            TotalBids = 3,
            EndedAtUtc = DateTime.UtcNow
        });

        var message = EventSerialization.Deserialize<AuctionEndedEvent>(json);

        Assert.NotNull(message);
        Assert.Equal("Legacy lot", message!.AuctionTitle);
        Assert.Equal(3, message.TotalBids);
    }
}
