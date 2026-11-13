var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint va DTO theo chuan RESTful...", true),
    new(2, "Bai tap Client", "Viet console client goi API qua HttpClient...", true),
    new(3, "Bai tap cu (da dong)", "Bai tap nay da het han nop...", false)
};

var submissions = new List<SubmissionDto>();

app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    return assignment is not null
        ? Results.Ok(assignment)
        : Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions", (int assignmentId, CreateSubmissionRequest request) =>
{
    // Cổng 1: Bài tập có tồn tại không?
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    }

    // Cổng 2: Bài tập còn mở không?
    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
    }

    // Cổng 3: Dữ liệu mã SV và URL kho mã nguồn có hợp lệ không?
    if (string.IsNullOrWhiteSpace(request?.StudentCode) || string.IsNullOrWhiteSpace(request?.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Ma sinh vien va duong dan kho khong duoc de trong."));
    }

    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow
    );
    submissions.Add(submission);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(s => s.Id == submissionId);
    return submission is not null
        ? Results.Ok(submission)
        : Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."));
});

app.Run();

public record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
public record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
public record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset SubmittedAt
);
public record ApiError(string Code, string Message);
