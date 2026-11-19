var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Khởi tạo dữ liệu mẫu trong bộ nhớ (Bài 1 đang mở, Bài 2 đã đóng để test lỗi 5h)
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint...", true),
    new(2, "Bai tap Client", "Viet console client...", false)
};

var submissions = new List<SubmissionDto>();

// Bước 2: Endpoint kiểm tra trạng thái máy chủ
app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

// Bước 3: Lấy danh sách bài tập (200 OK)
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Bước 4: Xem chi tiết một bài tập (200 OK hoặc 404 kèm ApiError)
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// Bước 5: Nộp bài tập (kiểm tra 3 cổng: tồn tại -> còn mở -> hợp lệ -> 201 Created + Location)
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
    {
        var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
        if (assignment is null)
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

        if (!assignment.IsOpen)
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong han nop."));

        if (string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
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

// Bước 6: Đọc lại lượt nộp theo Guid Id (200 OK hoặc 404 kèm ApiError)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.Run();

// Khai báo các DTO và cấu trúc lỗi chuẩn
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);