using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "ChatRoomApi", status = "running" }));
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

static class ChatState
{
    public static ConcurrentDictionary<string, (string User, string Room)> Users { get; } = new();
}

sealed class ChatHub : Hub
{
    public async Task JoinRoom(string userName, string roomName)
    {
        if (ChatState.Users.TryGetValue(Context.ConnectionId, out var oldInfo) && !string.IsNullOrEmpty(oldInfo.Room))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, oldInfo.Room);
            await Clients.Group(oldInfo.Room).SendAsync("SystemMessage", $"{oldInfo.User} da roi phong {oldInfo.Room}.");
        }

        ChatState.Users[Context.ConnectionId] = (userName, roomName);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        await Clients.Group(roomName).SendAsync("SystemMessage", $"{userName} da tham gia phong [{roomName}].");
    }

    public async Task SendMessageToRoom(string message)
    {
        if (!ChatState.Users.TryGetValue(Context.ConnectionId, out var info))
        {
            await Clients.Caller.SendAsync("SystemMessage", "Ban chua tham gia phong nao! Dung lenh /join <ten-phong>");
            return;
        }

        var time = DateTime.Now.ToString("HH:mm:ss");
        await Clients.Group(info.Room).SendAsync("ReceiveMessage", info.Room, info.User, message, time);
    }

    public async Task SendPrivateMessage(string targetUserName, string message)
    {
        var sender = ChatState.Users.TryGetValue(Context.ConnectionId, out var info) ? info.User : "An danh";
        var targetEntry = ChatState.Users.FirstOrDefault(x => 
            x.Value.User.Equals(targetUserName, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(targetEntry.Key))
        {
            await Clients.Caller.SendAsync("SystemMessage", $"Khong tim thay nguoi dung '{targetUserName}' dang truc tuyen.");
            return;
        }

        var time = DateTime.Now.ToString("HH:mm:ss");
        await Clients.Client(targetEntry.Key).SendAsync("ReceivePrivate", sender, message, time);
        await Clients.Caller.SendAsync("SystemMessage", $"[Rieng toi {targetEntry.Value.User} - {time}] {message}");
    }

    public async Task GetOnlineUsers()
    {
        var list = ChatState.Users.Values
            .Select(v => $"{v.User} (phong: {v.Room})")
            .ToList();
        await Clients.Caller.SendAsync("SystemMessage", 
            list.Count > 0 ? $"Dang truc tuyen ({list.Count}): {string.Join(", ", list)}" : "Chua co ai truc tuyen.");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ChatState.Users.TryRemove(Context.ConnectionId, out var info))
        {
            await Clients.Group(info.Room).SendAsync("SystemMessage", $"{info.User} da ngat ket noi.");
        }
        await base.OnDisconnectedAsync(exception);
    }
}