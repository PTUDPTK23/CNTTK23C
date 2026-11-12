using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

try
{
    // 1. Lay danh sach bai tap dang mo.
    Console.WriteLine("=== 1. DANH SACH BAI TAP DANG MO ===");

    using var listResponse =
        await http.GetAsync("/api/assignments");

    if (listResponse.StatusCode != HttpStatusCode.OK)
    {
        await PrintApiErrorAsync(listResponse);
        return;
    }

    var assignments = await listResponse.Content
        .ReadFromJsonAsync<List<AssignmentDto>>();

    var openAssignments = assignments?
        .Where(a => a.IsOpen)
        .ToList() ?? [];

    foreach (var assignment in openAssignments)
    {
        Console.WriteLine(
            $"- {assignment.Id}: {assignment.Title}");
    }

    var selected = openAssignments.FirstOrDefault();

    if (selected is null)
    {
        Console.WriteLine("Khong co bai tap dang mo.");
        return;
    }

    // 2. Nop bai.
    Console.WriteLine("\n=== 2. NOP BAI ===");

    var request = new CreateSubmissionRequest(
        "SV001",
        "https://example.com/sv001/rest-api");

    using var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        request);

    Console.WriteLine($"HTTP: {(int)createResponse.StatusCode}");

    // Kiem tra ma trang thai truoc khi doc body.
    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content
        .ReadFromJsonAsync<SubmissionDto>();

    Console.WriteLine($"Da tao luot nop: {created?.Id}");
    Console.WriteLine($"Trang thai: {created?.Status}");

    // 3. Doc lai bang dia chi trong Location.
    Console.WriteLine("\n=== 3. DOC LAI LUOT NOP ===");

    var location = createResponse.Headers.Location;

    if (location is null)
    {
        Console.WriteLine("API khong tra header Location.");
        return;
    }

    Console.WriteLine($"Location: {location}");

    using var readResponse = await http.GetAsync(location);

    if (readResponse.StatusCode != HttpStatusCode.OK)
    {
        await PrintApiErrorAsync(readResponse);
        return;
    }

    var saved = await readResponse.Content
        .ReadFromJsonAsync<SubmissionDto>();

    Console.WriteLine($"Ma sinh vien: {saved?.StudentCode}");
    Console.WriteLine($"Kho ma nguon: {saved?.RepositoryUrl}");
    Console.WriteLine($"Trang thai: {saved?.Status}");

    // 4. Co y gui du lieu rong de kiem tra loi.
    Console.WriteLine("\n=== 4. THU GUI YEU CAU SAI ===");

    using var badResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest("", ""));

    Console.WriteLine($"HTTP: {(int)badResponse.StatusCode}");

    if (!badResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(badResponse);
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Loi ket noi API: {ex.Message}");
}
catch (TaskCanceledException)
{
    Console.WriteLine("Yeu cau bi huy hoac het thoi gian cho.");
}

// Doc loi ApiError; van xu ly duoc body rong hoac khong phai JSON.
static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    var body = await response.Content.ReadAsStringAsync();

    try
    {
        var error = JsonSerializer.Deserialize<ApiError>(
            body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (error is not null &&
            !string.IsNullOrWhiteSpace(error.Code))
        {
            Console.WriteLine($"{error.Code}: {error.Message}");
            return;
        }
    }
    catch (JsonException)
    {
        // Loi framework co the khong tra JSON.
    }

    Console.WriteLine(
        $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
}

// Client khai bao cac DTO rieng, khop voi hop dong JSON cua API.
record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);

record ApiError(string Code, string Message);