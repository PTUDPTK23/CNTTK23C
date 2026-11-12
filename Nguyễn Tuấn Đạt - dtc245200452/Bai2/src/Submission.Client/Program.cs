using System.Net;
using System.Net.Http.Json;

// Địa chỉ máy chủ API (mặc định http://localhost:5000, có thể nhận qua tham số args[0])
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine($"=== KẾT NỐI TỚI MÁY CHỦ: {apiBaseUrl} ===\n");

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

try
{
    // =========================================================================
    // NẤC 1: LIỆT KÊ DANH SÁCH BÀI TẬP ĐANG MỞ (Slide 84)
    // =========================================================================
    Console.WriteLine("Danh sach bai tap dang mo:");
    var assignmentsResponse = await http.GetAsync("/api/assignments");
    if (!assignmentsResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(assignmentsResponse);
        return;
    }

    var assignments = await assignmentsResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>() ?? [];
    foreach (var a in assignments.Where(x => x.IsOpen))
    {
        Console.WriteLine($"- {a.Id}: {a.Title}");
    }

    // =========================================================================
    // NẤC 2: NỘP BÀI (Slide 86)
    // =========================================================================
    var selectedAssignmentId = assignments.FirstOrDefault(x => x.IsOpen)?.Id ?? 1;
    var newSubmission = new CreateSubmissionRequest("dtc245200452", "https://github.com/dtc245200452-hub/Bai2");
    
    var createResponse = await http.PostAsJsonAsync($"/api/assignments/{selectedAssignmentId}/submissions", newSubmission);

    // QUY TẮC VÀNG: Xem mã trạng thái trước, đọc body sau
    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Da tao luot nop: {created?.Id}");
    Console.WriteLine($"Trang thai ban dau: {created?.Status}");

    // =========================================================================
    // NẤC 3: ĐỌC LẠI LƯỢT NỘP THEO LOCATION (Slide 83, 86)
    // =========================================================================
    var readLocation = createResponse.Headers.Location?.ToString() ?? $"/api/submissions/{created?.Id}";
    var getResponse = await http.GetAsync(readLocation);
    if (getResponse.IsSuccessStatusCode)
    {
        var fetched = await getResponse.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine($"Doc lai luot nop: {fetched?.StudentCode} - {fetched?.RepositoryUrl}");
    }
    else
    {
        await PrintApiErrorAsync(getResponse);
    }

    // =========================================================================
    // NẤC 4: THỬ GỬI YÊU CẦU LỖI (Dữ liệu rỗng -> Nhận 400 và không bị crash) (Slide 87)
    // =========================================================================
    Console.WriteLine("Thu gui yeu cau loi: 400 BadRequest");
    var badSubmission = new CreateSubmissionRequest("", "");
    var badResponse = await http.PostAsJsonAsync($"/api/assignments/{selectedAssignmentId}/submissions", badSubmission);
    await PrintApiErrorAsync(badResponse);
}
catch (HttpRequestException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[LOI MANG / CONNECTION REFUSED]: Khong the ket noi toi may chu API! {ex.Message}");
    Console.ResetColor();
}

// Hàm dùng chung đọc và hiển thị ApiError
static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;

    try
    {
        var error = await r.Content.ReadFromJsonAsync<ApiError>();
        if (error is not null)
        {
            Console.WriteLine($"{error.Code}: {error.Message}");
        }
        else
        {
            Console.WriteLine($"{(int)r.StatusCode} {r.StatusCode}");
        }
    }
    catch
    {
        Console.WriteLine($"{(int)r.StatusCode} {r.StatusCode}");
    }
}

// Khai báo lại các DTO tại Client (Hợp đồng JSON độc lập, không dùng chung class)
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
