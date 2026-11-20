namespace Auction.Domain.Entities;

public class AuctionParticipant
{
    public Guid AuctionId { get; set; }
    public Auction Auction { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastBidAtUtc { get; set; }
    public int BidCount { get; set; }
}
