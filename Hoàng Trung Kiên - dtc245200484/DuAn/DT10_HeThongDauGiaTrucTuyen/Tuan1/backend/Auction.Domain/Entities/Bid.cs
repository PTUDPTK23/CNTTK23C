namespace Auction.Domain.Entities;

public class Bid
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuctionId { get; set; }
    public Auction Auction { get; set; } = null!;
    public Guid BidderId { get; set; }
    public User Bidder { get; set; } = null!;
    public decimal Amount { get; set; }
    /// <summary>1-based sequence of the bid inside its auction.</summary>
    public int Sequence { get; set; }
    /// <summary>Unique identifier of the domain event emitted for this bid (idempotency key).</summary>
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime PlacedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsWinning { get; set; }
}
