using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine("=== CHUONG TRINH CLIENT NOP BAI (Submission.Client) ===");
Console.WriteLine($"Ket noi den API tai: {apiBaseUrl}\n");

try
{
    // Nac 1: Lay danh sach bai tap dang mo
    Console.WriteLine("[Nac 1] Lay danh sach bai tap dang mo...");
    var assignmentsResponse = await http.GetAsync("/api/assignments");
    if (!assignmentsResponse.IsSuccessStatusCode)
    {
        Console.WriteLine("Khong the lay danh sach bai tap:");
        await PrintApiErrorAsync(assignmentsResponse);
        return;
    }

    var assignments = await assignmentsResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>();
    Console.WriteLine("Danh sach bai tap dang mo:");
    foreach (var a in (assignments ?? []).Where(x => x.IsOpen))
    {
        Console.WriteLine($"  - Id: {a.Id} | Tieu de: {a.Title} | Mo: {a.IsOpen}");
    }

    // Nac 2: Gui yeu cau nop bai
    var selectedAssignment = (assignments ?? []).FirstOrDefault(x => x.IsOpen);
    var selectedAssignmentId = selectedAssignment?.Id ?? 1;

    Console.WriteLine($"\n[Nac 2] Gui yeu cau nop bai cho bai tap Id={selectedAssignmentId}...");
    var createRequest = new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api");
    var createResponse = await http.PostAsJsonAsync($"/api/assignments/{selectedAssignmentId}/submissions", createRequest);

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        Console.WriteLine("Loi khi nop bai:");
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var locationUrl = createResponse.Headers.Location;
    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Nop bai thanh cong! (HTTP {(int)createResponse.StatusCode} {createResponse.StatusCode})");
    Console.WriteLine($"  - Header Location: {locationUrl}");
    Console.WriteLine($"  - Id luot nop: {created?.Id}");
    Console.WriteLine($"  - Ma sinh vien: {created?.StudentCode}");
    Console.WriteLine($"  - Kho ma nguon: {created?.RepositoryUrl}");
    Console.WriteLine($"  - Trang thai: {created?.Status}");
    Console.WriteLine($"  - Thoi gian: {created?.SubmittedAt:yyyy-MM-dd HH:mm:ss 'UTC'}");

    // Nac 3: Doc lai luot nop theo Location header
    Console.WriteLine($"\n[Nac 3] Doc lai luot nop theo Location header: {locationUrl}...");
    if (locationUrl is not null)
    {
        var getResponse = await http.GetAsync(locationUrl);
        if (getResponse.IsSuccessStatusCode)
        {
            var readBack = await getResponse.Content.ReadFromJsonAsync<SubmissionDto>();
            Console.WriteLine($"Doc lai thanh cong (HTTP {(int)getResponse.StatusCode} {getResponse.StatusCode}):");
            Console.WriteLine($"  - Id: {readBack?.Id}");
            Console.WriteLine($"  - AssignmentId: {readBack?.AssignmentId}");
            Console.WriteLine($"  - Ma sinh vien: {readBack?.StudentCode}");
            Console.WriteLine($"  - Kho ma nguon: {readBack?.RepositoryUrl}");
            Console.WriteLine($"  - Trang thai: {readBack?.Status}");
        }
        else
        {
            Console.WriteLine("Loi khi doc lai luot nop:");
            await PrintApiErrorAsync(getResponse);
        }
    }
    else
    {
        Console.WriteLine("Khong tim thay Location header trong phan hoi tao luot nop.");
    }

    // Nac 4: Co y gui yeu cau sai de kiem thu xu ly loi 400
    Console.WriteLine($"\n[Nac 4] Co y gui yeu cau rong de kiem thu xu ly loi 400...");
    var badRequest = new CreateSubmissionRequest("", "");
    var badResponse = await http.PostAsJsonAsync($"/api/assignments/{selectedAssignmentId}/submissions", badRequest);
    Console.WriteLine($"Nhan ma phan hoi: {(int)badResponse.StatusCode} {badResponse.StatusCode}");
    await PrintApiErrorAsync(badResponse);
    Console.WriteLine("Xu ly loi an toan, ung dung khong bi crash.");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\n[LOI KET NOI MANG]: Khong the ket noi den may chu API ({ex.Message}).");
}

static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;
    try
    {
        var error = await r.Content.ReadFromJsonAsync<ApiError>();
        if (error is not null && !string.IsNullOrEmpty(error.Code))
        {
            Console.WriteLine($"  [ApiError] Ma loi: {error.Code} | Thong diep: {error.Message}");
        }
        else
        {
            var rawText = await r.Content.ReadAsStringAsync();
            Console.WriteLine($"  [HTTP {(int)r.StatusCode}] {rawText}");
        }
    }
    catch
    {
        var rawText = await r.Content.ReadAsStringAsync();
        Console.WriteLine($"  [HTTP {(int)r.StatusCode}] {rawText}");
    }
}

// Ban sao DTO phia Client
public record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
public record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
public record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset SubmittedAt
);
public record ApiError(string Code, string Message);
