using System.Collections.Concurrent;

// EduSubmit - Lat cat 1 (Bai 1, Buoc 9-12): Submission.Api (vai tro MAY CHU)
// Chay: dotnet run --project src/Submission.Api --urls http://localhost:5000

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Du lieu mau trong bo nho tien trinh (chua dung co so du lieu)
var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tao API dau tien va giao tiep hai tien trinh"),
    new Assignment(2, "Bai tap 2", "Goi API tu ung dung Client qua HTTP")
};

// Buoc 12 (Mo rong): Luu danh sach luot nop trong bo nho de truy van lai theo ID
var submissions = new ConcurrentDictionary<Guid, SubmissionResult>();

// GET / : Kiem tra dich vu dang hoat dong
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// GET /api/assignments : Lay danh sach bai tap mau
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// POST /api/submissions : Nhan yeu cau nop bai, kiem tra dau vao va luu tru
app.MapPost("/api/submissions", (CreateSubmissionRequest request) =>
{
    if (request.AssignmentId <= 0 ||
        string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new { message = "Du lieu nop bai khong hop le." });
    }

    var submission = new SubmissionResult(
        Guid.NewGuid(),
        request.AssignmentId,
        request.StudentCode.Trim(),
        "received",
        DateTimeOffset.UtcNow);

    submissions[submission.Id] = submission;

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// Buoc 12 (Mo rong): Doc lai luot nop theo ID (200 OK hoac 404 Not Found)
app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    return submissions.TryGetValue(id, out var item)
        ? Results.Ok(item)
        : Results.NotFound(new { message = $"Khong tim thay luot nop: {id}" });
});

app.Run();

// Hop dong du lieu trao doi qua HTTP/JSON
record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string? StudentCode, string? RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);
