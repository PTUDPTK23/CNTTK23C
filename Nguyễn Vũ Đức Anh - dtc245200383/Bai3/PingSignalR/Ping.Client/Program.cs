using Microsoft.AspNetCore.SignalR.Client;

var hubUrl = "http://localhost:5200/hubs/ping";

var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("ReceiveEcho", message =>
{
    Console.WriteLine($"Echo: {message}");
});

connection.On<string>("ReceiveBroadcast", message =>
{
    Console.WriteLine($"Broadcast: {message}");
});

connection.On<string>("ReceiveGroupMessage", message =>
{
    Console.WriteLine($"Group: {message}");
});

connection.On<string>("ReceiveSystem", message =>
{
    Console.WriteLine($"He thong: {message}");
});

connection.Reconnecting += error =>
{
    Console.WriteLine("Dang reconnect...");
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Console.WriteLine($"Da reconnect. ConnectionId moi: {connectionId}");
    return Task.CompletedTask;
};

connection.Closed += error =>
{
    Console.WriteLine(error == null
        ? "Ket noi da dong."
        : $"Ket noi dong loi: {error.Message}");
    return Task.CompletedTask;
};

await connection.StartAsync();
Console.WriteLine($"Da ket noi: {connection.ConnectionId}");

while (true)
{
    Console.WriteLine();
    Console.WriteLine("Chon thao tac:");
    Console.WriteLine("1 - Echo: chi gui lai cho may hien tai");
    Console.WriteLine("2 - Broadcast: gui cho tat ca client");
    Console.WriteLine("3 - Tham gia group");
    Console.WriteLine("4 - Gui tin den group");
    Console.WriteLine("0 - Thoat");
    Console.Write("Lua chon: ");

    var choice = Console.ReadLine();
    if (choice == null)
    {
        await connection.StopAsync();
        await connection.DisposeAsync();
        return;
    }

    try
    {
        switch (choice)
        {
            case "1":
                Console.Write("Noi dung Echo: ");
                var echo = Console.ReadLine();
                await connection.InvokeAsync("Echo", echo);
                break;

            case "2":
                Console.Write("Noi dung Broadcast: ");
                var broadcast = Console.ReadLine();
                await connection.InvokeAsync("Broadcast", broadcast);
                break;

            case "3":
                Console.Write("Ten group: ");
                var groupName = Console.ReadLine();
                await connection.InvokeAsync("JoinGroup", groupName);
                break;

            case "4":
                Console.Write("Ten group: ");
                var targetGroup = Console.ReadLine();
                Console.Write("Noi dung gui group: ");
                var groupMessage = Console.ReadLine();
                await connection.InvokeAsync(
                    "SendToGroup",
                    targetGroup,
                    groupMessage);
                break;

            case "0":
                await connection.StopAsync();
                await connection.DisposeAsync();
                return;

            default:
                Console.WriteLine("Lua chon khong hop le.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Loi: {ex.Message}");
    }
}
