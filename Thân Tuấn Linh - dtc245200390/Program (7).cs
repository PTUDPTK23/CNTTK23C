using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
var mode = args.Length > 2 ? args[2].ToLowerInvariant() : "single";

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var bells = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    Console.WriteLine(mode == "two"
        ? $"Thong bao SignalR: {Short(m.SubmissionId)} -> {m.Status}"
        : $"Thong bao SignalR: {Short(m.SubmissionId)} -> {m.Status} luc {m.UpdatedAt:HH:mm:ss}");

    if (m.Status == "failed")
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {Short(m.SubmissionId)} cham LOI (failed)");
        Console.ResetColor();
    }

    if ((m.Status == "graded" || m.Status == "failed") &&
        bells.TryGetValue(m.SubmissionId, out var bell))
    {
        bell.TrySetResult(m.Status);
    }
});

connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};

connection.Reconnected += newId =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {newId?[..8]}");
    return Task.CompletedTask;
};

connection.Closed += _ =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault(a => !a.IsClosed);
if (selected is null)
{
    Console.WriteLine("Khong co bai tap nao dang mo.");
    return;
}
Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

if (mode == "two")
{
    await RunTwoSubmissionsAsync();
}
else
{
    await RunOneSubmissionAsync();
}

async Task RunTwoSubmissionsAsync()
{
    var plans = new (string Code, string Repo)[]
    {
        ("SV001", "https://example.com/sv001/signalr"),
        ("SV002", "http://example.com/sv002/signalr")
    };

    var created = new List<SubmissionDto>();
    foreach (var plan in plans)
    {
        var submission = await CreateSubmissionAsync(plan.Code, plan.Repo, announce: false);
        if (submission is null) return;
        created.Add(submission);
        Console.WriteLine($"Da tao {plan.Code}: {Short(submission.Id)} (received)");
    }

    foreach (var submission in created)
    {
        bells[submission.Id] = new TaskCompletionSource<string>();
        await connection.InvokeAsync("WatchSubmission", submission.Id.ToString());
    }

    var allBells = Task.WhenAll(bells.Values.Select(b => b.Task));
    if (await Task.WhenAny(allBells, Task.Delay(TimeSpan.FromSeconds(12))) != allBells)
    {
        Console.WriteLine("Qua thoi gian cho thong bao trang thai.");
        return;
    }

    for (var i = 0; i < created.Count; i++)
    {
        var signalRStatus = await bells[created[i].Id].Task;
        var latest = await http.GetFromJsonAsync<SubmissionDto>(
            $"/api/submissions/{created[i].Id}");
        Console.WriteLine($"{plans[i].Code}: SignalR = {signalRStatus}, REST = {latest?.Status}");
    }
}

async Task RunOneSubmissionAsync()
{
    var created = await CreateSubmissionAsync(
        studentCode, $"https://example.com/{studentCode}/signalr");
    if (created is null) return;

    Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

    var bell = new TaskCompletionSource<string>();
    bells[created.Id] = bell;
    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());

    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
    if (await Task.WhenAny(bell.Task, Task.Delay(TimeSpan.FromSeconds(10))) != bell.Task)
    {
        Console.WriteLine("Qua thoi gian cho thong bao graded.");
    }

    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
}

async Task<SubmissionDto?> CreateSubmissionAsync(string code, string repositoryUrl,
    bool announce = true)
{
    var response = await http.PostAsJsonAsync(
        $"/api/assignments/{selected!.Id}/submissions",
        new CreateSubmissionRequest(code, repositoryUrl));

    if (response.StatusCode != HttpStatusCode.Created)
    {
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"Loi {(int)response.StatusCode}: {error?.Code} - {error?.Message}");
        return null;
    }

    var submission = await response.Content.ReadFromJsonAsync<SubmissionDto>();
    if (submission is null) Console.WriteLine("Khong doc duoc body 201.");
    else if (announce) Console.WriteLine($"Da tao luot nop: {submission.Id}");
    return submission;
}

static string Short(Guid id) => id.ToString()[..8];

record AssignmentDto(int Id, string Title, bool IsClosed);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
