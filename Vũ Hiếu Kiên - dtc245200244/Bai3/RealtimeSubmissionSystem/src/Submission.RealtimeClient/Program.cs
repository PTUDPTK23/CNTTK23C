using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = "http://localhost:5000";
var studentCode = "SV001";
var multiMode = args.Any(a => a.Equals("--multi", StringComparison.OrdinalIgnoreCase));
var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();
if (positional.Length > 0) apiBaseUrl = positional[0].TrimEnd('/');
if (positional.Length > 1) studentCode = positional[1];

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

// Dong bo nhieu luot nop khong tron lan nho ConcurrentDictionary<Guid, TaskCompletionSource<string>>
var watchers = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss}");
    if (message.Status == "failed")
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {message.SubmissionId} cham LOI (failed)");
        Console.ForegroundColor = prev;
    }

    if (message.Status is "graded" or "failed" && watchers.TryGetValue(message.SubmissionId, out var tcs))
    {
        tcs.TrySetResult(message.Status);
    }
});

connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Mat ket noi SignalR, dang ket noi lai...");
    return Task.CompletedTask;
};

connection.Reconnected += async newId =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai SignalR ({newId?[..Math.Min(8, newId.Length)]}). Dang ky lai nhom...");
    foreach (var id in watchers.Keys)
    {
        await connection.InvokeAsync("WatchSubmission", id.ToString());
    }
};

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selectedAssignment = assignments?.FirstOrDefault(a => a.IsOpen);
if (selectedAssignment is null)
{
    Console.WriteLine("Khong co bai tap nao de nop.");
    return;
}

Console.WriteLine($"Chon bai tap: {selectedAssignment.Id} - {selectedAssignment.Title}");

if (!multiMode)
{
    var created = await CreateAndWatchAsync(
        http, connection, watchers, selectedAssignment.Id,
        studentCode, $"https://example.com/{studentCode.ToLowerInvariant()}/signalr",
        verbose: true);

    if (created is null) return;

    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
    var tcs = watchers[created.Id];
    var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
    if (completed != tcs.Task)
    {
        Console.WriteLine("Qua thoi gian cho thong bao ket thuc.");
    }

    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
}
else
{
    // Bai tap 2 + 3 (Slide 62): Theo doi dong thoi 2 luot nop (https:// -> graded, http:// -> failed)
    var s1 = await CreateAndWatchAsync(http, connection, watchers, selectedAssignment.Id, "SV001", "https://example.com/sv001/signalr", verbose: false);
    var s2 = await CreateAndWatchAsync(http, connection, watchers, selectedAssignment.Id, "SV002", "http://example.com/sv002/signalr", verbose: false);
    if (s1 is null || s2 is null) return;

    Console.WriteLine($"Da tao SV001: {s1.Id} ({s1.Status})");
    Console.WriteLine($"Da tao SV002: {s2.Id} ({s2.Status})");
    Console.WriteLine("Dang cho thong bao dong thoi cho 2 luot nop...");

    var allDone = Task.WhenAll(watchers[s1.Id].Task, watchers[s2.Id].Task);
    if (await Task.WhenAny(allDone, Task.Delay(TimeSpan.FromSeconds(10))) != allDone)
    {
        Console.WriteLine("Qua thoi gian cho thong bao cho ca 2 luot nop.");
    }

    var r1 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{s1.Id}");
    var r2 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{s2.Id}");
    var sr1 = watchers[s1.Id].Task.IsCompletedSuccessfully ? watchers[s1.Id].Task.Result : "timeout";
    var sr2 = watchers[s2.Id].Task.IsCompletedSuccessfully ? watchers[s2.Id].Task.Result : "timeout";
    Console.WriteLine($"SV001: SignalR = {sr1}, REST = {r1?.Status}");
    Console.WriteLine($"SV002: SignalR = {sr2}, REST = {r2?.Status}");
}

static async Task<SubmissionDto?> CreateAndWatchAsync(
    HttpClient http,
    HubConnection connection,
    ConcurrentDictionary<Guid, TaskCompletionSource<string>> watchers,
    int assignmentId,
    string code,
    string repoUrl,
    bool verbose)
{
    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{assignmentId}/submissions",
        new CreateSubmissionRequest(code, repoUrl));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return null;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null)
    {
        Console.WriteLine("Khong doc duoc phan hoi tao luot nop.");
        return null;
    }

    if (verbose)
    {
        Console.WriteLine($"Da tao luot nop: {created.Id}");
        Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
    }

    watchers[created.Id] = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
    return created;
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode) return;
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{(int)response.StatusCode} {response.StatusCode}: {error?.Code} - {error?.Message}");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
