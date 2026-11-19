using System.Net;
using System.Net.Http.Json;

// Lấy địa chỉ API từ tham số dòng lệnh hoặc mặc định localhost:5000 (Slide 84)
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

// =========================================================
// NẤC 1: LẤY DANH SÁCH BÀI TẬP ĐANG MỞ (Slide 84)
// =========================================================
Console.WriteLine("Danh sach bai tap dang mo:");
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
foreach (var a in assignments ?? []) {
    if (a.IsOpen) {
        Console.WriteLine($" - {a.Id}: {a.Title}");
    }
}

// =========================================================
// NẤC 2: NỘP BÀI VÀ KIỂM TRA MÃ 201 TRƯỚC KHI ĐỌC BODY (Slide 86)
// =========================================================
var selectedAssignmentId = assignments?.FirstOrDefault(a => a.IsOpen)?.Id ?? 1;

var createRequest = new CreateSubmissionRequest("SV001", "https://github.com/dtc245340007-collab/EduSubmit");
var createResponse = await http.PostAsJsonAsync($"/api/assignments/{selectedAssignmentId}/submissions", createRequest);

if (createResponse.StatusCode != HttpStatusCode.Created) {
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");
Console.WriteLine($"Doc lai tai header Location: {createResponse.Headers.Location}");

// =========================================================
// NẤC 3: ĐỌC LẠI CHI TIẾT THEO LOCATION (Slide 87)
// =========================================================
if (createResponse.Headers.Location != null) {
    var detailResponse = await http.GetAsync(createResponse.Headers.Location);
    if (detailResponse.IsSuccessStatusCode) {
        var detail = await detailResponse.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine($"Doc lai luot nop thanh cong: {detail?.StudentCode} - {detail?.RepositoryUrl}");
    }
}

// =========================================================
// NẤC 4: CỐ Ý GỬI SAI ĐỂ QUAN SÁT LỖI 400 (Slide 86 - 87)
// =========================================================
Console.WriteLine("\nThu gui yeu cau loi (thieu link repo):");
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions", 
    new CreateSubmissionRequest("SV001", ""));

Console.WriteLine($"Thu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

// Hàm đọc ApiError dùng chung cho mọi lỗi HTTP (Slide 86)
static async Task PrintApiErrorAsync(HttpResponseMessage r) {
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// ---------------------------------------------------------
// KHAI BÁO DTO PHÍA CLIENT (Slide 84, 86)
// ---------------------------------------------------------
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);