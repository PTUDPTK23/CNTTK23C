// EduSubmit - Bai 1: Submission.Api (vai tro MAY CHU)
// Chay: dotnet run --project src/Submission.Api --urls http://localhost:5000

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Du lieu mau trong bo nho (da them Bai tap 3 theo yeu cau)
var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tao API dau tien"),
    new Assignment(2, "Bai tap 2", "Goi API tu client"),
    new Assignment(3, "Bai tap 3", "Mo rong tra cuu luot nop theo Id")
};

var submissions = new List<SubmissionResult>();

// GET / : kiem tra dich vu dang chay
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// GET /api/assignments : lay danh sach bai tap
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// POST /api/submissions : gui mot luot nop bai
app.MapPost("/api/submissions", (CreateSubmissionRequest request) =>
{
    // 1. Kiem tra du lieu dau vao truoc khi xu ly
    if (request.AssignmentId <= 0 ||
        string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new { message = "Du lieu nop bai khong hop le" });
    }

    // 2. Ghi nhan luot nop voi trang thai ban dau "received" va luu vao danh sach
    var submission = new SubmissionResult(
        Guid.NewGuid(),
        request.AssignmentId,
        request.StudentCode.Trim(),
        "received",
        DateTimeOffset.UtcNow);

    submissions.Add(submission);

    // 3. Tra 201 Created kem header Location
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// Bai mo rong (Buoc 12): GET /api/submissions/{id:guid} : tra cuu lai luot nop
app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    var submission = submissions.FirstOrDefault(s => s.Id == id);
    return submission is null
        ? Results.NotFound(new { message = "Khong tim thay luot nop" })
        : Results.Ok(submission);
});

app.Run();

// Hop dong du lieu trao doi qua ranh gioi API
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
