using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký SignalR vào DI
builder.Services.AddSignalR();

var app = builder.Build();

// Dữ liệu mẫu bài tập trong bộ nhớ
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Thiet ke endpoint va hub realtime...", true),
    new(2, "Bai tap Client", "Viet console client realtime...", true)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object(); // Khóa bảo vệ danh sách submissions

// 1. GET / : kiểm tra trạng thái dịch vụ
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// 2. GET /api/assignments : lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 3. GET /api/assignments/{assignmentId:int} : lấy chi tiết bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bài tap."))
        : Results.Ok(assignment);
});

// 4. POST /api/assignments/{assignmentId:int}/submissions : nộp bài và bắt đầu chấm nền
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (
    int assignmentId,
    CreateSubmissionRequest request,
    IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        now,
        now);

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Khởi động tác vụ chấm nền bất đồng bộ (không chờ)
    _ = SimulateGradingAsync(submission.Id, submission.RepositoryUrl, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 5. GET /api/submissions/{submissionId:guid} : đọc lại lượt nộp (nguồn sự thật)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

// Ánh xạ hub SignalR
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// --- Các phương thức static trợ giúp (đặt trước class) ---

static async Task SimulateGradingAsync(Guid id, string repoUrl, List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    // Sau 1s chuyển sang processing
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    // Sau 2s tiếp theo (tổng 3s) chuyển sang graded hoặc failed (Bài tập 2: URL không bắt đầu bằng https:// -> failed)
    await Task.Delay(TimeSpan.FromSeconds(2));
    string finalStatus = repoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? "graded" : "failed";
    await UpdateStatusAsync(id, finalStatus, list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status, List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)
    {
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0)
        {
            updated = list[i] with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            list[i] = updated;
        }
    }

    if (updated is null) return;

    Console.WriteLine($"[Push] {SubmissionGroups.ForSubmission(id)} -> {status}");
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}

// --- Hub và Classes ---

sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SubmissionGroups.ForSubmission(submissionId));
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {SubmissionGroups.ForSubmission(submissionId)}");
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// DTOs & Records
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record ApiError(string Code, string Message);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
