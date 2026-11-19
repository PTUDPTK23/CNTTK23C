var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu trong bộ nhớ (RAM)
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint chuan REST", true),
    new(2, "Bai tap Client", "Viet console client voi HttpClient", true),
    new(3, "Bai tap da dong", "Bai tap nay da het han nop", false)
};
var submissions = new List<SubmissionDto>();

// Bước 2: Kiểm tra máy chủ sống
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// Bước 3: GET /api/assignments - Danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Bước 4: GET /api/assignments/{assignmentId:int} - Chi tiết một bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// Bước 5: POST /api/assignments/{assignmentId:int}/submissions - Nộp bài (3 cổng kiểm tra)
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
{
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong the nop."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: request.StudentCode.Trim(),
        RepositoryUrl: request.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: DateTimeOffset.UtcNow);

    submissions.Add(submission);
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// Bước 6: GET /api/submissions/{submissionId:guid} - Đọc lại lượt nộp theo Location
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.Run();

// Khai báo các DTO ở cuối tệp (bắt buộc sau app.Run)
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);