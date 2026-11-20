namespace Auction.Application.Dtos;

public sealed record TimePointDto(string Date, int Count);

public sealed record StatusCountDto(string Status, int Count);

public sealed record TopAuctionDto(
    Guid Id,
    string Title,
    string Status,
    decimal CurrentPrice,
    int BidCount,
    string? WinnerName);

public sealed record DashboardDto(
    int TotalProducts,
    int TotalAuctions,
    int ActiveAuctions,
    int ScheduledAuctions,
    int EndedAuctions,
    int TotalUsers,
    int TotalBidders,
    int TotalBids,
    decimal TotalBidVolume,
    IReadOnlyList<TimePointDto> BidsPerDay,
    IReadOnlyList<StatusCountDto> AuctionsByStatus,
    IReadOnlyList<TopAuctionDto> TopAuctions,
    IReadOnlyList<BidDto> RecentBids,
    DateTime GeneratedAtUtc);

public sealed record AuditLogDto(
    Guid Id,
    Guid EventId,
    string EventType,
    Guid? AuctionId,
    Guid? UserId,
    string Payload,
    DateTime OccurredAtUtc,
    DateTime ProcessedAtUtc);
