var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint cho he thong nop bai", true),
    new(2, "Bai tap Client", "Viet console client goi API", true),
    new(3, "Bai tap da dong", "Bai nay da het han nop", false)
};

app.MapGet("/", () => Results.Ok(new {
    service = "Catalog API",
    status = "running",
}));
app.MapGet("/api/assignments", () => Results.Ok(assignments));
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});
app.Run();
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record ApiError(string Code, string Message);