using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "dtc245200452";
var secondStudentCode = args.Length > 2 ? args[2] : $"{studentCode}-fail";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var pending = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();
var consoleLock = new object();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    lock (consoleLock)
    {
        if (message.Status == "failed")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            try
            {
                Console.WriteLine($"!!! CANH BAO: luot nop {message.SubmissionId} cham LOI (failed) luc {message.UpdatedAt:HH:mm:ss}");
            }
            finally
            {
                Console.ResetColor();
            }
        }
        else
        {
            Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss}");
        }
    }

    if (message.Status is "graded" or "failed" &&
        pending.TryGetValue(message.SubmissionId, out var tcs))
    {
        tcs.TrySetResult(message.Status);
    }
});

await connection.StartAsync();
WriteLine(consoleLock, "Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selectedAssignment = assignments?.FirstOrDefault();
if (selectedAssignment is null)
{
    WriteLine(consoleLock, "Khong co bai tap nao de nop.");
    return;
}

WriteLine(consoleLock, $"Chon bai tap: {selectedAssignment.Id} - {selectedAssignment.Title}");

var createRequests = new[]
{
    new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode}/signalr"),
    new CreateSubmissionRequest(secondStudentCode, $"http://example.com/{secondStudentCode}/signalr")
};

var submissions = new List<SubmissionDto>(createRequests.Length);
foreach (var createRequest in createRequests)
{
    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignment.Id}/submissions",
        createRequest);

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse, consoleLock);
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null)
    {
        WriteLine(consoleLock, "Khong doc duoc phan hoi tao luot nop.");
        return;
    }

    // Register before WatchSubmission so an early server push cannot miss the dictionary entry
    pending[created.Id] = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    submissions.Add(created);

    WriteLine(consoleLock, $"Da tao {created.StudentCode}: {created.Id} ({created.Status})");
    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
}

WriteLine(consoleLock, $"Dang cho thong bao trang thai tu may chu cho {submissions.Count} luot nop...");

var allCompleted = Task.WhenAll(submissions.Select(s => pending[s.Id].Task));
var completed = await Task.WhenAny(allCompleted, Task.Delay(TimeSpan.FromSeconds(10)));
if (completed != allCompleted)
{
    WriteLine(consoleLock, "Qua thoi gian cho thong bao trang thai cuoi.");
}

foreach (var submission in submissions)
{
    var signalRStatus = pending[submission.Id].Task.IsCompletedSuccessfully
        ? pending[submission.Id].Task.Result
        : "timeout";
    var latestSubmission = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{submission.Id}");
    WriteLine(consoleLock, $"{submission.StudentCode} ({submission.Id}): SignalR = {signalRStatus}, REST = {latestSubmission?.Status}");
}

static void WriteLine(object consoleLock, string text)
{
    lock (consoleLock)
    {
        Console.WriteLine(text);
    }
}

static async Task PrintApiErrorAsync(HttpResponseMessage response, object consoleLock)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    WriteLine(consoleLock, $"{(int)response.StatusCode} {response.StatusCode}: {error?.Code} - {error?.Message}");
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
