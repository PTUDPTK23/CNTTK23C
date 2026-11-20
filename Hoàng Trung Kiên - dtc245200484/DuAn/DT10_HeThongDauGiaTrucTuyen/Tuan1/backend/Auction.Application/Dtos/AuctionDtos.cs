using System.ComponentModel.DataAnnotations;

namespace Auction.Application.Dtos;

public sealed class CreateAuctionRequest
{
    [Required(ErrorMessage = "Vui lòng chọn sản phẩm.")]
    public Guid ProductId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề phiên đấu giá."), MinLength(3, ErrorMessage = "Tiêu đề phải có ít nhất 3 ký tự."), MaxLength(180, ErrorMessage = "Tiêu đề tối đa 180 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Range(0.01, 999_999_999, ErrorMessage = "Giá khởi điểm phải lớn hơn 0.")]
    public decimal StartPrice { get; set; }

    [Range(0.01, 99_999_999, ErrorMessage = "Bước giá tối thiểu phải lớn hơn 0.")]
    public decimal MinIncrement { get; set; }

    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
}

public sealed record AuctionSummaryDto(
    Guid Id,
    string Title,
    Guid ProductId,
    string ProductName,
    string Category,
    string ImageUrl,
    decimal StartingPrice,
    decimal CurrentPrice,
    decimal MinIncrement,
    decimal MinimumNextBid,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    string Status,
    int BidCount,
    string? HighestBidderName,
    DateTime ServerTimeUtc);

public sealed record AuctionDetailDto(
    Guid Id,
    string Title,
    Guid ProductId,
    string ProductName,
    string Category,
    string ImageUrl,
    string ProductDescription,
    decimal StartingPrice,
    decimal CurrentPrice,
    decimal MinIncrement,
    decimal MinimumNextBid,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    string Status,
    int BidCount,
    int ParticipantCount,
    string? HighestBidderName,
    string SellerName,
    DateTime CreatedAtUtc,
    DateTime? EndedAtUtc,
    WinnerDto? Winner,
    IReadOnlyList<BidDto> RecentBids,
    DateTime ServerTimeUtc);

public sealed record WinnerDto(
    Guid UserId,
    string DisplayName,
    decimal WinningAmount,
    Guid? WinningBidId);

public sealed record BidDto(
    Guid Id,
    Guid AuctionId,
    string? AuctionTitle,
    decimal Amount,
    Guid BidderId,
    string BidderName,
    DateTime PlacedAtUtc,
    int Sequence,
    bool IsWinning);

public sealed class PlaceBidRequest
{
    [Range(0.01, 999_999_999, ErrorMessage = "Giá đặt phải lớn hơn 0.")]
    public decimal Amount { get; set; }
}

public sealed record PlaceBidResponse(
    Guid BidId,
    Guid AuctionId,
    decimal Amount,
    decimal CurrentPrice,
    decimal MinimumNextBid,
    string BidderName,
    DateTime PlacedAtUtc,
    string Message);

public sealed record AuctionResultDto(
    Guid AuctionId,
    string Title,
    string Status,
    bool HasWinner,
    Guid? WinnerId,
    string? WinnerName,
    decimal? WinningAmount,
    Guid? WinningBidId,
    int TotalBids,
    DateTime? EndedAtUtc);

public sealed record ParticipantDto(
    Guid UserId,
    string DisplayName,
    DateTime JoinedAtUtc,
    DateTime? LastBidAtUtc,
    int BidCount);

public sealed record AuctionStatsDto(
    int TotalAuctions,
    int ActiveAuctions,
    int TotalBids,
    int TotalBidders);

public static class AuctionStatusNames
{
    public static string ToApi(this Domain.Enums.AuctionStatus status) =>
        status.ToString().ToLowerInvariant();
}
