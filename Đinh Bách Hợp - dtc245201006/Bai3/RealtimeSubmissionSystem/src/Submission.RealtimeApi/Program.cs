using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero)),
    new(2, "Bai tap da dong", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero))
};
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new { service = "RealtimeApi", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

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

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) => // MỚI: thêm tham số này
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong co bai tap {assignmentId}."));
    if (DateTimeOffset.UtcNow > assignment.Deadline)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da het han nop."));
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_REQUEST", "Thieu studentCode hoac repositoryUrl."));

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(Guid.NewGuid(), assignmentId, request.StudentCode,
        request.RepositoryUrl, "received", now, now);

    lock (submissionsLock) { submissions.Add(submission); }

    // MỚI: chấm bài ở nền, KHÔNG await nên POST trả 201 ngay
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// MỚI: 2 hàm này phải nằm SAU app.Run() và TRƯỚC các record/class
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
    lock (gate) // ① cập nhật dữ liệu trước
    {
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0)
        {
            updated = list[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            list[i] = updated;
        }
    }
    if (updated is null) return;

    var group = SubmissionGroups.ForSubmission(id);
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt); // ② dữ liệu tối thiểu
    Console.WriteLine($"[Push] {group} -> {status}");
    await hub.Clients.Group(group).SendAsync("SubmissionStatusChanged", msg); // ③ đẩy đúng nhóm
}

record AssignmentDto(int Id, string Title, DateTimeOffset Deadline);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt); // MỚI
record ApiError(string Code, string Message);

sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        var group = SubmissionGroups.ForSubmission(submissionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {group}");
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}