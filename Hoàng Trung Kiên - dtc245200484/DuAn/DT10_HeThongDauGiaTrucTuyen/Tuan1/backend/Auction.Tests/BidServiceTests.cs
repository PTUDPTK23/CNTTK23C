using Auction.Application.Abstractions;
using Auction.Application.Common;
using Auction.Application.Services;
using Auction.Contracts;
using Auction.Domain.Entities;
using Auction.Domain.Enums;
using Auction.Domain.Rules;
using Auction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AuctionEntity = Auction.Domain.Entities.Auction;

namespace Auction.Tests;

[Collection("postgres")]
public sealed class BidServiceTests
{
    private static readonly DateTime Now = new(2026, 4, 10, 15, 0, 0, DateTimeKind.Utc);

    private readonly PostgresTestFixture _fixture;

    public BidServiceTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
        Xunit.Assert.True(_fixture.Available, $"PostgreSQL test fixture unavailable: {_fixture.UnavailableReason}");
    }

    private sealed record Seed(
        Guid AdminId,
        Guid AliceId,
        Guid BobId,
        Guid CarolId,
        Guid AuctionId,
        Guid SelfAuctionId,
        Guid ClosedAuctionId);

    private static async Task<Seed> SeedAsync(AuctionDbContext db)
    {
        var admin = new User
        {
            Email = "admin@test.local",
            NormalizedEmail = "ADMIN@TEST.LOCAL",
            DisplayName = "House Admin",
            PasswordHash = "hash",
            Role = UserRole.Admin
        };

        var alice = new User
        {
            Email = "alice@test.local",
            NormalizedEmail = "ALICE@TEST.LOCAL",
            DisplayName = "Alice Bidder",
            PasswordHash = "hash",
            Role = UserRole.Bidder
        };

        var bob = new User
        {
            Email = "bob@test.local",
            NormalizedEmail = "BOB@TEST.LOCAL",
            DisplayName = "Bob Bidder",
            PasswordHash = "hash",
            Role = UserRole.Bidder
        };

        var carol = new User
        {
            Email = "carol@test.local",
            NormalizedEmail = "CAROL@TEST.LOCAL",
            DisplayName = "Carol Disabled",
            PasswordHash = "hash",
            Role = UserRole.Bidder,
            IsActive = false
        };

        var product = new Product
        {
            Name = "Test Watch",
            Description = "A test product used by the PostgreSQL integration suite.",
            Category = "Watches",
            ImageUrl = "/media/watch.svg",
            StartingPrice = 1_000m
        };

        var auction = new AuctionEntity
        {
            Product = product,
            ProductId = product.Id,
            Title = "Active test auction",
            StartPrice = 1_000m,
            CurrentPrice = 1_000m,
            MinIncrement = 100m,
            StartTimeUtc = Now.AddHours(-1),
            EndTimeUtc = Now.AddHours(1),
            Status = AuctionStatus.Active,
            CreatedByUserId = admin.Id,
            CreatedAtUtc = Now.AddHours(-2),
            UpdatedAtUtc = Now.AddHours(-2)
        };

        var selfAuction = new AuctionEntity
        {
            Product = product,
            ProductId = product.Id,
            Title = "Consignor's own auction",
            StartPrice = 500m,
            CurrentPrice = 500m,
            MinIncrement = 50m,
            StartTimeUtc = Now.AddHours(-1),
            EndTimeUtc = Now.AddHours(1),
            Status = AuctionStatus.Active,
            CreatedByUserId = alice.Id,
            CreatedAtUtc = Now.AddHours(-2),
            UpdatedAtUtc = Now.AddHours(-2)
        };

        var closedAuction = new AuctionEntity
        {
            Product = product,
            ProductId = product.Id,
            Title = "Closed test auction",
            StartPrice = 2_000m,
            CurrentPrice = 2_000m,
            MinIncrement = 100m,
            StartTimeUtc = Now.AddHours(-3),
            EndTimeUtc = Now.AddHours(-1),
            Status = AuctionStatus.Active,
            CreatedByUserId = admin.Id,
            CreatedAtUtc = Now.AddHours(-4),
            UpdatedAtUtc = Now.AddHours(-4)
        };

        db.Users.AddRange(admin, alice, bob, carol);
        db.Products.Add(product);
        db.Auctions.AddRange(auction, selfAuction, closedAuction);
        await db.SaveChangesAsync();

        return new Seed(admin.Id, alice.Id, bob.Id, carol.Id, auction.Id, selfAuction.Id, closedAuction.Id);
    }

    private static (BidService Service, NoopOutbox Outbox) BuildService(AuctionDbContext db) =>
        (new BidService(db, new AlwaysValidValidator(), new NoopOutbox(), new FixedClock(Now)), new NoopOutbox());

    private sealed class AlwaysValidValidator : IBidValidationClient
    {
        public Task<BidValidationOutcome> ValidateAsync(Guid auctionId, Guid bidderId, decimal amount,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BidValidationOutcome(true, "OK", "Validated by test pre-check.", 0m, 0m, Now));
    }

    private sealed class NoopOutbox : IOutboxSignal
    {
        public int Notifications { get; private set; }
        public void Notify() => Notifications++;
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime now) => UtcNow = now;
        public DateTime UtcNow { get; }
    }

    private static async Task<AppException> CaptureFailureAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (AppException ex)
        {
            return ex;
        }

        throw new Xunit.Sdk.XunitException("Expected an AppException, but the call succeeded.");
    }

    [Fact]
    public async Task Accepted_bid_persists_bid_participant_and_outbox_together()
    {
        if (!_fixture.Available) return;
        await _fixture.ResetAsync();

        await using var db = _fixture.CreateContext();
        var seed = await SeedAsync(db);

        var outbox = new NoopOutbox();
        var service = new BidService(db, new AlwaysValidValidator(), outbox, new FixedClock(Now));

        var response = await service.PlaceBidAsync(seed.AuctionId, seed.AliceId, 1_100m);

        Assert.Equal(1_100m, response.Amount);
        Assert.Equal(1_100m, response.CurrentPrice);
        Assert.Equal(1_200m, response.MinimumNextBid);
        Assert.Equal("Alice Bidder", response.BidderName);
        Assert.Equal(1, outbox.Notifications);

        db.ChangeTracker.Clear();

        var auction = await db.Auctions.SingleAsync(a => a.Id == seed.AuctionId);
        Assert.Equal(1_100m, auction.CurrentPrice);
        Assert.Equal(1, auction.Version);

        var bid = await db.Bids.SingleAsync();
        Assert.Equal(1_100m, bid.Amount);
        Assert.Equal(1, bid.Sequence);
        Assert.Equal(seed.AliceId, bid.BidderId);
        Assert.NotEqual(Guid.Empty, bid.EventId);

        var participant = await db.Participants.SingleAsync();
        Assert.Equal(seed.AliceId, participant.UserId);
        Assert.Equal(1, participant.BidCount);

        var outboxMessage = await db.OutboxMessages.SingleAsync();
        Assert.Equal(EventTypes.BidAccepted, outboxMessage.EventType);
        Assert.Equal(RabbitTopology.BidAcceptedRoutingKey, outboxMessage.RoutingKey);
        Assert.Equal(bid.EventId, outboxMessage.EventId);
        Assert.Null(outboxMessage.PublishedAtUtc);

        var published = EventSerialization.Deserialize<BidAcceptedEvent>(outboxMessage.Payload);
        Assert.NotNull(published);
        Assert.Equal(seed.AuctionId, published!.AuctionId);
        Assert.Equal(bid.Id, published.BidId);
        Assert.Equal(1_100m, published.Amount);
        Assert.Equal(1, published.Sequence);
    }

    [Fact]
    public async Task Sequential_bids_increment_price_sequence_and_participant_counts()
    {
        if (!_fixture.Available) return;
        await _fixture.ResetAsync();

        await using var db = _fixture.CreateContext();
        var seed = await SeedAsync(db);
        var service = new BidService(db, new AlwaysValidValidator(), new NoopOutbox(), new FixedClock(Now));

        var first = await service.PlaceBidAsync(seed.AuctionId, seed.AliceId, 1_100m);
        var second = await service.PlaceBidAsync(seed.AuctionId, seed.BobId, 1_200m);

        Assert.Equal(1_200m, first.MinimumNextBid);
        Assert.Equal(1_300m, second.MinimumNextBid);

        db.ChangeTracker.Clear();

        var bids = await db.Bids.OrderBy(b => b.Sequence).ToListAsync();
        Assert.Equal(2, bids.Count);
        Assert.Equal(new[] { 1, 2 }, bids.Select(b => b.Sequence).ToArray());

        var participants = await db.Participants.OrderBy(p => p.UserId).ToListAsync();
        Assert.Equal(2, participants.Count);
        Assert.All(participants, p => Assert.Equal(1, p.BidCount));

        var auction = await db.Auctions.SingleAsync(a => a.Id == seed.AuctionId);
        Assert.Equal(1_200m, auction.CurrentPrice);
        Assert.Equal(2, auction.Version);
    }

    [Fact]
    public async Task Concurrent_bids_at_the_same_price_accept_exactly_one()
    {
        if (!_fixture.Available) return;
        await _fixture.ResetAsync();

        Guid auctionId;
        Guid aliceId;
        Guid bobId;

        await using (var seedDb = _fixture.CreateContext())
        {
            var seed = await SeedAsync(seedDb);
            auctionId = seed.AuctionId;
            aliceId = seed.AliceId;
            bobId = seed.BobId;
        }

        await using var db1 = _fixture.CreateContext();
        await using var db2 = _fixture.CreateContext();

        var service1 = new BidService(db1, new AlwaysValidValidator(), new NoopOutbox(), new FixedClock(Now));
        var service2 = new BidService(db2, new AlwaysValidValidator(), new NoopOutbox(), new FixedClock(Now));

        async Task<Exception?> TryBidAsync(BidService service, Guid bidderId)
        {
            try
            {
                await service.PlaceBidAsync(auctionId, bidderId, 1_100m);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        var results = await Task.WhenAll(
            TryBidAsync(service1, aliceId),
            TryBidAsync(service2, bobId));

        Assert.Single(results, r => r is null);
        var failure = Assert.Single(results, r => r is not null)!;
        var appException = Assert.IsType<AppException>(failure);
        Assert.Equal(409, appException.StatusCode);
        Assert.True(appException.Code is BidErrorCodes.BidTooLow or BidErrorCodes.BidStepTooSmall,
            $"Unexpected rejection code: {appException.Code}");

        await using var verify = _fixture.CreateContext();
        var bids = await verify.Bids.ToListAsync();
        Assert.Single(bids);
        Assert.Equal(1_100m, bids[0].Amount);

        var auction = await verify.Auctions.SingleAsync(a => a.Id == auctionId);
        Assert.Equal(1_100m, auction.CurrentPrice);
        Assert.Single(await verify.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task Bid_below_the_increment_is_rejected_even_if_the_prevalidator_approves()
    {
        if (!_fixture.Available) return;
        await _fixture.ResetAsync();

        await using var db = _fixture.CreateContext();
        var seed = await SeedAsync(db);
        var service = new BidService(db, new AlwaysValidValidator(), new NoopOutbox(), new FixedClock(Now));

        var error = await CaptureFailureAsync(() => service.PlaceBidAsync(seed.AuctionId, seed.AliceId, 1_050m));

        Assert.Equal(409, error.StatusCode);
        Assert.Equal(BidErrorCodes.BidStepTooSmall, error.Code);

        db.ChangeTracker.Clear();
        Assert.Empty(await db.Bids.ToListAsync());
        Assert.Empty(await db.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task Administrator_accounts_are_forbidden_from_bidding()
    {
        if (!_fixture.Available) return;
        await _fixture.ResetAsync();

        await using var db = _fixture.CreateContext();
        var seed = await SeedAsync(db);
        var service = new BidService(db, new AlwaysValidValidator(), new NoopOutbox(), new FixedClock(Now));

        var error = await CaptureFailureAsync(() => service.PlaceBidAsync(seed.AuctionId, seed.AdminId, 1_100m));

        Assert.Equal(403, error.StatusCode);
        Assert.Equal(BidErrorCodes.SelfBidNotAllowed, error.Code);
    }

    [Fact]
    public async Task Disabled_bidder_is_forbidden()
    {
        if (!_fixture.Available) return;
        await _fixture.ResetAsync();

        await using var db = _fixture.CreateContext();
        var seed = await SeedAsync(db);
        var service = new BidService(db, new AlwaysValidValidator(), new NoopOutbox(), new FixedClock(Now));

        var error = await CaptureFailureAsync(() => service.PlaceBidAsync(seed.AuctionId, seed.CarolId, 1_100m));

        Assert.Equal(403, error.StatusCode);
        Assert.Equal(BidErrorCodes.BidderInactive, error.Code);
    }

    [Fact]
    public async Task Bid_after_the_closing_time_is_rejected()
    {
        if (!_fixture.Available) return;
        await _fixture.ResetAsync();

        await using var db = _fixture.CreateContext();
        var seed = await SeedAsync(db);
        var service = new BidService(db, new AlwaysValidValidator(), new NoopOutbox(), new FixedClock(Now));

        var error = await CaptureFailureAsync(() => service.PlaceBidAsync(seed.ClosedAuctionId, seed.AliceId, 2_100m));

        Assert.Equal(409, error.StatusCode);
        Assert.Equal(BidErrorCodes.AuctionEnded, error.Code);
    }

    [Fact]
    public async Task Consignor_cannot_bid_on_their_own_auction()
    {
        if (!_fixture.Available) return;
        await _fixture.ResetAsync();

        await using var db = _fixture.CreateContext();
        var seed = await SeedAsync(db);
        var service = new BidService(db, new AlwaysValidValidator(), new NoopOutbox(), new FixedClock(Now));

        var error = await CaptureFailureAsync(() => service.PlaceBidAsync(seed.SelfAuctionId, seed.AliceId, 550m));

        Assert.Equal(403, error.StatusCode);
        Assert.Equal(BidErrorCodes.SelfBidNotAllowed, error.Code);
    }

    [Fact]
    public async Task Unknown_auction_returns_not_found()
    {
        if (!_fixture.Available) return;
        await _fixture.ResetAsync();

        await using var db = _fixture.CreateContext();
        var seed = await SeedAsync(db);
        var service = new BidService(db, new AlwaysValidValidator(), new NoopOutbox(), new FixedClock(Now));

        var error = await CaptureFailureAsync(() => service.PlaceBidAsync(Guid.NewGuid(), seed.AliceId, 1_100m));

        Assert.Equal(404, error.StatusCode);
        Assert.Equal(BidErrorCodes.AuctionNotFound, error.Code);
    }

    [Fact]
    public async Task Zero_amount_is_rejected_before_validation()
    {
        if (!_fixture.Available) return;
        await _fixture.ResetAsync();

        await using var db = _fixture.CreateContext();
        var seed = await SeedAsync(db);
        var service = new BidService(db, new AlwaysValidValidator(), new NoopOutbox(), new FixedClock(Now));

        var error = await CaptureFailureAsync(() => service.PlaceBidAsync(seed.AuctionId, seed.AliceId, 0m));

        Assert.Equal(400, error.StatusCode);
        Assert.Equal(BidErrorCodes.InvalidAmount, error.Code);
    }
}
