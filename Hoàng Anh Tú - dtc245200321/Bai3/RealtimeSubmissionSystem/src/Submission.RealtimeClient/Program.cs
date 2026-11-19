using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
var watchTwo = args.Contains("--two", StringComparer.Ordinal);

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var notifications = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    if (!notifications.TryGetValue(message.SubmissionId, out var completion))
    {
        return;
    }

    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss.fff} UTC");
    if (message.Status == "failed")
    {
        var color = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"CANH BAO: luot nop {message.SubmissionId} cham loi (failed).");
        }
        finally
        {
            Console.ForegroundColor = color;
        }
    }

    if (message.Status is "graded" or "failed")
    {
        completion.TrySetResult(message.Status);
    }
});

try
{
    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault(assignment => assignment.IsOpen);
    if (selected is null)
    {
        Console.Error.WriteLine("Khong co bai tap dang mo.");
        return 1;
    }

    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

    var submissions = new List<SubmissionDto>();
    var first = await SubmitAndWatchAsync(studentCode, $"https://example.com/{studentCode}/signalr");
    if (first is null)
    {
        return 1;
    }
    submissions.Add(first);

    if (watchTwo)
    {
        var otherStudent = studentCode == "SV002" ? "SV001" : "SV002";
        var second = await SubmitAndWatchAsync(otherStudent, $"http://example.com/{otherStudent}/signalr");
        if (second is null)
        {
            return 1;
        }
        submissions.Add(second);
    }

    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
    var pending = Task.WhenAll(submissions.Select(submission => notifications[submission.Id].Task));
    if (await Task.WhenAny(pending, Task.Delay(TimeSpan.FromSeconds(10))) != pending)
    {
        Console.WriteLine("Qua thoi gian cho thong bao graded.");
    }

    foreach (var submission in submissions)
    {
        var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{submission.Id}");
        if (watchTwo)
        {
            var notification = notifications[submission.Id].Task;
            var status = notification.IsCompletedSuccessfully ? notification.Result : "khong nhan";
            Console.WriteLine($"{submission.StudentCode} ({submission.Id}): SignalR = {status}, REST = {latest?.Status}");
        }
        else
        {
            Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
        }
    }

    return 0;

    async Task<SubmissionDto?> SubmitAndWatchAsync(string code, string repositoryUrl)
    {
        using var response = await http.PostAsJsonAsync(
            $"/api/assignments/{selected.Id}/submissions",
            new CreateSubmissionRequest(code, repositoryUrl));
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Console.Error.WriteLine($"HTTP {(int)response.StatusCode}: {error?.Code} {error?.Message}");
            return null;
        }

        var created = await response.Content.ReadFromJsonAsync<SubmissionDto>();
        if (created is null)
        {
            Console.Error.WriteLine("May chu khong tra du lieu luot nop.");
            return null;
        }

        Console.WriteLine($"Da tao luot nop: {created.Id}");
        Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
        notifications[created.Id] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
        return created;
    }
}
catch (HttpRequestException exception)
{
    Console.Error.WriteLine($"HttpRequestException: {exception.Message}");
    return 1;
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
