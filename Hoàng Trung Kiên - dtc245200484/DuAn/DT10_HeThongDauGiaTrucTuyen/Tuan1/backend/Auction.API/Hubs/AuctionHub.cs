using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Auction.API.Hubs;

/// <summary>
/// Read-only realtime hub. Clients may only join/leave an auction room.
/// Bid results are broadcast exclusively by the server (via the RabbitMQ consumer),
/// so a client can never inject a fake accepted bid.
/// </summary>
[AllowAnonymous]
public sealed class AuctionHub : Hub
{
    public const string GroupPrefix = "auction:";

    public static string GroupName(Guid auctionId) => $"{GroupPrefix}{auctionId:N}";

    public async Task JoinAuction(string auctionId)
    {
        if (!Guid.TryParse(auctionId, out var id))
        {
            throw new HubException("Mã phiên đấu giá không hợp lệ.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(id));
        await Clients.Caller.SendAsync("JoinedAuction", new { auctionId = id });
    }

    public async Task LeaveAuction(string auctionId)
    {
        if (!Guid.TryParse(auctionId, out var id))
        {
            throw new HubException("Mã phiên đấu giá không hợp lệ.");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(id));
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", new { serverTimeUtc = DateTime.UtcNow });
        await base.OnConnectedAsync();
    }
}
