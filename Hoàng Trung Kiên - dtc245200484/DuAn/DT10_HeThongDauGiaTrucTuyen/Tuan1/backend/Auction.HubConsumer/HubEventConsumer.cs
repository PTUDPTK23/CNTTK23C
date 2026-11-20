using System.Net.Http.Json;
using System.Text;
using Auction.Contracts;
using Auction.Messaging;
using Microsoft.Extensions.Options;

namespace Auction.HubConsumer;

public sealed class HubBroadcastOptions
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5100";
    public string InternalApiKey { get; set; } = "dev-internal-key-change-me";
    public int Port { get; set; } = 5102;
}

/// <summary>Forwards accepted domain events to the Auction API's SignalR hub.</summary>
public sealed class AuctionHubClient
{
    private readonly HttpClient _http;
    private readonly HubBroadcastOptions _options;
    private readonly ILogger<AuctionHubClient> _logger;

    public AuctionHubClient(HttpClient http, IOptions<HubBroadcastOptions> options, ILogger<AuctionHubClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task BroadcastBidAcceptedAsync(string json, CancellationToken ct)
    {
        await PostAsync("/internal/hub/bid-accepted", json, ct);
    }

    public async Task BroadcastAuctionEndedAsync(string json, CancellationToken ct)
    {
        await PostAsync("/internal/hub/auction-ended", json, ct);
    }

    private async Task PostAsync(string path, string json, CancellationToken ct)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, path)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-Internal-Api-Key", _options.InternalApiKey);

                using var response = await _http.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                var body = await response.Content.ReadAsStringAsync(ct);
                last = new HttpRequestException(
                    $"Auction API broadcast returned {(int)response.StatusCode}: {body}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
            }

            _logger.LogWarning(last, "Broadcast to {Path} failed (attempt {Attempt}/3).", path, attempt);
            await Task.Delay(300 * attempt, ct);
        }

        throw last ?? new HttpRequestException($"Broadcast to {path} failed.");
    }
}

public sealed class HubEventConsumer : QueueConsumerService
{
    private readonly AuctionHubClient _client;

    public HubEventConsumer(RabbitMqConnection connection, RabbitMqOptions options,
        AuctionHubClient client, ILogger<HubEventConsumer> logger)
        : base(connection, options, logger)
    {
        _client = client;
    }

    protected override string QueueName => RabbitTopology.HubQueue;

    protected override async Task HandleAsync(EventEnvelope envelope, CancellationToken ct)
    {
        switch (envelope.EventType)
        {
            case EventTypes.BidAccepted:
                await _client.BroadcastBidAcceptedAsync(envelope.Json, ct);
                break;
            case EventTypes.AuctionEnded:
                await _client.BroadcastAuctionEndedAsync(envelope.Json, ct);
                break;
            default:
                // Unknown event types are ignored (acknowledged) so they cannot poison the queue.
                break;
        }
    }
}
