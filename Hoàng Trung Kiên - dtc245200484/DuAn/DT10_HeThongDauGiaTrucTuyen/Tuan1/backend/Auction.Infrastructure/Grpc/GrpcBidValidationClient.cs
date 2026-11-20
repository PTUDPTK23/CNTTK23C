using System.Globalization;
using Auction.Application.Abstractions;
using Auction.Application.Common;
using BidValidation.Contracts;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auction.Infrastructure.Grpc;

public sealed class BidValidationOptions
{
    public string Address { get; set; } = "http://localhost:5101";
    public int TimeoutSeconds { get; set; } = 5;
}

/// <summary>Real gRPC client for the Bid Validation Service (no REST fallback).</summary>
public sealed class GrpcBidValidationClient : IBidValidationClient, IAsyncDisposable
{
    private readonly GrpcChannel _channel;
    private readonly BidValidationOptions _options;
    private readonly ILogger<GrpcBidValidationClient> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private BidValidationService.BidValidationServiceClient? _client;

    public GrpcBidValidationClient(IOptions<BidValidationOptions> options, ILogger<GrpcBidValidationClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        // h2c is used inside the private network (Docker Compose) and local development.
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        var handler = new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(10)
        };

        _channel = GrpcChannel.ForAddress(_options.Address, new GrpcChannelOptions
        {
            HttpHandler = handler,
            DisposeHttpClient = true
        });
    }

    private async Task<BidValidationService.BidValidationServiceClient> GetClientAsync(CancellationToken ct)
    {
        if (_client is not null) return _client;
        await _gate.WaitAsync(ct);
        try
        {
            _client ??= new BidValidationService.BidValidationServiceClient(_channel);
            return _client;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<BidValidationOutcome> ValidateAsync(
        Guid auctionId, Guid bidderId, decimal amount, CancellationToken ct = default)
    {
        try
        {
            var client = await GetClientAsync(ct);
            var deadline = DateTime.UtcNow.AddSeconds(_options.TimeoutSeconds);

            var response = await client.ValidateBidAsync(new BidValidationRequest
            {
                AuctionId = auctionId.ToString(),
                BidderId = bidderId.ToString(),
                Amount = amount.ToString("0.00", CultureInfo.InvariantCulture)
            }, deadline: deadline, cancellationToken: ct);

            return new BidValidationOutcome(
                response.IsValid,
                response.Code,
                response.Message,
                ParseMoney(response.CurrentPrice),
                ParseMoney(response.MinimumNextBid),
                ParseUtc(response.ServerTimeUtc));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Bid Validation Service gRPC call failed ({Status}).", ex.StatusCode);
            throw new AppException(503, "VALIDATION_SERVICE_UNAVAILABLE",
                "Dịch vụ xác thực đấu giá tạm thời không khả dụng. Vui lòng thử lại sau ít phút.");
        }
        catch (Exception ex) when (ex is not AppException and not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error calling Bid Validation Service.");
            throw new AppException(503, "VALIDATION_SERVICE_UNAVAILABLE",
                "Dịch vụ xác thực đấu giá tạm thời không khả dụng. Vui lòng thử lại sau ít phút.");
        }
    }

    private static decimal ParseMoney(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;

    private static DateTime ParseUtc(string value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var parsed) ? parsed : DateTime.UtcNow;

    public async ValueTask DisposeAsync()
    {
        await _channel.ShutdownAsync();
        _gate.Dispose();
    }
}
