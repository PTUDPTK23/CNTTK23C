using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

const string apiBaseUrl = "http://localhost:5000";
Guid? watchedSubmissionId = null;
var completed = new TaskCompletionSource<string>(
    TaskCreationOptions.RunContinuationsAsynchronously);

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

connection.On<SubmissionStatusChanged>(
    "SubmissionStatusChanged",
    update =>
    {
        Console.WriteLine(
            $"Cap nhat realtime: {update.Status} ({update.UpdatedAt:O})");

        if (update.Status == "graded")
        {
            completed.TrySetResult(update.Status);
        }
    });

connection.Reconnecting += error =>
{
    Console.WriteLine($"Mat ket noi, dang ket noi lai: {error?.Message}");
    return Task.CompletedTask;
};

connection.Reconnected += async connectionId =>
{
    Console.WriteLine($"Da ket noi lai: {connectionId}");
    if (watchedSubmissionId is Guid id)
    {
        await connection.InvokeAsync("WatchSubmission", id);
    }
};

using var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await connection.StartAsync();
Console.WriteLine("SignalR da ket noi.");

var request = new CreateSubmissionRequest(
    args.Length > 0 ? args[0] : "Sinh vien demo",
    args.Length > 1 ? args[1] : "Bai tap SignalR");

using var postResponse = await httpClient.PostAsJsonAsync(
    "/api/submissions",
    request);
postResponse.EnsureSuccessStatusCode();

var created = await postResponse.Content.ReadFromJsonAsync<Submission>()
    ?? throw new InvalidOperationException("API returned an empty submission.");
watchedSubmissionId = created.Id;
Console.WriteLine($"Da tao submission {created.Id}: {created.Status}");

await connection.InvokeAsync("WatchSubmission", created.Id);
Console.WriteLine($"Da theo doi group submission:{created.Id}");

var current = await httpClient.GetFromJsonAsync<Submission>(
    $"/api/submissions/{created.Id}")
    ?? throw new InvalidOperationException("Could not read the submission.");
Console.WriteLine($"Trang thai REST hien tai: {current.Status}");

await completed.Task.WaitAsync(TimeSpan.FromSeconds(20));

var finalState = await httpClient.GetFromJsonAsync<Submission>(
    $"/api/submissions/{created.Id}")
    ?? throw new InvalidOperationException("Could not read the final submission.");
Console.WriteLine($"Trang thai cuoi qua REST: {finalState.Status}");

public sealed record CreateSubmissionRequest(
    string StudentName,
    string AssignmentName);

public sealed record Submission(
    Guid Id,
    string StudentName,
    string AssignmentName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SubmissionStatusChanged(
    Guid Id,
    string StudentName,
    string AssignmentName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
