var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu lưu trong bộ nhớ tiến trình (RAM)
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint chuan RESTful cho he thong nop bai.", true),
    new(2, "Bai tap Client", "Viet ung dung console goi REST API bang HttpClient.", true),
    new(3, "Bai tap da dong", "Bai tap nay da het han nop (dung de kiem thu ca 5h).", false)
};


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

app.Run();

// Khai báo các kiểu DTO và ApiError ở cuối tệp
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record ApiError(string Code, string Message);
