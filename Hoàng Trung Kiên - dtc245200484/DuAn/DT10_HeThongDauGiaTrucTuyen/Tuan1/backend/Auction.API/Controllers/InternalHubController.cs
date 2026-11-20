using Auction.API.Infrastructure;
using Auction.API.Realtime;
using Auction.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Auction.API.Controllers;

/// <summary>
/// Service-to-service endpoints used by Auction.HubConsumer. Protected by a shared
/// internal API key; never exposed through the public frontend or Swagger.
/// </summary>
[ApiController]
[Route("internal/hub")]
[ApiExplorerSettings(IgnoreApi = true)]
[Produces("application/json")]
public sealed class InternalHubController : ControllerBase
{
    private readonly AuctionBroadcaster _broadcaster;
    private readonly InternalApiOptions _options;
    private readonly ILogger<InternalHubController> _logger;

    public InternalHubController(AuctionBroadcaster broadcaster, IOptions<InternalApiOptions> options,
        ILogger<InternalHubController> logger)
    {
        _broadcaster = broadcaster;
        _options = options.Value;
        _logger = logger;
    }

    [HttpPost("bid-accepted")]
    public async Task<IActionResult> BidAccepted([FromBody] BidAcceptedEvent evt)
    {
        if (!InternalRequestValidator.HasValidKey(Request, _options))
        {
            return Unauthorized();
        }

        await _broadcaster.BroadcastBidAcceptedAsync(evt);
        _logger.LogInformation("SignalR broadcast BidAccepted for auction {AuctionId}, bid {BidId}.",
            evt.AuctionId, evt.BidId);
        return Accepted(new { status = "broadcasted" });
    }

    [HttpPost("auction-ended")]
    public async Task<IActionResult> AuctionEnded([FromBody] AuctionEndedEvent evt)
    {
        if (!InternalRequestValidator.HasValidKey(Request, _options))
        {
            return Unauthorized();
        }

        await _broadcaster.BroadcastAuctionEndedAsync(evt);
        _logger.LogInformation("SignalR broadcast AuctionEnded for auction {AuctionId}.", evt.AuctionId);
        return Accepted(new { status = "broadcasted" });
    }
}
