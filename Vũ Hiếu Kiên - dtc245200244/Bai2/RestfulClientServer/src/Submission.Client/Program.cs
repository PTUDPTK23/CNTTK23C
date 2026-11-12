// EduSubmit - Bai 2: Submission.Client (May khach console - chuoi 4 nac)
// Chay: dotnet run --project src/Submission.Client -- http://localhost:5000

using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Nac 1: Lay danh sach bai tap dang mo
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
Console.WriteLine("Danh sach bai tap dang mo:");
foreach (var a in (assignments ?? []).Where(x => x.IsOpen))
{
    Console.WriteLine($"- {a.Id}: {a.Title}");
}

// Nac 2: Nop bai va kiem tra ma trang thai 201 TRUOC khi doc body
var selectedAssignmentId = assignments?.FirstOrDefault(x => x.IsOpen)?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
var location = createResponse.Headers.Location;
Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");
Console.WriteLine($"Location: {location}");

// Nac 3: Doc lai luot nop theo dung dia chi trong Header Location
if (location is not null)
{
    var detailResponse = await http.GetAsync(location);
    if (!detailResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(detailResponse);
        return;
    }

    var detail = await detailResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Doc lai luot nop: {detail?.StudentCode} - {detail?.RepositoryUrl}");
}

// Nac 4: Co y gui du lieu sai de quan sat loi 400 va doc ApiError khong bi sap
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", ""));

Console.WriteLine($"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// Khai bao ban sao DTO o phia Client (khong tham chieu project Api)
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
