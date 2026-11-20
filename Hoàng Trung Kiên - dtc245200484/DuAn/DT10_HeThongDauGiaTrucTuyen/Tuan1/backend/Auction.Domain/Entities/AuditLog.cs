namespace Auction.Domain.Entities;

/// <summary>
/// Immutable audit trail written by the Audit Worker from RabbitMQ events.
/// EventId is unique so re-delivered messages are only processed once.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? AuctionId { get; set; }
    public Guid? UserId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "rabbitmq";
}
