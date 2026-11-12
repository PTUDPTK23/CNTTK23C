using System.Net;
using System.Net.Http.Json;

// Lấy địa chỉ API từ dòng lệnh (nếu có), mặc định là http://localhost:5000
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

// 1. Lấy danh sách bài tập từ API Server
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");

Console.WriteLine("Danh sach bai tap dang mo:");
foreach (var a in assignments ?? []) {
    Console.WriteLine($"- {a.Id}: {a.Title}");
}

// 2. Chọn bài tập đầu tiên để nộp bài thử nghiệm
var selectedAssignmentID = assignments?.FirstOrDefault()?.Id ?? 1;

var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentID}/submissions",
    new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api")
);

// Kiểm tra mã trạng thái trả về (nếu không phải 201 Created thì đọc lỗi ApiError)
if (!createResponse.IsSuccessStatusCode) {
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");

// 3. Đọc lại lượt nộp thông qua đường dẫn từ header Location
var getSubmissionResponse = await http.GetAsync(createResponse.Headers.Location);
if (getSubmissionResponse.IsSuccessStatusCode) {
    var readBack = await getSubmissionResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Doc lại luot nop: {readBack?.StudentCode} - {readBack?.RepositoryUrl}");
}

// Hàm hỗ trợ in lỗi đồng nhất từ ApiError của server
static async Task PrintApiErrorAsync(HttpResponseMessage r) {
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"\nThu gui yeu cau loi: {r.StatusCode}");
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// Khai báo các hợp đồng (record) dùng chung giữa Client và Server
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id, 
    int AssignmentId, 
    string StudentCode, 
    string RepositoryUrl, 
    string Status, 
    DateTimeOffset ReceivedAt
);
record ApiError(string Code, string Message);