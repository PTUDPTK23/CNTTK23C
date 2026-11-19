
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// 1. Dang ky dich vu SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Du lieu bai tap
var assignments = new List<AssignmentDto>
{
    new AssignmentDto(1, "Bai tap SignalR", true)
};

// Danh sach bai nop
var submissions = new List<SubmissionDto>();

// Khoa du lieu khi HTTP va tac vu nen cung truy cap
var submissionsLock = new object();

// 2. REST API
app.MapGet("/", () => Results.Ok(
    new { service = "SubmissionApi", status = "running" }));

// GET danh sach bai tap
app.MapGet("/api/assignments", () =>
    Results.Ok(assignments));

// POST tao bai nop
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    async (
        int assignmentId,
        CreateSubmissionRequest request,
        IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId);

    // Kiem tra bai tap ton tai
    if (assignment is null)
        return Results.NotFound(
            new ApiError("ASSIGNMENT_NOT_FOUND",
                "Khong tim thay bai tap"));

    // Kiem tra bai tap da dong chua
    if (!assignment.IsOpen)
        return Results.BadRequest(
            new ApiError("ASSIGNMENT_CLOSED",
                "Bai tap da dong"));

    // Kiem tra du lieu dau vao
    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(
            new ApiError("INVALID_SUBMISSION",
                "Thieu studentCode hoac repositoryUrl"));

    // Tao bai nop
    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: request.StudentCode,
        RepositoryUrl: request.RepositoryUrl,
        Status: "received",
        ReceivedAt: DateTimeOffset.UtcNow,
        UpdatedAt: DateTimeOffset.UtcNow
    );

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Chay cham bai nen, khong await
    _ = SimulateGradingAsync(
        submission.Id,
        submissions,
        submissionsLock,
        hubContext
    );

    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission);
});

// GET chi tiet bai nop
app.MapGet("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(
            i => i.Id == submissionId);

        return submission is null
            ? Results.NotFound(
                new ApiError("SUBMISSION_NOT_FOUND",
                    "Khong tim thay bai nop"))
            : Results.Ok(submission);
    }
});

// 3. Map SignalR Hub
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();


// 4. Tac vu cham bai gia lap
static async Task SimulateGradingAsync(
    Guid id,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));

    await UpdateStatusAsync(
        id, "processing", list, gate, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));

    await UpdateStatusAsync(
        id, "graded", list, gate, hub);
}


// 5. Cap nhat trang thai va gui thong bao SignalR
static async Task UpdateStatusAsync(
    Guid id,
    string status,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;

    // ① Cap nhat du lieu
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

    // ② Tao thong diep
    var msg = new SubmissionStatusMessage(
        updated.Id,
        updated.Status,
        updated.UpdatedAt
    );

    // ③ Gui thong bao den dung nhom
    await hub.Clients.Group(
        SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);

    Console.WriteLine(
        $"[Push] {SubmissionGroups.ForSubmission(id)} -> {status}");
}


// 6. SignalR Hub
sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        Console.WriteLine(
            $"[Hub] {Context.ConnectionId} theo doi submission:{submissionId}");

        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId)
        );
    }
}


// 7. Quan ly ten nhom
static class SubmissionGroups
{
    public static string ForSubmission(Guid id)
        => ForSubmission(id.ToString());

    public static string ForSubmission(string id)
        => $"submission:{id}";
}


// 8. DTO va Request
record AssignmentDto(
    int Id,
    string Title,
    bool IsOpen
);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl
);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt
);

record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt
);

record ApiError(
    string Code,
    string Message
);
