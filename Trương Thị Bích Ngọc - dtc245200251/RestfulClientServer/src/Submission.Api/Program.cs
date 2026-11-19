var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu bài tập trong bộ nhớ (Bài 1, 2 mở, bài 3 đã đóng để test nghiệp vụ)
var assignments = new List<AssignmentDto> {
    new(1, "Bai tap REST API", "Thiet ke endpoint va hop dong du lieu...", true),
    new(2, "Bai tap Client", "Viet ung dung console client ket noi API...", true),
    new(3, "Bai tap Cu (Da dong)", "Bai tap da het han nop...", false)
};

// Danh sách lưu trữ các lượt nộp bài trong bộ nhớ tiến trình
var submissions = new List<SubmissionDto>();

// Trang chủ kiểm tra trạng thái máy chủ
app.MapGet("/", () => Results.Ok(new {
    service = "Submission.Api",
    status = "running"
}));

// 1. GET /api/assignments : Lấy danh sách toàn bộ bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 2. GET /api/assignments/{assignmentId:int} : Lấy chi tiết một bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) => {
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null 
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap.")) 
        : Results.Ok(assignment);
});

// 3. POST /api/assignments/{assignmentId:int}/submissions : Nộp bài cho bài tập
app.MapPost("/api/assignments/{assignmentId:int}/submissions", 
    (int assignmentId, CreateSubmissionRequest request) => {
    
    // Cổng 1: Kiểm tra bài tập có tồn tại không
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    // Cổng 2: Kiểm tra bài tập còn mở không
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan nop nua."));

    // Cổng 3: Kiểm tra hợp lệ dữ liệu gửi lên (StudentCode và RepositoryUrl không để trống)
    if (string.IsNullOrWhiteSpace(request.StudentCode) || 
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    // Kiểm tra xung đột: Trùng lặp sinh viên đã nộp bài tập này (Mở rộng Bước 9 -> 409 Conflict)
    var duplicated = submissions.Any(item =>
        item.AssignmentId == assignmentId &&
        item.StudentCode.Equals(request.StudentCode.Trim(), StringComparison.OrdinalIgnoreCase));
    if (duplicated)
        return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay."));

    // Server tự sinh Id (Guid duy nhất), Status ("received"), thời điểm nhận UtcNow
    var submission = new SubmissionDto(
        Guid.NewGuid(), 
        assignmentId,
        request.StudentCode.Trim(), 
        request.RepositoryUrl.Trim(),
        "received", 
        DateTimeOffset.UtcNow
    );

    submissions.Add(submission);
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 4. GET /api/submissions/{submissionId:guid} : Đọc lại chi tiết một lượt nộp theo Guid
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null 
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop.")) 
        : Results.Ok(submission);
});

// 5. DELETE /api/submissions/{submissionId:guid} : Xóa lượt nộp bài (Mở rộng Bước 9 -> 204 No Content)
app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    var s = submissions.FirstOrDefault(i => i.Id == submissionId);
    if (s is null)
        return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop de xoa."));
    submissions.Remove(s);
    return Results.NoContent();
});

app.Run();

// DTOs & Error Records
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id, 
    int AssignmentId, 
    string StudentCode, 
    string RepositoryUrl, 
    string Status, 
    DateTimeOffset ReceivedAt
);
record ApiError(string Code, string Message);
