var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ── Dữ liệu mẫu trong bộ nhớ ──────────────────────────────────────────────
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint va trien khai EduSubmit", true),
    new(2, "Bai tap Client",   "Viet console client goi EduSubmit API",      true),
    new(3, "Bai tap Docker",   "Dong goi ung dung bang Docker",               false),
};

var submissions = new List<SubmissionDto>();

// ── GET / : kiểm tra máy chủ đang chạy ───────────────────────────────────
app.MapGet("/", () => Results.Ok(new {
    service = "Submission.Api",
    status  = "running"
}));

// ── GET /api/assignments : danh sách bài tập ─────────────────────────────
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// ── GET /api/assignments/{assignmentId:int} : chi tiết bài tập ───────────
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// ── POST /api/assignments/{assignmentId:int}/submissions : nộp bài ────────
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
{
    // Cổng 1: bài tập có tồn tại?
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    // Cổng 2: bài tập còn mở nhận bài?
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError(
            "ASSIGNMENT_CLOSED", "Bai tap da dong."));

    // Cổng 3: dữ liệu đầu vào hợp lệ?
    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    // Server tự sinh Id, Status, ReceivedAt — client không gửi
    var submission = new SubmissionDto(
        Id            : Guid.NewGuid(),
        AssignmentId  : assignmentId,
        StudentCode   : request.StudentCode.Trim(),
        RepositoryUrl : request.RepositoryUrl.Trim(),
        Status        : "received",
        ReceivedAt    : DateTimeOffset.UtcNow);

    submissions.Add(submission);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// ── GET /api/submissions/{submissionId:guid} : đọc lại bài nộp ───────────
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.Run();

// ── DTOs & Records ─────────────────────────────────────────────────────────
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);

// Đầu ra — máy chủ trả về (Id, AssignmentId, Status, ReceivedAt do server tự sinh)
record SubmissionDto(
    Guid            Id,
    int             AssignmentId,
    string          StudentCode,
    string          RepositoryUrl,
    string          Status,
    DateTimeOffset  ReceivedAt);

// Đầu vào — máy khách gửi lên (KHÔNG có Id, Status, ReceivedAt)
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);

record ApiError(string Code, string Message);
