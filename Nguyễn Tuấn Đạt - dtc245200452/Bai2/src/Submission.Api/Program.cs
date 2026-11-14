var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu lưu trong bộ nhớ tiến trình (RAM)
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint chuan RESTful cho he thong nop bai.", true),
    new(2, "Bai tap Client", "Viet ung dung console goi REST API bang HttpClient.", true),
    new(3, "Bai tap da dong", "Bai tap nay da het han nop (dung de kiem thu ca 5h).", false)
};

var submissions = new List<SubmissionDto>();

// Bước 2: Endpoint kiểm tra trạng thái hoạt động của máy chủ
app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

// Bước 3: GET /api/assignments -> Danh sách bài tập (200 OK)
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Bước 4: GET /api/assignments/{assignmentId:int} -> Chi tiết một bài tập (200 hoặc 404 kèm ApiError)
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// Bước 5: POST /api/assignments/{assignmentId:int}/submissions -> Nộp bài qua 3 cổng kiểm tra
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
{
    // Cổng 1: Kiểm tra bài tập có tồn tại không
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    }

    // Cổng 2: Kiểm tra bài tập còn mở không
    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan luot nop moi."));
    }

    // Cổng 3: Kiểm tra dữ liệu đầu vào hợp lệ
    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));
    }

    // Máy chủ tự sinh Id (Guid), gán trạng thái ban đầu "received" và thời điểm nhận
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow);

    submissions.Add(submission);

    // Trả về 201 Created kèm header Location dẫn tới tài nguyên lượt nộp vừa tạo
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// Bước 6: GET /api/submissions/{submissionId:guid} -> Đọc lại lượt nộp theo Location (200 hoặc 404)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

// Bước 9 (Mở rộng): DELETE /api/submissions/{submissionId:guid} -> Rút lượt nộp (204 hoặc 404)
app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    if (submission is null)
    {
        return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."));
    }

    submissions.Remove(submission);
    return Results.NoContent();
});

app.Run();

// Khai báo các kiểu DTO và ApiError ở cuối tệp
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
