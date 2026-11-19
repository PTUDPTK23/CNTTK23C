using Microsoft.AspNetCore.SignalR.Client;

const string hubUrl = "http://localhost:5200/hubs/ping";

var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

// Đăng ký lắng nghe sự kiện "Echo" từ máy chủ gửi về
connection.On<string>("Echo", reply =>
{
    Console.WriteLine($"<< {reply}");
});

connection.Reconnecting += err =>
{
    Console.WriteLine($"[!] Dang ket noi lai... ({err?.Message})");
    return Task.CompletedTask;
};

connection.Reconnected += id =>
{
    Console.WriteLine($"[+] Da ket noi lai! ConnectionId moi: {id}");
    return Task.CompletedTask;
};

await connection.StartAsync();
Console.WriteLine($"Da ket noi toi {hubUrl} (ConnectionId: {connection.ConnectionId})");
Console.WriteLine("Nhap tin nhan va nhan Enter (go 'exit' de thoat):");

while (true)
{
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) continue;
    if (line.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    await connection.InvokeAsync("Echo", line);
}

await connection.StopAsync();