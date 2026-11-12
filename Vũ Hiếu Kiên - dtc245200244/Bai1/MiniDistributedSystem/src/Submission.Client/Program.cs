// EduSubmit - Bai 1: Submission.Client (vai tro MAY KHACH)
// Chay: dotnet run --project src/Submission.Client -- http://localhost:5000

using System.Net.Http.Json;

// Dia chi API lay tu tham so dong lenh -> khong gan cung vao mot moi truong
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Yeu cau 1: GET /api/assignments
var assignments = await http.GetFromJsonAsync<List<Assignment>>("/api/assignments");
Console.WriteLine("Danh sach bai tap:");

foreach (var assignment in assignments ?? new List<Assignment>())
{
    Console.WriteLine($"- {assignment.Id}: {assignment.Title} ({assignment.Description})");
}

// Yeu cau 2: POST /api/submissions
var request = new CreateSubmissionRequest(
    AssignmentId: 1,
    StudentCode: "dtc245200244",
    RepositoryUrl: "https://github.com/dtc245200244-dotcom/CNTTK23C");

var response = await http.PostAsJsonAsync("/api/submissions", request);
response.EnsureSuccessStatusCode(); // luon kiem tra ma trang thai truoc khi doc ket qua

var result = await response.Content.ReadFromJsonAsync<SubmissionResult>();
Console.WriteLine($"Da nop bai. Ma luot nop: {result?.Id}");
Console.WriteLine($"Trang thai: {result?.Status}");

// Yeu cau 3 (Bai mo rong Buoc 12): GET /api/submissions/{id} de doc lai luot nop vua tao
if (result is not null)
{
    var detail = await http.GetFromJsonAsync<SubmissionResult>($"/api/submissions/{result.Id}");
    Console.WriteLine($"Tra cuu lai luot nop: MSSV={detail?.StudentCode}, BaiTap={detail?.AssignmentId}, TrangThai={detail?.Status}");
}

// Cung "hinh dang" JSON voi API - hai ben chi thong nhat hop dong du lieu,
// may khach KHONG tham chieu ma nguon cua may chu.
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
