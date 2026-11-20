using Auction.Domain.Enums;

namespace Auction.Domain.Entities;

public class Auction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public decimal StartPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MinIncrement { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public AuctionStatus Status { get; set; } = AuctionStatus.Scheduled;
    public Guid CreatedByUserId { get; set; }
    public Guid? WinnerId { get; set; }
    public User? Winner { get; set; }
    public Guid? WinningBidId { get; set; }
    public decimal? WinningAmount { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Monotonic version used for optimistic concurrency checks.</summary>
    public int Version { get; set; }

    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<AuctionParticipant> Participants { get; set; } = new List<AuctionParticipant>();

    public decimal MinimumNextBid => CurrentPrice + MinIncrement;

    public bool IsOpenAt(DateTime utcNow) =>
        Status == AuctionStatus.Active && StartTimeUtc <= utcNow && EndTimeUtc > utcNow;
}
