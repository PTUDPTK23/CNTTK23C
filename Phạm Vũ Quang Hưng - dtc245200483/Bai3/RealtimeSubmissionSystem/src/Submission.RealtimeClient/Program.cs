using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";

// Kênh 1: REST HttpClient
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kênh 2: SignalR HubConnection
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var gradedNotification = new TaskCompletionSource(); // "Cái chuông" báo chấm xong

// Đăng ký nghe thông điệp trạng thái
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status} luc {m.UpdatedAt:HH:mm:ss}");
    if (m.Status == "graded" || m.Status == "failed")
    {
        if (m.Status == "failed")
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"!!! CANH BAO: luot nop {m.SubmissionId} cham LOI (failed)");
            Console.ForegroundColor = oldColor;
        }
        gradedNotification.TrySetResult();
    }
});

// Mở kết nối thời gian thực
await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// 1. REST: Lấy danh sách bài tập
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
if (selected is null)
{
    Console.WriteLine("Khong tim thay bai tap nao tren may chu.");
    return;
}
Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

// 2. REST: Tạo lượt nộp bài
var repoUrl = args.Length > 2 
    ? args[2] 
    : (studentCode.Equals("SV002", StringComparison.OrdinalIgnoreCase) 
        ? $"http://example.com/{studentCode}/signalr"  // Link http -> Thử nghiệm trạng thái failed (Bài tập 2)
        : $"https://example.com/{studentCode}/signalr"); // Link https -> Trạng thái graded bình thường
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, repoUrl));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi tao luot nop: {error?.Code} - {error?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Da tao luot nop: {created!.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

// 3. SignalR: Đăng ký theo dõi lượt nộp vào nhóm riêng
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());

// 4. Chờ chuông graded hoặc timeout 10 giây
var completed = await Task.WhenAny(gradedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
if (completed != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// 5. REST: Đọc lại trạng thái chốt hạ (Nguồn sự thật)
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

// Thoát tự động sau khi nhận graded theo đúng slide 71

// --- Records DTO ---
record AssignmentDto(int Id, string Title, string Description, DateTimeOffset DueDate);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
