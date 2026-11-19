var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var assignments = new List<AssignmentDto> {
    new(1, "Bai tap REST API", "Thiet ke endpoint...", true),
    new(2, "Bai tap Client", "Viet console client...", false) // Bài 2 tạm đóng (IsOpen = false) để test nghiệp vụ đóng/mở
};

// Danh sách lưu trữ các lượt nộp bài trong bộ nhớ
var submissions = new List<SubmissionDto>();

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) => {
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null 
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap.")) 
        : Results.Ok(assignment);
});

// Endpoint POST nộp bài với các lớp kiểm tra nghiệp vụ
app.MapPost("/api/assignments/{assignmentId:int}/submissions", 
    (int assignmentId, CreateSubmissionRequest request) => {
    
    // 1. Kiểm tra xem bài tập có tồn tại không
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    // 2. Kiểm tra xem bài tập còn mở hay không
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan nop nua."));

    // 3. Kiểm tra tính hợp lệ của dữ liệu đầu vào (mã sinh viên và link repo)
    if (string.IsNullOrWhiteSpace(request.StudentCode) || 
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac link repository."));

    // 4. Kiểm tra nộp trùng bài (409 Conflict)
    var duplicated = submissions.Any(item => 
        item.AssignmentId == assignmentId && 
        item.StudentCode.Equals(request.StudentCode.Trim(), StringComparison.OrdinalIgnoreCase));
    if (duplicated)
        return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay."));

    // Server tự sinh các thông tin bảo mật và định danh
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

// Endpoint GET đọc lại lượt nộp theo ID
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

// Endpoint DELETE xóa lượt nộp (Trả về 204 No Content)
app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    var s = submissions.FirstOrDefault(i => i.Id == submissionId);
    if (s is null)
        return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."));
    
    submissions.Remove(s);
    return Results.NoContent();
});

app.Run();

// Khai báo các DTO và Error Record
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