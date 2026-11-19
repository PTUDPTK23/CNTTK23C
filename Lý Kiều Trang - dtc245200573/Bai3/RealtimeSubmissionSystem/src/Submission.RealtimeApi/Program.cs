using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Bước 3: Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Dữ liệu mẫu danh sách bài tập
var assignments = new List<AssignmentDto>
{
    new(1, "Lap trinh Web nang cao", "Bai tap 1: REST API co ban", true),
    new(2, "Lap trinh mang", "Bai tap 2: Giao tiep thoi gian thuc", true),
    new(3, "Co so du lieu", "Bai tap 3: Toi uu truy van", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

// 1. Lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 2. Nộp bài mới (POST) - Bước 4.1: nhận IHubContext và chạy chấm bài ở nền
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest req, IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap {assignmentId}."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong han nop."));

    if (string.IsNullOrWhiteSpace(req.StudentCode) || string.IsNullOrWhiteSpace(req.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Ma sinh vien va duong dan repo khong duoc de trong."));

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: req.StudentCode.Trim(),
        RepositoryUrl: req.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: now,
        UpdatedAt: now
    );

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Khởi động chấm bài ở nền, KHÔNG await
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 3. Tra cứu trạng thái một lượt nộp bài (GET)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", $"Khong tim thay luot nop {submissionId}."))
            : Results.Ok(submission);
    }
});

// Ánh xạ Hub
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// ===== BƯỚC 4.2: TÁC VỤ NỀN GIẢ LẬP CHẤM BÀI =====

static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate) // 1. Cập nhật dữ liệu trước
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

    // 2. Tạo thông điệp gọn nhẹ 3 trường
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    Console.WriteLine($"[Push] {msg.SubmissionId} -> {msg.Status} ({msg.UpdatedAt:HH:mm:ss})");

    // 3. Đẩy thông báo tới đúng nhóm của lượt nộp
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}

// ===== CÁC LỚP DỮ LIỆU & HUB =====

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId));
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

record AssignmentDto(int Id, string CourseName, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt
);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);