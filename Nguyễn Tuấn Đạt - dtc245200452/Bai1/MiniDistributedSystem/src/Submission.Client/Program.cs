using System.Net.Http.Json;

// EduSubmit - Lat cat 1 (Bai 1, Buoc 9-12): Submission.Client (vai tro MAY KHACH)
// Chay: dotnet run --project src/Submission.Client -- http://localhost:5000

// Dia chi API nhan tu tham so dong lenh de khong gan cung trong ma nguon
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Yeu cau 1: GET /api/assignments -> lay danh sach bai tap
var assignments = await http.GetFromJsonAsync<List<Assignment>>("/api/assignments");
Console.WriteLine("Danh sach bai tap:");
foreach (var assignment in assignments ?? [])
{
    Console.WriteLine($"- {assignment.Id}: {assignment.Title}");
}

// Yeu cau 2: POST /api/submissions -> gui mot luot nop bai
var request = new CreateSubmissionRequest(
    AssignmentId: 1,
    StudentCode: "dtc245200452",
    RepositoryUrl: "https://github.com/dtc245200452-hub/CNTTK23C");

var response = await http.PostAsJsonAsync("/api/submissions", request);
response.EnsureSuccessStatusCode(); // Dung bang ngoai le neu ma HTTP khong thuoc nhom 2xx

var result = await response.Content.ReadFromJsonAsync<SubmissionResult>();
Console.WriteLine($"Da nop bai. Ma luot nop: {result?.Id}");
Console.WriteLine($"Trang thai: {result?.Status}");

// Yeu cau 3 (Mo rong Buoc 12): Doc lai luot nop vua tao theo header Location / ID
if (result is not null)
{
    var location = response.Headers.Location?.ToString() ?? $"/api/submissions/{result.Id}";
    var readResponse = await http.GetAsync(location);
    readResponse.EnsureSuccessStatusCode();

    var verified = await readResponse.Content.ReadFromJsonAsync<SubmissionResult>();
    Console.WriteLine($"[Mo rong] Doc lai thanh cong: {verified?.Id} (SV: {verified?.StudentCode}, trang thai: {verified?.Status})");
}

// Client tu dinh nghia DTO rieng theo hop dong JSON, khong tham chieu code cua API
record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);
