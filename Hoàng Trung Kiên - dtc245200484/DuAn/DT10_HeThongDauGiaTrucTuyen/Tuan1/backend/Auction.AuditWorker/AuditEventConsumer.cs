using Auction.Application.Abstractions;
using Auction.Contracts;
using Auction.Domain.Entities;
using Auction.Infrastructure.Persistence;
using Auction.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auction.AuditWorker;

/// <summary>
/// Independent audit consumer. Every event is persisted once (unique event_id)
/// and re-deliveries are acknowledged without creating duplicates.
/// </summary>
public sealed class AuditEventConsumer : QueueConsumerService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;

    public AuditEventConsumer(RabbitMqConnection connection, RabbitMqOptions options,
        IServiceScopeFactory scopeFactory, IClock clock, ILogger<AuditEventConsumer> logger)
        : base(connection, options, logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
    }

    protected override string QueueName => RabbitTopology.AuditQueue;

    protected override async Task HandleAsync(EventEnvelope envelope, CancellationToken ct)
    {
        Guid eventId;
        Guid? auctionId = null;
        Guid? userId = null;
        DateTime occurredAt;

        switch (envelope.EventType)
        {
            case EventTypes.BidAccepted:
            {
                var evt = EventSerialization.Deserialize<BidAcceptedEvent>(envelope.Json)
                          ?? throw new InvalidOperationException("Invalid BidAccepted payload.");
                eventId = evt.EventId;
                auctionId = evt.AuctionId;
                userId = evt.BidderId;
                occurredAt = evt.OccurredAtUtc;
                break;
            }
            case EventTypes.AuctionEnded:
            {
                var evt = EventSerialization.Deserialize<AuctionEndedEvent>(envelope.Json)
                          ?? throw new InvalidOperationException("Invalid AuctionEnded payload.");
                eventId = evt.EventId;
                auctionId = evt.AuctionId;
                userId = evt.WinnerId;
                occurredAt = evt.EndedAtUtc;
                break;
            }
            default:
                return;
        }

        if (eventId == Guid.Empty)
        {
            throw new InvalidOperationException("Event payload has no eventId.");
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();

        var alreadyProcessed = await db.AuditLogs.AnyAsync(a => a.EventId == eventId, ct);
        if (alreadyProcessed)
        {
            return;
        }

        db.AuditLogs.Add(new AuditLog
        {
            EventId = eventId,
            EventType = envelope.EventType,
            AuctionId = auctionId,
            UserId = userId,
            Payload = envelope.Json,
            OccurredAtUtc = DateTime.SpecifyKind(occurredAt, DateTimeKind.Utc),
            ProcessedAtUtc = _clock.UtcNow,
            Source = "rabbitmq"
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique index on event_id: a concurrent duplicate was already recorded.
            if (await db.AuditLogs.AsNoTracking().AnyAsync(a => a.EventId == eventId, ct))
            {
                return;
            }

            throw;
        }
    }
}
