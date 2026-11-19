var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu trong bộ nhớ
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint chuan REST", true),
    new(2, "Bai tap Client", "Viet console client goi API", true),
    new(3, "Bai tap da dong", "Bai tap nay da het han nop", false)
};

var submissions = new List<SubmissionDto>();

// Kiểm tra máy chủ hoạt động
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// Danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Chi tiết một bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// Nộp bài cho một bài tập (Tài nguyên con)
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (int assignmentId, CreateSubmissionRequest request) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong the nop."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Ma sinh vien va link repo khong duoc de trong."));

    // Kiểm tra nộp trùng lặp -> 409 Conflict
    var isDuplicate = submissions.Any(s =>
        s.AssignmentId == assignmentId &&
        s.StudentCode.Equals(request.StudentCode.Trim(), StringComparison.OrdinalIgnoreCase));
    if (isDuplicate)
        return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai cho bai tap nay roi."));

    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: request.StudentCode.Trim(),
        RepositoryUrl: request.RepositoryUrl.Trim(),
        SubmittedAt: DateTime.UtcNow
    );

    submissions.Add(submission);
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// Xem chi tiết một lượt nộp theo Guid
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(s => s.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

// Xóa lượt nộp -> 204 No Content
app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(s => s.Id == submissionId);
    if (submission is null)
        return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."));

    submissions.Remove(submission);
    return Results.NoContent();
});

app.Run();

// Khai báo các kiểu DTO ở cuối tệp
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, DateTime SubmittedAt);
record ApiError(string Code, string Message);