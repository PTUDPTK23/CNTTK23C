using System.Text.Json;
using Auction.Application.Abstractions;
using Auction.Infrastructure.Persistence;
using Auction.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auction.Infrastructure.Messaging;

public sealed class OutboxSignal : IOutboxSignal
{
    private readonly System.Threading.Channels.Channel<bool> _channel =
        System.Threading.Channels.Channel.CreateBounded<bool>(new System.Threading.Channels.BoundedChannelOptions(1)
        {
            FullMode = System.Threading.Channels.BoundedChannelFullMode.DropWrite
        });

    public void Notify() => _channel.Writer.TryWrite(true);

    public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            return await _channel.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}

/// <summary>
/// Transactional outbox dispatcher: polls pending rows and also wakes immediately
/// when the API signals that a new event was committed. Retries with backoff.
/// </summary>
public sealed class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxSignal _signal;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(IServiceScopeFactory scopeFactory, IOutboxSignal signal,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _signal = (OutboxSignal)signal;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1500, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var publishedAny = false;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<RabbitEventPublisher>();

                var pending = await db.OutboxMessages
                    .Where(m => m.PublishedAtUtc == null)
                    .OrderBy(m => m.OccurredAtUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var message in pending)
                {
                    try
                    {
                        await publisher.PublishAsync(message.RoutingKey, message.EventType,
                            message.EventId, message.Payload, stoppingToken);
                        message.PublishedAtUtc = DateTime.UtcNow;
                        message.Attempts += 1;
                        message.LastError = null;
                        publishedAny = true;
                    }
                    catch (Exception ex)
                    {
                        message.Attempts += 1;
                        message.LastError = ex.Message.Length > 1900 ? ex.Message[..1900] : ex.Message;
                        _logger.LogWarning(ex, "Outbox publish failed for {EventType} {EventId} (attempt {Attempt}).",
                            message.EventType, message.EventId, message.Attempts);
                        break;
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatcher iteration failed.");
            }

            if (publishedAny)
            {
                continue;
            }

            await _signal.WaitAsync(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
