using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

try
{
    // ===== Nac 1: liet ke bai tap dang mo =====
    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var openAssignments = (assignments ?? []).Where(a => a.IsOpen).ToList();

    Console.WriteLine("Danh sach bai tap dang mo:");
    foreach (var a in openAssignments)
        Console.WriteLine($"- {a.Id}: {a.Title}");

    var selectedAssignmentId = openAssignments.FirstOrDefault()?.Id ?? 1;

    // ===== Nac 2: nop bai, kiem tra ma trang thai TRUOC khi doc body =====
    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api"));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
    Console.WriteLine($"Trang thai ban dau: {created?.Status}");

    // ===== Nac 3: doc lai luot nop theo header Location =====
    var location = createResponse.Headers.Location;
    var readBack = await http.GetAsync(location);
    if (readBack.StatusCode == HttpStatusCode.OK)
    {
        var submission = await readBack.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine($"Doc lai luot nop: {submission?.StudentCode} - {submission?.RepositoryUrl}");
    }
    else
    {
        await PrintApiErrorAsync(readBack);
    }

    // ===== Nac 4: co y gui du lieu sai de quan sat loi 400 =====
    var invalidResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest("", ""));

    Console.WriteLine($"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException ex)
{
    // Loi mang (khong co phan hoi): khac voi loi HTTP 4xx/5xx
    Console.WriteLine($"\nKhong ket noi duoc toi API ({apiBaseUrl}): {ex.Message}");
    Console.WriteLine("Hay kiem tra Submission.Api da chay chua va dung cong chua.");
}

// Mot ham doc loi chung cho moi phan hoi loi, nho API tra cung mot dang ApiError
static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode) return;

    try
    {
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"{error?.Code}: {error?.Message}");
    }
    catch
    {
        // Loi do framework tra (404/405/415...) khong co ApiError
        Console.WriteLine($"{(int)response.StatusCode} {response.StatusCode} (khong co ApiError)");
    }
}

// Ban sao DTO cua may chu: hai ben chi chung hop dong JSON, khong chung code
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
