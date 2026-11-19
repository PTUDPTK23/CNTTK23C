using Microsoft.AspNetCore.SignalR.Client;

const string hubUrl = "http://localhost:5200/hubs/ping";

Console.Write("Nhap ten cua ban: ");
var userName = Console.ReadLine();
if (string.IsNullOrWhiteSpace(userName)) userName = "User-" + Random.Shared.Next(100, 999);

var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("Echo", reply => Console.WriteLine($"<< {reply}"));
connection.On<string, string>("ReceiveBroadcast", (user, msg) =>
    Console.WriteLine($"[Broadcast - {user}]: {msg}"));
connection.On<string, string, string>("ReceiveRoomMessage", (room, user, msg) =>
    Console.WriteLine($"[Phong {room} - {user}]: {msg}"));

await connection.StartAsync();
Console.WriteLine($"Da ket noi toi {hubUrl} (Id: {connection.ConnectionId})");
Console.WriteLine("Cu phap: '/all <tin>' | '/join <phong>' | '/room <phong> <tin>' | 'exit'");

while (true)
{
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) continue;
    if (line.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    if (line.StartsWith("/all "))
    {
        await connection.InvokeAsync("Broadcast", userName, line[5..]);
    }
    else if (line.StartsWith("/join "))
    {
        await connection.InvokeAsync("JoinRoom", line[6..].Trim(), userName);
    }
    else if (line.StartsWith("/room "))
    {
        var parts = line[6..].Split(' ', 2);
        if (parts.Length == 2)
            await connection.InvokeAsync("SendToRoom", parts[0], userName, parts[1]);
    }
    else
    {
        await connection.InvokeAsync("Echo", line);
    }
}

await connection.StopAsync();