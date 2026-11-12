var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Du lieu mau trong bo nho
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint...", true),
    new(2, "Bai tap Client", "Viet console client...", true)
};

var submissions = new List<SubmissionDto>();

// GET / : kiem tra may chu dang chay
app.MapGet("/", () => Results.Ok(new {
    service = "Submission.Api",
    status = "running"
}));

// GET /api/assignments : danh sach bai tap
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// GET /api/assignments/{assignmentId:int} : chi tiet mot bai tap
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) => {
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// POST /api/assignments/{assignmentId:int}/submissions : nop bai
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
(int assignmentId, CreateSubmissionRequest request) => {
    // 1. Bai tap co ton tai?
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    // 2. Bai tap con mo?
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));

    // 3. Body hop le?
    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    // 4. Tao, luu, tra ve 201 + Location
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

// GET /api/submissions/{submissionId:guid} : doc lai luot nop
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.Run();

// DTO: hinh dang du lieu di qua mang (dat o cuoi tep)
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
