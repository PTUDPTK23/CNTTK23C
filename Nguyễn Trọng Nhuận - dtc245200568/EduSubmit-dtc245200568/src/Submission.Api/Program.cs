// EduSubmit - Submission.Api (Máy chủ tiếp nhận bài nộp)
// Chạy: dotnet run --project src/Submission.Api --urls http://localhost:5000

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Danh sách bài tập mẫu trong bộ nhớ
var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tao API dau tien"),
    new Assignment(2, "Bai tap 2", "Goi API tu client")
};

// Danh sách lưu trữ các lượt nộp bài trong bộ nhớ (Bước 12)
var submissions = new List<SubmissionResult>();

// GET / : Kiểm tra dịch vụ đang chạy
app.MapGet("/", () => Results.Ok(new {
    service = "Submission.Api",
    status = "running"
}));

// GET /api/assignments : Lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// POST /api/submissions : Nộp bài mới
app.MapPost("/api/submissions", (CreateSubmissionRequest request) => {
    // 1. Kiểm tra tính hợp lệ của dữ liệu đầu vào
    if (request.AssignmentId <= 0 ||
        string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new { message = "Du lieu nop bai khong hop le" });
    }

    // 2. Tạo bản ghi lượt nộp bài với trạng thái ban đầu "received"
    var submission = new SubmissionResult(
        Guid.NewGuid(),
        request.AssignmentId,
        request.StudentCode.Trim(),
        "received",
        DateTimeOffset.UtcNow);

    // Lưu vào danh sách (Bước 12)
    lock (submissions)
    {
        submissions.Add(submission);
    }

    // 3. Trả về 201 Created kèm header Location và thông tin lượt nộp
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// GET /api/submissions/{id:guid} : Lấy thông tin lượt nộp theo ID (Bước 12)
app.MapGet("/api/submissions/{id:guid}", (Guid id) => {
    SubmissionResult? submission;
    lock (submissions)
    {
        submission = submissions.FirstOrDefault(s => s.Id == id);
    }

    return submission is not null
        ? Results.Ok(submission)
        : Results.NotFound(new { message = $"Khong tim thay luot nop bai voi ma: {id}" });
});

app.Run();

// Các hợp đồng dữ liệu (Data Contract)
record Assignment(int Id, string Title, string Description);

record CreateSubmissionRequest(
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl);

record SubmissionResult(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string Status,
    DateTimeOffset ReceivedAt);
