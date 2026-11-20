namespace Auction.Domain.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    /// <summary>Relative (/media/...) or absolute URL of the product image.</summary>
    public string ImageUrl { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Domain.Entities.Auction> Auctions { get; set; } = new List<Domain.Entities.Auction>();
}
