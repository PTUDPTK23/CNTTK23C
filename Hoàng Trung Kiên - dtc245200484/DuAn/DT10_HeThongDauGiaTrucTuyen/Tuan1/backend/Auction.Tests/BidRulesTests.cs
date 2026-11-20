using Auction.Domain.Enums;
using Auction.Domain.Rules;

namespace Auction.Tests;

/// <summary>
/// Pure rule tests. These run without any infrastructure and are the fastest guard
/// against bidding-policy regressions.
/// </summary>
public sealed class BidRulesTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

    private static AuctionSnapshot Auction(
        AuctionStatus status = AuctionStatus.Active,
        decimal start = 1_000m,
        decimal current = 1_000m,
        decimal increment = 100m,
        DateTime? startTime = null,
        DateTime? endTime = null,
        Guid? createdBy = null) =>
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            true,
            status,
            startTime ?? Now.AddHours(-1),
            endTime ?? Now.AddHours(1),
            start,
            current,
            increment,
            createdBy ?? Guid.Parse("99999999-9999-9999-9999-999999999999"));

    private static BidderSnapshot Bidder(
        bool exists = true,
        bool active = true,
        UserRole role = UserRole.Bidder,
        Guid? id = null) =>
        new(id ?? Guid.Parse("22222222-2222-2222-2222-222222222222"),
            exists, active, role, "Alice Bidder");

    [Fact]
    public void Exact_minimum_next_bid_is_accepted()
    {
        var result = BidRules.Validate(Auction(), Bidder(), 1_100m, Now);

        Assert.True(result.IsValid);
        Assert.Equal("OK", result.Code);
        Assert.Equal(1_100m, result.MinimumNextBid);
    }

    [Fact]
    public void Higher_bid_is_accepted_and_minimum_still_reported()
    {
        var result = BidRules.Validate(Auction(), Bidder(), 1_450m, Now);

        Assert.True(result.IsValid);
        Assert.Equal(1_100m, result.MinimumNextBid);
    }

    [Fact]
    public void Bid_equal_to_current_price_is_rejected_as_too_low()
    {
        var result = BidRules.Validate(Auction(), Bidder(), 1_000m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.BidTooLow, result.Code);
    }

    [Fact]
    public void Bid_above_current_but_below_increment_is_rejected()
    {
        var result = BidRules.Validate(Auction(), Bidder(), 1_050m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.BidStepTooSmall, result.Code);
        Assert.Equal(1_100m, result.MinimumNextBid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Non_positive_amounts_are_rejected(decimal amount)
    {
        var result = BidRules.Validate(Auction(), Bidder(), amount, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.InvalidAmount, result.Code);
    }

    [Fact]
    public void Three_decimal_scale_is_rejected()
    {
        var result = BidRules.Validate(Auction(), Bidder(), 1_100.001m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.InvalidAmount, result.Code);
    }

    [Fact]
    public void Two_decimal_scale_is_accepted()
    {
        var result = BidRules.Validate(Auction(), Bidder(), 1_100.01m, Now);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Scheduled_auction_rejects_bids_as_not_started()
    {
        var result = BidRules.Validate(
            Auction(AuctionStatus.Scheduled, startTime: Now.AddHours(1), endTime: Now.AddHours(2)),
            Bidder(), 1_100m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.AuctionNotStarted, result.Code);
    }

    [Fact]
    public void Active_status_with_future_start_rejects_bids_as_not_started()
    {
        var result = BidRules.Validate(
            Auction(startTime: Now.AddMinutes(5)), Bidder(), 1_100m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.AuctionNotStarted, result.Code);
    }

    [Fact]
    public void Closed_window_rejects_bids_as_ended()
    {
        var result = BidRules.Validate(
            Auction(endTime: Now.AddSeconds(-1)), Bidder(), 1_100m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.AuctionEnded, result.Code);
    }

    [Fact]
    public void Ended_status_rejects_bids()
    {
        var result = BidRules.Validate(Auction(AuctionStatus.Ended), Bidder(), 1_100m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.AuctionEnded, result.Code);
    }

    [Fact]
    public void Cancelled_auction_rejects_bids()
    {
        var result = BidRules.Validate(Auction(AuctionStatus.Cancelled), Bidder(), 1_100m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.AuctionNotActive, result.Code);
    }

    [Fact]
    public void Missing_auction_is_rejected()
    {
        var result = BidRules.Validate(null, Bidder(), 1_100m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.AuctionNotFound, result.Code);
    }

    [Fact]
    public void Missing_bidder_is_rejected()
    {
        var result = BidRules.Validate(Auction(), Bidder(exists: false), 1_100m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.BidderNotFound, result.Code);
    }

    [Fact]
    public void Inactive_bidder_is_rejected()
    {
        var result = BidRules.Validate(Auction(), Bidder(active: false), 1_100m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.BidderInactive, result.Code);
    }

    [Fact]
    public void Administrator_cannot_bid()
    {
        var result = BidRules.Validate(Auction(), Bidder(role: UserRole.Admin), 1_100m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.SelfBidNotAllowed, result.Code);
    }

    [Fact]
    public void Consignor_cannot_bid_on_own_auction()
    {
        var bidderId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var result = BidRules.Validate(
            Auction(createdBy: bidderId), Bidder(id: bidderId), 1_100m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.SelfBidNotAllowed, result.Code);
    }

    [Fact]
    public void Missing_current_price_falls_back_to_start_price()
    {
        var result = BidRules.Validate(Auction(start: 2_000m, current: 0m, increment: 250m), Bidder(), 2_250m, Now);

        Assert.True(result.IsValid);
        Assert.Equal(2_250m, result.MinimumNextBid);
    }

    [Theory]
    [InlineData(10.004, 10.0)]
    [InlineData(10.005, 10.01)]
    [InlineData(10.015, 10.02)]
    public void Money_rounding_uses_away_from_zero(decimal input, decimal expected)
    {
        Assert.Equal(expected, BidRules.RoundMoney(input));
    }

    [Theory]
    [InlineData(10.1, true)]
    [InlineData(10.12, true)]
    [InlineData(10.123, false)]
    public void Scale_validation_accepts_at_most_two_decimals(decimal value, bool expected)
    {
        Assert.Equal(expected, BidRules.HasValidScale(value));
    }

    [Fact]
    public void Rejection_reports_the_minimum_next_bid_for_the_client()
    {
        var result = BidRules.Validate(Auction(current: 4_500m, increment: 500m), Bidder(), 4_600m, Now);

        Assert.False(result.IsValid);
        Assert.Equal(BidErrorCodes.BidStepTooSmall, result.Code);
        Assert.Equal(5_000m, result.MinimumNextBid);
    }
}
