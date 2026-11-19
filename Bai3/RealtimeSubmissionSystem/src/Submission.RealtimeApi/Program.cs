using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai 1: REST Co ban", true),
    new(2, "Bai 2: Xu ly loi & Validation", true),
    new(3, "Bai 3: SignalR Realtime (Da dong)", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();
app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));
// 1. Lay danh sach bai tap
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 2. Nop bai moi (POST)
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest req,
     IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap {assignmentId}."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan bai nop."));

    if (string.IsNullOrWhiteSpace(req.StudentCode) ||
        string.IsNullOrWhiteSpace(req.RepositoryUrl) ||
        !Uri.TryCreate(req.RepositoryUrl, UriKind.Absolute, out _))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "StudentCode hoac RepositoryUrl khong hop le."));
    }

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

    // Khoi dong cham bai o nen, KHONG cho
    _ = SimulateGradingAsync(submission.Id, submissions,
                             submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 3. Xem chi tiet luot nop (GET)
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

// 4. Anh xa Hub SignalR
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// --- CAC LOP VA PHUONG THUC HO TRO (DAT SAU app.Run()) ---

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
    lock (gate) // 1. Cap nhat du lieu truoc
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

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status,
                                          updated.UpdatedAt); // 2. Du lieu toi thieu
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
             .SendAsync("SubmissionStatusChanged", msg); // 3. Day dung nhom

    Console.WriteLine($"[Push] {id} -> {status}");
}

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

record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record ApiError(string Code, string Message);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);