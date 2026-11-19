using System.Net;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ---------------------------------------------------------
// VÙNG 2: DỮ LIỆU MẪU TRONG BỘ NHỚ RAM (Slide 74)
// ---------------------------------------------------------
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint...", true),
    new(2, "Bai tap Client", "Viet console client...", true),
    new(3, "Bai tap Da dong", "Bai tap het han nop...", false) // Dùng để test bài đã đóng
};

var submissions = new List<SubmissionDto>();

// ---------------------------------------------------------
// VÙNG 3: BẢNG ĐỊNH TUYẾN CÁC ENDPOINT (Slide 73 - 83)
// ---------------------------------------------------------

// Endpoint 0: Kiểm tra máy chủ đang chạy (Slide 73)
app.MapGet("/", () => Results.Ok(new {
    service = "Submission.Api",
    status = "running"
}));

// Endpoint 1: GET /api/assignments - Lấy danh sách bài tập (Slide 74)
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Endpoint 2: GET /api/assignments/{assignmentId:int} - Chi tiết bài tập (Slide 75)
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) => {
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// Endpoint 3: POST /api/assignments/{assignmentId:int}/submissions - Nộp bài (Slide 78 - 79 & mở rộng 102)
app.MapPost("/api/assignments/{assignmentId:int}/submissions", 
(int assignmentId, CreateSubmissionRequest request) => {
    // Cổng 1: Bài tập có tồn tại không?
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    // Cổng 2: Bài tập còn mở không?
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong the nop."));

    // Cổng 3: Dữ liệu hợp lệ?
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac link repository."));

    // (Mở rộng Bước 9 - Slide 102): Tránh nộp trùng
    var duplicated = submissions.Any(item =>
        item.AssignmentId == assignmentId &&
        item.StudentCode.Equals(request.StudentCode.Trim(), StringComparison.OrdinalIgnoreCase));
    if (duplicated)
        return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay."));

    // Tạo mới lượt nộp (Server tự sinh Id, Status, ReceivedAt)
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

// Endpoint 4: GET /api/submissions/{submissionId:guid} - Đọc lại lượt nộp theo Location (Slide 83)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

// ---------------------------------------------------------
// VÙNG 4: CHẠY ỨNG DỤNG
// ---------------------------------------------------------
app.Run();

// ---------------------------------------------------------
// VÙNG 5: KHAI BÁO DTO VÀ APIERROR (Slide 74, 77)
// ---------------------------------------------------------
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);