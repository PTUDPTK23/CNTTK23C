using Auction.API.Infrastructure;
using Auction.Application.Common;
using Auction.Application.Dtos;
using Auction.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auction.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _users;

    public UsersController(UserService users)
    {
        _users = users;
    }

    /// <summary>Current user profile with bidding statistics.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken ct) =>
        Ok(await _users.GetProfileAsync(User.GetRequiredUserId(), ct));

    /// <summary>Bid history of the current user (newest first).</summary>
    [HttpGet("me/bids")]
    [ProducesResponseType(typeof(PagedResult<BidDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BidDto>>> MyBids(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _users.GetMyBidsAsync(User.GetRequiredUserId(), page, pageSize, ct));

    /// <summary>Auctions the current user has participated in.</summary>
    [HttpGet("me/auctions")]
    [ProducesResponseType(typeof(IReadOnlyList<AuctionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuctionSummaryDto>>> MyAuctions(CancellationToken ct) =>
        Ok(await _users.GetMyAuctionsAsync(User.GetRequiredUserId(), ct));

    /// <summary>Auctions won by the current user.</summary>
    [HttpGet("me/wins")]
    [ProducesResponseType(typeof(IReadOnlyList<AuctionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuctionSummaryDto>>> MyWins(CancellationToken ct) =>
        Ok(await _users.GetMyWinsAsync(User.GetRequiredUserId(), ct));
}
