var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Danh sách bài tập
var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tong quan he thong va MEP"),
    new Assignment(2, "Bai tap 2", "Giao tiep RESTful API voi Minimal API")
};

// Lưu các lượt nộp bài trong bộ nhớ (Bước 12)
var submissions = new List<SubmissionResult>();

// GET / -> kiểm tra service
app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

// GET /api/assignments -> danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// POST /api/submissions -> nộp bài
app.MapPost("/api/submissions", (CreateSubmissionRequest request) =>
{
    if (request.AssignmentId <= 0 ||
        string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new { message = "Du lieu nop bai khong hop le hoac bi thieu." });
    }

    var result = new SubmissionResult(
        Guid.NewGuid(),
        request.AssignmentId,
        request.StudentCode,
        "received",
        DateTimeOffset.UtcNow);

    submissions.Add(result);

    return Results.Created($"/api/submissions/{result.Id}", result);
});

// GET /api/submissions/{id} -> đọc lại lượt nộp (Bước 12)
app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    var item = submissions.FirstOrDefault(s => s.Id == id);
    return item is not null
        ? Results.Ok(item)
        : Results.NotFound(new { message = "Khong tim thay luot nop bai." });
});

app.Run();

record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);
