using Auction.API.Infrastructure;
using Auction.Application.Common;
using Auction.Application.Dtos;
using Auction.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auction.API.Controllers;

[ApiController]
[Route("api/auctions")]
[Produces("application/json")]
public sealed class AuctionsController : ControllerBase
{
    private readonly AuctionService _auctions;
    private readonly BidService _bids;
    private readonly AuctionLifecycleService _lifecycle;

    public AuctionsController(AuctionService auctions, BidService bids, AuctionLifecycleService lifecycle)
    {
        _auctions = auctions;
        _bids = bids;
        _lifecycle = lifecycle;
    }

    /// <summary>List auctions with status, category, search and sorting filters.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<AuctionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuctionSummaryDto>>> List(
        [FromQuery] string? status,
        [FromQuery] string? category,
        [FromQuery] string? q,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default) =>
        Ok(await _auctions.ListAsync(status, category, q, sort, page, pageSize, ct));

    /// <summary>Public house statistics used by the home page (lots, live rooms, bids, bidders).</summary>
    [HttpGet("stats")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuctionStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuctionStatsDto>> Stats(CancellationToken ct) =>
        Ok(await _auctions.GetStatsAsync(ct));

    /// <summary>Auction detail, including recent bids and the server clock for countdowns.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuctionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuctionDetailDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await _auctions.GetAsync(id, ct));

    /// <summary>Create an auction (admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AuctionDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAuctionRequest request, CancellationToken ct)
    {
        var created = await _auctions.CreateAsync(request, User.GetRequiredUserId(), ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>
    /// Place a bid. Validated by the gRPC Bid Validation Service, then persisted inside a
    /// serialized transaction (row lock) before BidAccepted is published to RabbitMQ.
    /// </summary>
    [HttpPost("{id:guid}/bids")]
    [Authorize(Roles = "Bidder")]
    [ProducesResponseType(typeof(PlaceBidResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> PlaceBid(Guid id, [FromBody] PlaceBidRequest request, CancellationToken ct)
    {
        var response = await _bids.PlaceBidAsync(id, User.GetRequiredUserId(), request.Amount, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Full bid history of an auction, newest first.</summary>
    [HttpGet("{id:guid}/bids")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<BidDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BidDto>>> Bids(Guid id,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        Ok(await _auctions.GetBidsAsync(id, page, pageSize, ct));

    /// <summary>Result of an auction: winner, winning amount and status.</summary>
    [HttpGet("{id:guid}/result")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuctionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuctionResultDto>> Result(Guid id, CancellationToken ct) =>
        Ok(await _auctions.GetResultAsync(id, ct));

    /// <summary>Participants of an auction with per-user bid counts.</summary>
    [HttpGet("{id:guid}/participants")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ParticipantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ParticipantDto>>> Participants(Guid id, CancellationToken ct) =>
        Ok(await _auctions.GetParticipantsAsync(id, ct));

    /// <summary>Close an auction immediately and determine the winner (admin only).</summary>
    [HttpPost("{id:guid}/end")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AuctionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuctionResultDto>> End(Guid id, CancellationToken ct)
    {
        await _lifecycle.EndAuctionManuallyAsync(id, ct);
        return Ok(await _auctions.GetResultAsync(id, ct));
    }
}
