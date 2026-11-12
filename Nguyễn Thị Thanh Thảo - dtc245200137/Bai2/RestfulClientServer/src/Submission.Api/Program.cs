var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu trong bộ nhớ (2 bài mở, 1 bài đã đóng)
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint va ma trang thai", true),
    new(2, "Bai tap Client", "Viet console client goi API", true),
    new(3, "Bai tap da dong", "Bai tap nay da het han nop", false)
};

app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

// Bước 3: GET /api/assignments - Danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.Run();

// Khai báo kiểu DTO & ApiError ở cuối tệp (sau app.Run())
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record ApiError(string Code, string Message);