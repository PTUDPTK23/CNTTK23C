var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Kiem tra API dang chay.
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// Du lieu bai tap mau.
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint", true),
    new(2, "Bai tap Client", "Viet console client", true),
    new(3, "Bai tap da dong", "Dung de kiem tra loi", false)
};

// Luu cac luot nop trong bo nho.
var submissions = new List<SubmissionDto>();

// Lay danh sach bai tap.
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Lay chi tiet bai tap.
app.MapGet("/api/assignments/{assignmentId:int}",
    (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(
        item => item.Id == assignmentId);

    if (assignment is null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."));
    }

    return Results.Ok(assignment);
});

// Nop bai.
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
{
    var assignment = assignments.FirstOrDefault(
        item => item.Id == assignmentId);

    if (assignment is null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."));
    }

    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError(
            "ASSIGNMENT_CLOSED",
            "Bai tap da dong."));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION",
            "Thieu ma sinh vien hoac kho ma nguon."));
    }

    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow);

    submissions.Add(submission);

    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission);
});

// Doc lai luot nop theo ID.
app.MapGet("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(
        item => item.Id == submissionId);

    if (submission is null)
    {
        return Results.NotFound(new ApiError(
            "SUBMISSION_NOT_FOUND",
            "Khong tim thay luot nop."));
    }

    return Results.Ok(submission);
});

app.Run();

// Cac DTO.
record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);

record ApiError(string Code, string Message);