var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Lap trinh RESTful API", "Xay dung API bang ASP.NET Core Minimal API.", true),
    new(2, "Ung dung HttpClient", "Viet client console goi REST API.", true),
    new(3, "Bao cao cuoi ky", "Bai tap da het han nop.", false)
};

var submissions = new List<SubmissionDto>();

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);

    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions", (int assignmentId, CreateSubmissionRequest request) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    }

    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong the nop."));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Ma sinh vien va RepositoryUrl khong duoc de trong."));
    }

    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow);

    submissions.Add(submission);
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);

    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop bai."))
        : Results.Ok(submission);
});

app.Run("http://localhost:5000");

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);