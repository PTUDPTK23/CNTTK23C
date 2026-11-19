using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
var baseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
await using var connection = new HubConnectionBuilder().WithUrl($"{baseUrl}/hubs/submissions").WithAutomaticReconnect().Build();
var graded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status} luc {m.UpdatedAt:HH:mm:ss}");
    if (m.Status == "graded") graded.TrySetResult();
});
await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
if (selected is null) { Console.WriteLine("Khong co bai tap de nop."); return; }
Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");
var response = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode}/signalr"));
if (response.StatusCode != HttpStatusCode.Created)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"HTTP {(int)response.StatusCode}: {error?.Code} - {error?.Message}");
    return;
}
var created = await response.Content.ReadFromJsonAsync<SubmissionDto>();
if (created is null) { Console.WriteLine("Khong doc duoc phan hoi tao luot nop."); return; }
Console.WriteLine($"Da tao luot nop: {created.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
var finished = await Task.WhenAny(graded.Task, Task.Delay(TimeSpan.FromSeconds(10)));
if (finished != graded.Task) Console.WriteLine("Qua thoi gian cho thong bao graded.");
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
