using Microsoft.AspNetCore.SignalR;

namespace Ping.Api.Hubs;

public class PingHub : Hub
{
    public Task Echo(string message)
    {
        return Clients.Caller.SendAsync("ReceiveEcho", message);
    }

    public Task Broadcast(string message)
    {
        return Clients.All.SendAsync("ReceiveBroadcast", message);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        await Clients.Caller.SendAsync(
            "ReceiveSystem",
            $"Da tham gia group: {groupName}");
    }

    public Task SendToGroup(string groupName, string message)
    {
        return Clients.Group(groupName).SendAsync(
            "ReceiveGroupMessage",
            message);
    }
}