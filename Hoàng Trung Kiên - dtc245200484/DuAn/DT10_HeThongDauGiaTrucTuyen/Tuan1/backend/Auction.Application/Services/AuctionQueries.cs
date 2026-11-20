using System.Linq.Expressions;
using Auction.Application.Dtos;
using Auction.Domain.Entities;
using Auction.Domain.Enums;

namespace Auction.Application.Services;

public static class AuctionQueries
{
    /// <summary>Reusable EF Core projection that keeps list queries free of N+1 issues.</summary>
    public static Expression<Func<Domain.Entities.Auction, AuctionSummaryDto>> Summary(DateTime now) =>
        a => new AuctionSummaryDto(
            a.Id,
            a.Title,
            a.ProductId,
            a.Product.Name,
            a.Product.Category,
            a.Product.ImageUrl,
            a.StartPrice,
            a.CurrentPrice,
            a.MinIncrement,
            a.CurrentPrice + a.MinIncrement,
            a.StartTimeUtc,
            a.EndTimeUtc,
            a.Status == AuctionStatus.Scheduled ? "scheduled"
                : a.Status == AuctionStatus.Active ? "active"
                : a.Status == AuctionStatus.Ended ? "ended" : "cancelled",
            a.Bids.Count,
            a.Bids.OrderByDescending(b => b.Amount).Select(b => b.Bidder.DisplayName).FirstOrDefault(),
            now);
}
