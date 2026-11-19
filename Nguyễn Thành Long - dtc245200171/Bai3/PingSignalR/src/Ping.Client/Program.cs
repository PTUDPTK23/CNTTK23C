using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = (args.Length > 0 ? args[0] : "http://localhost:5200").TrimEnd('/');
var name = args.Length > 1 ? args[1] : "Client";
var retryDelays = new[]
{
    TimeSpan.Zero,
    TimeSpan.FromSeconds(2),
    TimeSpan.FromSeconds(10),
    TimeSpan.FromSeconds(30)
};

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/ping")
    .WithAutomaticReconnect(retryDelays)
    .Build();

connection.On<string>("EchoResponse", message => Console.WriteLine($"[Echo] {message}"));
connection.On<string>("BroadcastMessage", message => Console.WriteLine($"[Broadcast] {message}"));
connection.On<string>("GroupMessage", message => Console.WriteLine($"[Group] {message}"));
connection.On<string>("SystemMessage", message => Console.WriteLine($"[System] {message}"));
connection.Reconnecting += exception =>
{
    Console.WriteLine($"[Lifecycle] Dang ket noi lai: {exception?.Message ?? "connection lost"}");
    return Task.CompletedTask;
};
connection.Reconnected += async connectionId =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai. ConnectionId: {connectionId}");
    await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
};
connection.Closed += exception =>
{
    Console.WriteLine($"[Lifecycle] Ket noi da dong: {exception?.Message ?? "closed"}");
    return Task.CompletedTask;
};

await connection.StartAsync();
Console.WriteLine($"{name} da ket noi den PingApi. ConnectionId: {connection.ConnectionId}");
await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");
await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
await connection.InvokeAsync("SendToGroup", "lop-hoc-01", $"Thong bao nhom tu {name}");
await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");
Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");

while (true)
{
    var line = Console.ReadLine();
    if (line is null || string.IsNullOrWhiteSpace(line))
    {
        break;
    }

    var parts = line.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0)
    {
        continue;
    }

    var method = parts[0].ToLowerInvariant();
    if (method is "exit" or "quit")
    {
        break;
    }

    try
    {
        switch (method)
        {
            case "echo" when parts.Length >= 2:
                await connection.InvokeAsync("Echo", string.Join(' ', parts.Skip(1)));
                break;
            case "broadcast" when parts.Length >= 2:
                await connection.InvokeAsync("Broadcast", string.Join(' ', parts.Skip(1)));
                break;
            case "group" when parts.Length >= 2:
                await connection.InvokeAsync("SendToGroup", "lop-hoc-01", string.Join(' ', parts.Skip(1)));
                break;
            case "to" when parts.Length == 3:
                await connection.InvokeAsync("SendToClient", parts[1], parts[2]);
                break;
            case "bad":
                await connection.InvokeAsync("MethodKhongTonTai");
                break;
            default:
                Console.WriteLine("Lenh khong hop le. Dung echo, broadcast, group, to, bad hoac exit.");
                break;
        }
    }
    catch (HubException exception)
    {
        Console.WriteLine($"[InvocationError] {exception.Message}");
    }
}
