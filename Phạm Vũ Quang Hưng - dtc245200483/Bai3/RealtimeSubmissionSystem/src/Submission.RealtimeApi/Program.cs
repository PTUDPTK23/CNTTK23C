using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký SignalR vào DI
builder.Services.AddSignalR();

var app = builder.Build();

// Dữ liệu mẫu bài tập
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Xay dung he thong nop bai realtime", DateTimeOffset.UtcNow.AddDays(7))
};

// Danh sách lượt nộp và lock đồng bộ hóa
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

// Endpoint kiểm tra trạng thái dịch vụ
app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));

// 1. GET danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 2. GET chi tiết lượt nộp (nguồn sự thật)
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

// 3. POST tạo lượt nộp bài
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
    {
        // Kiểm tra tồn tại bài tập
        var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment is null)
        {
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
        }

        // Kiểm tra hợp lệ dữ liệu
        if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Ma sinh vien va repository URL khong duoc de trong."));
        }

        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(
            Guid.NewGuid(),
            assignmentId,
            request.StudentCode,
            request.RepositoryUrl,
            "received",
            now,
            now
        );

        lock (submissionsLock)
        {
            submissions.Add(submission);
        }

        // Khởi động chấm bài ở nền, KHÔNG chờ (Task chưa chờ)
        _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

// Ánh xạ tuyến Hub thời gian thực
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// --- Tác vụ chấm bài nền (dùng IHubContext để gửi từ bên ngoài Hub) ---
static async Task SimulateGradingAsync(
    Guid id,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    // Giả lập nhận bài -> bắt đầu chấm (1s)
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    // Giả lập đang chấm -> hoàn thành chấm (2s)
    await Task.Delay(TimeSpan.FromSeconds(2));

    string finalStatus = "graded";
    lock (gate)
    {
        var item = list.FirstOrDefault(s => s.Id == id);
        // Vận dụng Bài tập 2: URL không bắt đầu bằng https:// thì trạng thái là failed
        if (item is not null && !item.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            finalStatus = "failed";
        }
    }

    await UpdateStatusAsync(id, finalStatus, list, gate, hub);
}

static async Task UpdateStatusAsync(
    Guid id,
    string status,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
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

    Console.WriteLine($"[Push] submission:{id} -> {status}");

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}

// --- Hub thời gian thực ---
sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        var groupName = SubmissionGroups.ForSubmission(submissionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {groupName}");
    }
}

// Lớp định danh nhóm dùng chung
static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// --- Records DTO ---
record AssignmentDto(int Id, string Title, string Description, DateTimeOffset DueDate);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
