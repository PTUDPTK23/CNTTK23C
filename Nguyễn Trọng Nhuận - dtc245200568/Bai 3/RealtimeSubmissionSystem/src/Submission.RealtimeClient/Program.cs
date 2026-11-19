using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var isVanDung = args.Any(a => a.Equals("--vandung", StringComparison.OrdinalIgnoreCase) || a.Equals("VANDUNG", StringComparison.OrdinalIgnoreCase));
var apiBaseUrl = args.Length > 0 && !args[0].StartsWith("--") ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "SV001";

// Kênh 1: REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kênh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

if (isVanDung)
{
    // ==========================================
    // VẬN DỤNG: BÀI TẬP 2 & BÀI TẬP 3
    // ==========================================
    var pendingSubmissions = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

    connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m => {
        var shortId = m.SubmissionId.ToString()[..8];
        if (m.Status == "failed")
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"!!! CANH BAO: luot nop {shortId} cham LOI (failed)");
            Console.ForegroundColor = oldColor;
        }
        else
        {
            Console.WriteLine($"Thong bao SignalR: {shortId} -> {m.Status}");
        }

        if (m.Status is "graded" or "failed")
        {
            if (pendingSubmissions.TryGetValue(m.SubmissionId, out var tcs))
            {
                tcs.TrySetResult(m.Status);
            }
        }
    });

    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault();
    if (selected is null) { Console.WriteLine("Khong co bai tap."); return; }

    // Tạo lượt nộp 1 (SV001 - hợp lệ với https://)
    var res1 = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest("SV001", "https://example.com/sv001/signalr"));
    var sub1 = await res1.Content.ReadFromJsonAsync<SubmissionDto>();
    var tcs1 = new TaskCompletionSource<string>();
    pendingSubmissions[sub1!.Id] = tcs1;
    Console.WriteLine($"Da tao SV001: {sub1.Id.ToString()[..8]} ({sub1.Status})");

    // Tạo lượt nộp 2 (SV002 - không bắt đầu bằng https:// -> sẽ failed)
    var res2 = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest("SV002", "http://example.com/sv002/insecure"));
    var sub2 = await res2.Content.ReadFromJsonAsync<SubmissionDto>();
    var tcs2 = new TaskCompletionSource<string>();
    pendingSubmissions[sub2!.Id] = tcs2;
    Console.WriteLine($"Da tao SV002: {sub2.Id.ToString()[..8]} ({sub2.Status})");

    // Watch cả 2 trên cùng một kết nối
    await connection.InvokeAsync("WatchSubmission", sub1.Id.ToString());
    await connection.InvokeAsync("WatchSubmission", sub2.Id.ToString());

    // Chờ cả 2 hoàn tất hoặc timeout
    await Task.WhenAll(tcs1.Task, tcs2.Task);

    // Đọc lại bằng REST để đối chiếu
    var rest1 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub1.Id}");
    var rest2 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub2.Id}");

    Console.WriteLine($"SV001: SignalR = {tcs1.Task.Result}, REST = {rest1?.Status}");
    Console.WriteLine($"SV002: SignalR = {tcs2.Task.Result}, REST = {rest2?.Status}");
}
else
{
    // ==========================================
    // BƯỚC 5 & BƯỚC 6: CHUẨN MÁY KHÁCH
    // ==========================================
    var gradedNotification = new TaskCompletionSource(); // "cái chuông"

    connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m => {
        Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status} luc {m.UpdatedAt.ToLocalTime():HH:mm:ss}");
        if (m.Status is "graded" or "failed")
        {
            gradedNotification.TrySetResult();
        }
    });

    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault();
    if (selected is null)
    {
        Console.WriteLine("Khong co bai tap nao.");
        return;
    }
    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode}/signalr"));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        var err = await createResponse.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"Loi tao luot nop: {err?.Code} - {err?.Message}");
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Da tao luot nop: {created!.Id}");
    Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());

    var completed = await Task.WhenAny(gradedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
    if (completed != gradedNotification.Task)
    {
        Console.WriteLine("Qua thoi gian cho thong bao graded.");
    }

    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
