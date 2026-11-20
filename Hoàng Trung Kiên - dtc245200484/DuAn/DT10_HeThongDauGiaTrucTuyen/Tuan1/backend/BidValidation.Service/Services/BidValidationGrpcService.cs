using System.Globalization;
using Auction.Application.Services;
using BidValidation.Contracts;
using Grpc.Core;

namespace BidValidation.Service.Services;

public sealed class BidValidationGrpcService : BidValidationService.BidValidationServiceBase
{
    private readonly BidValidationEvaluator _evaluator;
    private readonly ILogger<BidValidationGrpcService> _logger;

    public BidValidationGrpcService(BidValidationEvaluator evaluator, ILogger<BidValidationGrpcService> logger)
    {
        _evaluator = evaluator;
        _logger = logger;
    }

    public override async Task<BidValidationResponse> ValidateBid(
        BidValidationRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AuctionId, out var auctionId) ||
            !Guid.TryParse(request.BidderId, out var bidderId) ||
            !decimal.TryParse(request.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return new BidValidationResponse
            {
                IsValid = false,
                Code = "INVALID_REQUEST",
                Message = "Cần có mã phiên đấu giá, mã người đặt giá và số tiền hợp lệ.",
                ServerTimeUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
        }

        var result = await _evaluator.EvaluateAsync(auctionId, bidderId, amount, context.CancellationToken);

        _logger.LogInformation(
            "Validated bid auction={AuctionId} bidder={BidderId} amount={Amount} => {Valid} {Code}",
            auctionId, bidderId, amount, result.IsValid, result.Code);

        return new BidValidationResponse
        {
            IsValid = result.IsValid,
            Code = result.Code,
            Message = result.Message,
            CurrentPrice = result.CurrentPrice.ToString("0.00", CultureInfo.InvariantCulture),
            MinimumNextBid = result.MinimumNextBid.ToString("0.00", CultureInfo.InvariantCulture),
            ServerTimeUtc = result.ServerTimeUtc.ToString("O", CultureInfo.InvariantCulture),
            AuctionStatus = result.AuctionStatus
        };
    }

    public override Task<HealthResponse> GetHealth(HealthRequest request, ServerCallContext context) =>
        Task.FromResult(new HealthResponse
        {
            Status = "healthy",
            Service = "BidValidation.Service",
            ServerTimeUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        });
}
