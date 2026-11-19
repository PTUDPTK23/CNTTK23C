using Microsoft.AspNetCore.SignalR.Client;

const string hubUrl = "http://localhost:5201/hubs/submissions";

var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

connection.On<SubmissionDto>("SubmissionUpdated", sub =>
{
    Console.WriteLine($"[SignalR] Bai {sub.Id} ({sub.StudentId} - {sub.Assignment}) -> status: {sub.Status} | diem: {sub.Score?.ToString() ?? "dang cham"}");
});

await connection.StartAsync();
Console.WriteLine($"Da ket noi toi {hubUrl}. Dang cho thong bao cham bai... (Nhan Enter de thoat)");
Console.ReadLine();
await connection.StopAsync();

record SubmissionDto(string Id, string StudentId, string Assignment, string Status, double? Score);