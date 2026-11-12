var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu lưu trong RAM (Bộ nhớ tiến trình)
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke cac endpoint chuan RESTful cho he thong nop bai", true),
    new(2, "Bai tap Client", "Viet ung dung console goi REST API bang HttpClient", true),
    new(3, "Bai tap Dong (Closed)", "Bai tap nay da het han nop", false)
};

var submissions = new List<SubmissionDto>();

// Endpoint kiểm tra dịch vụ sống
app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

// 1. GET /api/assignments -> Trả về danh sách bài tập (200 OK)
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 2. GET /api/assignments/{assignmentId:int} -> Chi tiết một bài tập (200 hoặc 404)
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// 3. POST /api/assignments/{assignmentId:int}/submissions -> Nộp bài với 3 cổng kiểm tra
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (int assignmentId, CreateSubmissionRequest request) =>
{
    // Cổng 1: Bài tập có tồn tại không?
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap can nop."));
    }

    // Cổng 2: Bài tập còn mở hạn nộp không?
    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong the nop tiep."));
    }

    // Cổng 3: Dữ liệu gửi lên có hợp lệ không?
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Ma sinh vien va link kho ma nguon khong duoc de trong."));
    }

    // Cổng mở rộng (Bước 9): Kiểm tra sinh viên đã nộp bài này chưa
    var duplicated = submissions.Any(s =>
        s.AssignmentId == assignmentId &&
        s.StudentCode.Equals(request.StudentCode.Trim(), StringComparison.OrdinalIgnoreCase));
    if (duplicated)
    {
        return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay truoc do."));
    }

    // Máy chủ tự sinh các trường nghiệp vụ: Id (Guid), Status, ReceivedAt
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow
    );

    submissions.Add(submission);

    // Trả về 201 Created kèm header Location dẫn tới endpoint đọc lại
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 4. GET /api/submissions/{submissionId:guid} -> Đọc lại lượt nộp theo Guid
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(s => s.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

// 5. DELETE /api/submissions/{submissionId:guid} (Bước 9 mở rộng) -> Hủy lượt nộp
app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var s = submissions.FirstOrDefault(item => item.Id == submissionId);
    if (s is null)
    {
        return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop de xoa."));
    }
    submissions.Remove(s);
    return Results.NoContent(); // 204 No Content
});

app.Run();

// --- KHAI BÁO CÁC HỢP ĐỒNG DTO & LỖI Ở CUỐI FILE ---
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);