using Auction.Domain.Enums;

namespace Auction.Domain.Rules;

public static class BidErrorCodes
{
    public const string AuctionNotFound = "AUCTION_NOT_FOUND";
    public const string AuctionNotActive = "AUCTION_NOT_ACTIVE";
    public const string AuctionNotStarted = "AUCTION_NOT_STARTED";
    public const string AuctionEnded = "AUCTION_ENDED";
    public const string BidderNotFound = "BIDDER_NOT_FOUND";
    public const string BidderInactive = "BIDDER_INACTIVE";
    public const string SelfBidNotAllowed = "SELF_BID_NOT_ALLOWED";
    public const string InvalidAmount = "INVALID_AMOUNT";
    public const string BidTooLow = "BID_TOO_LOW";
    public const string BidStepTooSmall = "BID_STEP_TOO_SMALL";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
}

public sealed record AuctionSnapshot(
    Guid AuctionId,
    bool Exists,
    AuctionStatus Status,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    decimal StartPrice,
    decimal CurrentPrice,
    decimal MinIncrement,
    Guid CreatedByUserId);

public sealed record BidderSnapshot(
    Guid UserId,
    bool Exists,
    bool IsActive,
    UserRole Role,
    string DisplayName);

public sealed record BidValidationResult(
    bool IsValid,
    string Code,
    string Message,
    decimal MinimumNextBid)
{
    public static BidValidationResult Ok(decimal minimumNextBid) =>
        new(true, "OK", "Lượt đặt giá hợp lệ.", minimumNextBid);
}

/// <summary>
/// Single source of truth for bidding rules. Used by the gRPC Bid Validation Service
/// and re-checked by the Auction API inside the write transaction.
/// </summary>
public static class BidRules
{
    public static readonly TimeSpan MaxBidWindow = TimeSpan.FromDays(90);

    public static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static bool HasValidScale(decimal value) =>
        value == decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static BidValidationResult Validate(
        AuctionSnapshot? auction,
        BidderSnapshot? bidder,
        decimal amount,
        DateTime utcNow)
    {
        if (auction is null || !auction.Exists)
        {
            return Reject(BidErrorCodes.AuctionNotFound, "Phiên đấu giá không tồn tại.", 0m);
        }

        var currentPrice = auction.CurrentPrice > 0 ? auction.CurrentPrice : auction.StartPrice;
        var minimumNext = RoundMoney(currentPrice + auction.MinIncrement);

        if (auction.Status == AuctionStatus.Cancelled)
        {
            return Reject(BidErrorCodes.AuctionNotActive, "Phiên đấu giá đã bị hủy.", minimumNext);
        }

        if (auction.Status == AuctionStatus.Ended || utcNow >= auction.EndTimeUtc)
        {
            return Reject(BidErrorCodes.AuctionEnded, "Phiên đấu giá đã kết thúc.", minimumNext);
        }

        if (auction.Status == AuctionStatus.Scheduled || utcNow < auction.StartTimeUtc)
        {
            return Reject(BidErrorCodes.AuctionNotStarted, "Phiên đấu giá chưa bắt đầu.", minimumNext);
        }

        if (auction.Status != AuctionStatus.Active)
        {
            return Reject(BidErrorCodes.AuctionNotActive, "Phiên đấu giá không nhận thêm lượt đặt giá.", minimumNext);
        }

        if (bidder is null || !bidder.Exists)
        {
            return Reject(BidErrorCodes.BidderNotFound, "Tài khoản người đặt giá không tồn tại.", minimumNext);
        }

        if (!bidder.IsActive)
        {
            return Reject(BidErrorCodes.BidderInactive, "Tài khoản người đặt giá đã bị khóa.", minimumNext);
        }

        if (bidder.Role == UserRole.Admin)
        {
            return Reject(BidErrorCodes.SelfBidNotAllowed, "Quản trị viên không thể đặt giá trên sàn.", minimumNext);
        }

        if (auction.CreatedByUserId == bidder.UserId)
        {
            return Reject(BidErrorCodes.SelfBidNotAllowed, "Bạn không thể đặt giá cho phiên do chính mình tạo.", minimumNext);
        }

        if (amount <= 0 || !HasValidScale(amount))
        {
            return Reject(BidErrorCodes.InvalidAmount, "Giá đặt phải lớn hơn 0 và có tối đa hai chữ số thập phân.", minimumNext);
        }

        if (amount <= currentPrice)
        {
            return Reject(BidErrorCodes.BidTooLow,
                $"Giá đặt phải cao hơn giá hiện tại {currentPrice:0.00}.", minimumNext);
        }

        if (amount < minimumNext)
        {
            return Reject(BidErrorCodes.BidStepTooSmall,
                $"Giá đặt tối thiểu là {minimumNext:0.00} (bước giá {auction.MinIncrement:0.00}).",
                minimumNext);
        }

        return BidValidationResult.Ok(minimumNext);
    }

    private static BidValidationResult Reject(string code, string message, decimal minimumNextBid) =>
        new(false, code, message, minimumNextBid);
}
