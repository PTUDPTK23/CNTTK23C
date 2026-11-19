using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = (args.Length > 0 ? args[0] : "http://localhost:5000").TrimEnd('/');
var studentCode = args.Length > 1 ? args[1] : "SV001";
var repositoryUrl = args.Length > 2
    ? args[2]
    : "https://example.com/sv001/signalr";
var eventName = args.Length > 3 ? args[3] : "SubmissionStatusChanged";
var watchDelaySeconds = 0;
if (args.Length > 4 && (!int.TryParse(args[4], out watchDelaySeconds) || watchDelaySeconds < 0))
{
    Console.Error.WriteLine("Watch delay must be a non-negative number of seconds.");
    return 2;
}

Guid? submissionId = null;
var terminalStatus = new TaskCompletionSource<SubmissionStatusMessage>(
    TaskCreationOptions.RunContinuationsAsynchronously);
var retryDelays = new[]
{
    TimeSpan.Zero,
    TimeSpan.FromSeconds(2),
    TimeSpan.FromSeconds(10),
    TimeSpan.FromSeconds(30)
};

await using var hubConnection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}/hubs/submissions")
    .WithAutomaticReconnect(retryDelays)
    .Build();

hubConnection.On<SubmissionStatusMessage>(eventName, update =>
{
    Console.WriteLine($"Thong bao SignalR: {update.SubmissionId} -> {update.Status} luc {update.UpdatedAt:HH:mm:ss}");
    if (update.Status is "graded" or "failed")
    {
        terminalStatus.TrySetResult(update);
    }
});
hubConnection.Reconnecting += exception =>
{
    Console.WriteLine($"[Lifecycle] Dang ket noi lai: {exception?.Message ?? "connection lost"}");
    return Task.CompletedTask;
};
hubConnection.Reconnected += connectionId =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai. ConnectionId: {connectionId}");
    return submissionId is Guid id
        ? hubConnection.InvokeAsync("WatchSubmission", id.ToString("D"))
        : Task.CompletedTask;
};
hubConnection.Closed += exception =>
{
    Console.WriteLine($"[Lifecycle] Ket noi da dong: {exception?.Message ?? "closed"}");
    return Task.CompletedTask;
};

await hubConnection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

using var http = new HttpClient { BaseAddress = new Uri(serverUrl) };

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
Console.WriteLine($"Chon bai tap: {selected?.Id ?? 1} - {selected?.Title ?? "Bai tap SignalR"}");

using var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected?.Id ?? 1}/submissions",
    new CreateSubmissionRequest(studentCode, repositoryUrl));

if (!createResponse.IsSuccessStatusCode)
{
    var error = await createResponse.Content.ReadAsStringAsync();
    Console.Error.WriteLine($"Submission failed: {(int)createResponse.StatusCode} {error}");
    return 1;
}

var submission = await createResponse.Content.ReadFromJsonAsync<SubmissionResponse>()
    ?? throw new InvalidDataException("The submission API returned an empty response.");
var location = createResponse.Headers.Location?.ToString() ?? $"/api/submissions/{submission.Id:D}";

submissionId = submission.Id;
Console.WriteLine($"Da tao luot nop: {submission.Id:D}");
Console.WriteLine($"Trang thai REST ban dau: {submission.Status}");
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

if (watchDelaySeconds > 0)
{
    Console.WriteLine($"[Experiment] Waiting {watchDelaySeconds}s before WatchSubmission.");
    await Task.Delay(TimeSpan.FromSeconds(watchDelaySeconds));
}

await hubConnection.InvokeAsync("WatchSubmission", submission.Id.ToString("D"));

var eventReceived = await Task.WhenAny(terminalStatus.Task, Task.Delay(TimeSpan.FromSeconds(10)));
if (eventReceived != terminalStatus.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

var latest = await http.GetFromJsonAsync<SubmissionResponse>(location);
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

return latest?.Status == "graded" ? 0 : 1;

public sealed record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);

public sealed record AssignmentDto(int Id, string Title, string? Description);

public sealed record SubmissionResponse(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    string Message,
    DateTimeOffset UpdatedAt);

public sealed record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt,
    string? Message = null,
    int? AssignmentId = null,
    string? StudentCode = null,
    string? RepositoryUrl = null)
{
    public Guid Id => SubmissionId;
}
