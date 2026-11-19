using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
// Việc 1: đăng ký dịch vụ SignalR vào DI
builder.Services.AddSignalR();

var app = builder.Build();

// Nguồn dữ liệu (giống Bài 2): bài tập + lượt nộp trong bộ nhớ
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", IsOpen: true),
    new(2, "Bai tap GraphQL", IsOpen: false)   // đã đóng -> POST trả 400 CLOSED
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();   // tác vụ nền + HTTP cùng sửa

// ---------- REST (nguồn sự thật, giữ nguyên Bài 2) ----------

app.MapGet("/", () => Results.Ok(
    new { service = "SubmissionRealtimeApi", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Bai tap khong ton tai."))
        : Results.Ok(assignment);
});

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    lock (submissionsLock) {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Luot nop khong ton tai."))
            : Results.Ok(submission);
    }
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest? request,
     IHubContext<SubmissionHub> hubContext) => {

        // 404: bài tập không tồn tại
        var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment is null)
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Bai tap khong ton tai."));

        // 400: bài tập đã đóng
        if (!assignment.IsOpen)
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong nop."));

        // 400: thiếu dữ liệu
        if (request is null
            || string.IsNullOrWhiteSpace(request.StudentCode)
            || string.IsNullOrWhiteSpace(request.RepositoryUrl))
            return Results.BadRequest(new ApiError("INVALID_REQUEST", "Thieu studentCode hoac repositoryUrl."));

        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(Guid.NewGuid(), assignmentId,
            request.StudentCode.Trim(), request.RepositoryUrl.Trim(),
            "received", now, now);

        lock (submissionsLock) { submissions.Add(submission); }

        // khởi động chấm bài ở nền, KHÔNG chờ -> POST trả 201 ngay
        _ = SimulateGradingAsync(submission.Id, submissions,
                                 submissionsLock, hubContext);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

// Việc 2: ánh xạ hub vào tuyến /hubs/submissions
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// Tác vụ chấm bài giả lập (hệ thật: hàng đợi + worker - Bài 6)
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
    lock (gate) {                                  // ① cập nhật dữ liệu TRƯỚC
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0) { updated = list[i] with { Status = status,
                                    UpdatedAt = DateTimeOffset.UtcNow };
                      list[i] = updated; }
    }
    if (updated is null) return;

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status,
                                          updated.UpdatedAt);   // ② dữ liệu tối thiểu
    Console.WriteLine($"[Push] {SubmissionGroups.ForSubmission(id)} -> {status}");
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
             .SendAsync("SubmissionStatusChanged", msg);   // ③ đẩy đúng nhóm
}

// ---------- SignalR ----------

sealed class SubmissionHub : Hub
{
    // Máy khách gọi sau khi có id từ POST: vào nhóm submission:{id}
    public Task WatchSubmission(string submissionId)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi submission:{submissionId}");
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId));
    }
}

static class SubmissionGroups
{
    // Một chỗ duy nhất sinh tên nhóm -> hai phía không lệch tên
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// ---------- DTO (giống Bài 2, thêm UpdatedAt) ----------

record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status,
    DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
