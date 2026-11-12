// EduSubmit - Submission.Client (Máy khách dòng lệnh)
// Chạy: dotnet run --project src/Submission.Client -- http://localhost:5000

using System.Net.Http.Json;

// Lấy địa chỉ API từ tham số dòng lệnh hoặc mặc định là http://localhost:5000
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// 1. GET /api/assignments : Lấy và hiển thị danh sách bài tập
var assignments = await http.GetFromJsonAsync<List<Assignment>>("/api/assignments");
Console.WriteLine("Danh sach bai tap:");
foreach (var assignment in assignments ?? new List<Assignment>())
{
    Console.WriteLine($"- {assignment.Id}: {assignment.Title}");
}

// 2. POST /api/submissions : Nộp bài tập
var request = new CreateSubmissionRequest(
    AssignmentId: 1,
    StudentCode: "dtc245200568",
    RepositoryUrl: "https://example.com/dtc245200568/bai-tap-1");

var response = await http.PostAsJsonAsync("/api/submissions", request);
response.EnsureSuccessStatusCode();

var result = await response.Content.ReadFromJsonAsync<SubmissionResult>();
Console.WriteLine($"Da nop bai. Ma luot nop: {result?.Id}");
Console.WriteLine($"Trang thai: {result?.Status}");

// 3. GET /api/submissions/{id} : Đọc lại lượt nộp vừa tạo từ API (Bước 12)
if (result != null)
{
    var fetched = await http.GetFromJsonAsync<SubmissionResult>($"/api/submissions/{result.Id}");
    Console.WriteLine("Doc lai tu API (GET /api/submissions/{id}):");
    Console.WriteLine($"- Ma luot nop: {fetched?.Id}");
    Console.WriteLine($"- Ma sinh vien: {fetched?.StudentCode}");
    Console.WriteLine($"- Bai tap so: {fetched?.AssignmentId}");
    Console.WriteLine($"- Trang thai: {fetched?.Status}");
    Console.WriteLine($"- Thoi gian nhan: {fetched?.ReceivedAt:yyyy-MM-dd HH:mm:ss} (UTC)");
}

// Client tự định nghĩa lại các record độc lập, không tham chiếu DLL của server
record Assignment(int Id, string Title, string Description);

record CreateSubmissionRequest(
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl);

record SubmissionResult(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string Status,
    DateTimeOffset ReceivedAt);
