using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine($"=== CLIENT EDUSUBMIT KET NOI TOI: {apiBaseUrl} ===\n");

try {
    // -------------------------------------------------------------
    // NAC 1: Liet ke danh sach bai tap dang mo
    // -------------------------------------------------------------
    Console.WriteLine("--- [Nac 1] Liet ke danh sach bai tap ---");
    var listResponse = await http.GetAsync("/api/assignments");
    if (!listResponse.IsSuccessStatusCode) {
        await PrintApiErrorAsync(listResponse);
        return;
    }
    var assignments = await listResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>();
    Console.WriteLine("Danh sach bai tap:");
    foreach (var a in assignments ?? []) {
        var status = a.IsOpen ? "Dang mo" : "Da dong";
        Console.WriteLine($"  - [{a.Id}] {a.Title} ({status}): {a.Description}");
    }
    Console.WriteLine();

    // -------------------------------------------------------------
    // NAC 2: Nop bai (POST /api/assignments/{id}/submissions)
    // -------------------------------------------------------------
    Console.WriteLine("--- [Nac 2] Nop bai tap ---");
    var selectedAssignmentId = assignments?.FirstOrDefault(a => a.IsOpen)?.Id ?? 1;
    var submissionRequest = new CreateSubmissionRequest(
        "dtc245200494",
        "https://github.com/dtc245200494-hue/CNTTK23C"
    );

    Console.WriteLine($"Gui luot nop cho bai tap {selectedAssignmentId}...");
    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        submissionRequest
    );

    // Kiem tra ma trang thai truoc khi doc body
    if (createResponse.StatusCode != HttpStatusCode.Created) {
        Console.WriteLine($"Tao that bai. Ma trang thai: {(int)createResponse.StatusCode} {createResponse.StatusCode}");
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    var locationHeader = createResponse.Headers.Location?.ToString() ?? $"/api/submissions/{created?.Id}";
    Console.WriteLine($"Da tao luot nop thanh cong!");
    Console.WriteLine($"  - Ma luot nop (Id): {created?.Id}");
    Console.WriteLine($"  - Ma sinh vien    : {created?.StudentCode}");
    Console.WriteLine($"  - Kho ma nguon    : {created?.RepositoryUrl}");
    Console.WriteLine($"  - Trang thai      : {created?.Status}");
    Console.WriteLine($"  - Thoi gian nhan  : {created?.ReceivedAt:yyyy-MM-dd HH:mm:ss UTC}");
    Console.WriteLine($"  - Header Location : {locationHeader}\n");

    // -------------------------------------------------------------
    // NAC 3: Doc lai luot nop theo Location
    // -------------------------------------------------------------
    Console.WriteLine("--- [Nac 3] Doc lai luot nop theo Location ---");
    Console.WriteLine($"Goi GET toi: {locationHeader}...");
    var getResponse = await http.GetAsync(locationHeader);
    if (getResponse.StatusCode != HttpStatusCode.OK) {
        Console.WriteLine($"Doc lai that bai. Ma: {(int)getResponse.StatusCode}");
        await PrintApiErrorAsync(getResponse);
    } else {
        var readBack = await getResponse.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine($"Doc lai thanh cong tu server:");
        Console.WriteLine($"  - Id          : {readBack?.Id}");
        Console.WriteLine($"  - AssignmentId: {readBack?.AssignmentId}");
        Console.WriteLine($"  - Sinh vien   : {readBack?.StudentCode}");
        Console.WriteLine($"  - Repo URL    : {readBack?.RepositoryUrl}");
        Console.WriteLine($"  - Trang thai  : {readBack?.Status}\n");
    }

    // -------------------------------------------------------------
    // NAC 4: Thu gui yeu cau loi (khong sap chuong trinh)
    // -------------------------------------------------------------
    Console.WriteLine("--- [Nac 4] Thu gui cac yeu cau loi ---");

    // Ca 4.1: Gui body rong -> mong doi 400 Bad Request
    Console.WriteLine("Ca 4.1: Gui body rong (thieu StudentCode & RepositoryUrl)...");
    var badBodyRequest = new CreateSubmissionRequest("", "");
    var badBodyResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        badBodyRequest
    );
    Console.WriteLine($"Ma phan hoi: {(int)badBodyResponse.StatusCode} {badBodyResponse.StatusCode}");
    await PrintApiErrorAsync(badBodyResponse);
    Console.WriteLine();

    // Ca 4.2: Nop vao bai tap khong ton tai (999) -> mong doi 404 Not Found
    Console.WriteLine("Ca 4.2: Nop bai vao bai tap 999 khong ton tai...");
    var notFoundResponse = await http.PostAsJsonAsync(
        "/api/assignments/999/submissions",
        submissionRequest
    );
    Console.WriteLine($"Ma phan hoi: {(int)notFoundResponse.StatusCode} {notFoundResponse.StatusCode}");
    await PrintApiErrorAsync(notFoundResponse);
    Console.WriteLine();

    // Ca 4.3: Doc chi tiet bai tap 999 (dung GetAsync kiem tra ma truoc, tranh sap nhu slide 85)
    Console.WriteLine("Ca 4.3: GET /api/assignments/999 (dung GetAsync, khong bi sap boi 404)...");
    var getNonExistResponse = await http.GetAsync("/api/assignments/999");
    Console.WriteLine($"Ma phan hoi: {(int)getNonExistResponse.StatusCode} {getNonExistResponse.StatusCode}");
    await PrintApiErrorAsync(getNonExistResponse);
    Console.WriteLine();

} catch (HttpRequestException ex) {
    Console.WriteLine($"\n[LOI MANG] Khong the ket noi toi may chu tai {apiBaseUrl}.");
    Console.WriteLine($"Chi tiet: {ex.Message}");
}

// Ham dung chung doc va in ApiError
static async Task PrintApiErrorAsync(HttpResponseMessage response) {
    if (response.IsSuccessStatusCode) return;
    try {
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        if (error is not null) {
            Console.WriteLine($"  -> ApiError: [{error.Code}] {error.Message}");
        } else {
            var raw = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"  -> Phuc dap khong co ApiError: {raw}");
        }
    } catch {
        var raw = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"  -> Body van ban tu framework: {raw}");
    }
}

// Ban sao DTO phia Client (doc lap hoan toan voi Server)
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
