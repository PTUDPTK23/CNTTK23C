var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// === DỮ LIỆU MẪU TRONG BỘ NHỚ ===
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint va DTO", true),
    new(2, "Bai tap Client", "Viet console client bang HttpClient", true),
    new(3, "Bai tap Da dong", "Bai tap nay da het han nop", false)
};

var submissions = new List<SubmissionDto>();

// === CÁC ENDPOINT ===
app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

// Bước 3: GET /api/assignments - Lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Bước 4: GET /api/assignments/{assignmentId} - Lấy chi tiết 1 bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// Bước 5: POST /api/assignments/{assignmentId}/submissions - Nộp bài (3 cổng kiểm tra)
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
{
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan bai nop."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

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

// Bước 6: GET /api/submissions/{submissionId} - Đọc lại lượt nộp theo Location
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.Run();

// === KHAI BÁO CÁC DTO (Nằm sau app.Run()) ===
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);