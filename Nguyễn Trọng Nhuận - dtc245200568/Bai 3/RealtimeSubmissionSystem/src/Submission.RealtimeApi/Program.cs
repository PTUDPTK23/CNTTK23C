using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

var assignments = new List<AssignmentDto> {
    new(1, "Bai tap REST API", "Thiet ke endpoint...", true),
    new(2, "Bai tap Client", "Viet console client...", true)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new {
    service = "Submission.RealtimeApi",
    status = "running"
}));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) => {
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) => {
        var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
        if (assignment is null)
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
        if (!assignment.IsOpen)
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
        if (string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
            return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(Guid.NewGuid(), assignmentId,
            request.StudentCode.Trim(), request.RepositoryUrl.Trim(),
            "received", now, now);

        lock (submissionsLock)
        {
            submissions.Add(submission);
        }

        // khởi động chấm bài ở nền, KHÔNG chờ
        _ = SimulateGradingAsync(submission.Id, submissions,
                                 submissionsLock, hubContext);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub) {
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));

    string finalStatus = "graded";
    lock (gate) {
        var sub = list.FirstOrDefault(s => s.Id == id);
        if (sub is not null && !sub.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
            finalStatus = "failed";
        }
    }
    await UpdateStatusAsync(id, finalStatus, list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub) {
    SubmissionDto? updated = null;
    lock (gate) {                                // 1: cập nhật dữ liệu
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0) {
            updated = list[i] with {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            list[i] = updated;
        }
    }
    if (updated is null) return;
    Console.WriteLine($"[Push] submission:{id} -> {status}");
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status,
                                          updated.UpdatedAt); // 2: dữ liệu tối thiểu
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg); // 3: đẩy đúng nhóm
}

sealed class SubmissionHub : Hub {
    public async Task WatchSubmission(string submissionId) {
        var connShort = Context.ConnectionId.Length > 8 ? Context.ConnectionId[..8] : Context.ConnectionId;
        Console.WriteLine($"[Hub] {connShort} theo doi submission:{submissionId}");
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId));
    }
}

static class SubmissionGroups {
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
