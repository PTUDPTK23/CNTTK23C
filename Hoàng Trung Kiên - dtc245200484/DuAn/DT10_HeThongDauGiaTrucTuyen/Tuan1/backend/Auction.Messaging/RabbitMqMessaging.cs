using System.Text;
using Auction.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Auction.Messaging;

public static class RabbitTopologySetup
{
    public static async Task EnsureAsync(IChannel channel, RabbitMqOptions options, CancellationToken ct = default)
    {
        await channel.ExchangeDeclareAsync(RabbitTopology.Exchange, ExchangeType.Topic,
            durable: true, autoDelete: false, arguments: null, passive: false, noWait: false, cancellationToken: ct);

        await channel.ExchangeDeclareAsync(RabbitTopology.DeadLetterExchange, ExchangeType.Direct,
            durable: true, autoDelete: false, arguments: null, passive: false, noWait: false, cancellationToken: ct);

        var queues = new[]
        {
            (RabbitTopology.HubQueue, RabbitTopology.HubDeadLetterQueue),
            (RabbitTopology.AuditQueue, RabbitTopology.AuditDeadLetterQueue)
        };

        foreach (var (queue, deadLetterQueue) in queues)
        {
            var queueName = options.ResolveQueue(queue);
            var dlqName = options.ResolveQueue(deadLetterQueue);

            var arguments = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = RabbitTopology.DeadLetterExchange
            };

            await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false,
                arguments: arguments, passive: false, noWait: false, cancellationToken: ct);

            await channel.QueueDeclareAsync(dlqName, durable: true, exclusive: false, autoDelete: false,
                arguments: null, passive: false, noWait: false, cancellationToken: ct);

            foreach (var routingKey in new[] { RabbitTopology.BidAcceptedRoutingKey, RabbitTopology.AuctionEndedRoutingKey })
            {
                await channel.QueueBindAsync(queueName, RabbitTopology.Exchange, routingKey,
                    arguments: null, noWait: false, cancellationToken: ct);
                await channel.QueueBindAsync(dlqName, RabbitTopology.DeadLetterExchange, routingKey,
                    arguments: null, noWait: false, cancellationToken: ct);
            }
        }
    }
}

public sealed class RabbitEventPublisher : IAsyncDisposable
{
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IChannel? _channel;

    public RabbitEventPublisher(RabbitMqConnection connection, RabbitMqOptions options)
    {
        _connection = connection;
        _options = options;
    }

    public async Task PublishAsync(string routingKey, string eventType, Guid eventId, string json,
        CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var channel = await GetChannelAsync(ct);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = eventId.ToString(),
                Type = eventType,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Headers = new Dictionary<string, object?>
                {
                    ["x-event-id"] = eventId.ToString(),
                    ["x-event-type"] = eventType
                }
            };

            await channel.BasicPublishAsync(RabbitTopology.Exchange, routingKey, mandatory: false,
                basicProperties: properties, body: Encoding.UTF8.GetBytes(json), cancellationToken: ct);
        }
        catch
        {
            await DisposeChannelAsync();
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await DisposeChannelAsync();
        var connection = await _connection.GetConnectionAsync(ct);
        _channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            ct);

        await RabbitTopologySetup.EnsureAsync(_channel, _options, ct);
        return _channel;
    }

    private async Task DisposeChannelAsync()
    {
        if (_channel is null) return;
        try { await _channel.DisposeAsync(); } catch { /* ignore */ }
        _channel = null;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeChannelAsync();
        _gate.Dispose();
    }
}

public sealed record EventEnvelope(string RoutingKey, string EventType, Guid EventId, string Json);

/// <summary>
/// Shared manual-ack consumer: retries processing in place, ACKs on success and
/// dead-letters (NACK without requeue → x-dead-letter-exchange) after the retry budget.
/// </summary>
public abstract class QueueConsumerService : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqOptions _options;
    private readonly ILogger _logger;
    private IChannel? _channel;

    protected QueueConsumerService(RabbitMqConnection connection, RabbitMqOptions options, ILogger logger)
    {
        _connection = connection;
        _options = options;
        _logger = logger;
    }

    protected abstract string QueueName { get; }
    protected virtual ushort PrefetchCount => 20;
    protected virtual int MaxAttempts => 3;

    protected abstract Task HandleAsync(EventEnvelope envelope, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("{Service} started with RabbitMQ disabled.", GetType().Name);
            return;
        }

        var queue = _options.ResolveQueue(QueueName);
        var attempt = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _channel = await _connection.CreateChannelAsync(stoppingToken);
                await RabbitTopologySetup.EnsureAsync(_channel, _options, stoppingToken);
                await _channel.BasicQosAsync(0, PrefetchCount, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += (_, args) => OnReceivedAsync(args, stoppingToken);

                await _channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("{Service} consuming queue {Queue}.", GetType().Name, queue);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                attempt++;
                var delay = TimeSpan.FromSeconds(Math.Min(15, Math.Pow(2, Math.Min(attempt, 4))));
                _logger.LogError(ex, "{Service} broker connection failed (attempt {Attempt}); retrying in {Delay}s.",
                    GetType().Name, attempt, delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
            finally
            {
                if (_channel is not null)
                {
                    try { await _channel.DisposeAsync(); } catch { /* ignore */ }
                    _channel = null;
                }
            }
        }
    }

    private async Task OnReceivedAsync(BasicDeliverEventArgs args, CancellationToken ct)
    {
        var channel = _channel;
        if (channel is null) return;

        var body = Encoding.UTF8.GetString(args.Body.Span);
        var eventType = args.BasicProperties?.Type
                        ?? (args.BasicProperties?.Headers?.TryGetValue("x-event-type", out var headerValue) == true
                            ? headerValue?.ToString()
                            : null)
                        ?? args.RoutingKey;

        var eventId = Guid.TryParse(args.BasicProperties?.MessageId, out var parsed) ? parsed : Guid.Empty;
        var envelope = new EventEnvelope(args.RoutingKey, eventType, eventId, body);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await HandleAsync(envelope, ct);
                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: ct);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(ex, "{Service} failed to process {EventType} {EventId} (attempt {Attempt}/{Max}); retrying.",
                    GetType().Name, eventType, eventId, attempt, MaxAttempts);
                await Task.Delay(TimeSpan.FromMilliseconds(300 * attempt), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Service} exhausted retries for {EventType} {EventId}; routing to DLQ.",
                    GetType().Name, eventType, eventId);
                await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
                return;
            }
        }
    }
}
