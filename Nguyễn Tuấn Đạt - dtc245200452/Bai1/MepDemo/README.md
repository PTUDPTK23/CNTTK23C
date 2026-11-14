# MepDemo – Minh họa 5 mô hình trao đổi thông điệp (MEP)

Chương trình console mô phỏng 5 mô hình trao đổi thông điệp cơ bản trong một tiến trình (không cần package ngoài):

1. **Request–Response** (`GradingService` + timeout)
2. **One-Way / Fire & Forget** (`Channel.CreateUnbounded<string>`)
3. **Queue-Based (Competing Consumers)** (`Channel.CreateBounded<GradeJob>`, 2 worker chia nhau xử lý `sub-1`..`sub-4`)
4. **Publish–Subscribe** (`InMemoryEventBus` phát `SubmissionGraded` tới 3 service)
5. **Streaming / Push** (`IAsyncEnumerable<string>` đẩy cập nhật trạng thái liên tục)

## Chạy thử

```powershell
# Chạy toàn bộ 5 mô hình
dotnet run --project MepDemo

# Chạy riêng một mô hình (1..5)
dotnet run --project MepDemo -- 3
```
