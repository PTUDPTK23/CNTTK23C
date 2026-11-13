# BasicWebApi – Luyện kỹ thuật Minimal API (Bài 1, Mục 8)

Ứng dụng ASP.NET Core Minimal API (.NET 10) minh họa:
- Cấu trúc solution `.slnx` và project `src/BasicWebApi`
- Mô hình 2 pha trong `Program.cs` (`WebApplication.CreateBuilder` + DI Singleton → `Build()` → `MapGet`/`MapPost` → `Run()`)
- Đọc dữ liệu từ route (`/hello/{name}`), query string (`?language=vi|en|ja`), JSON body (`POST /greetings`) và Dependency Injection (`GreetingService`)

## Lệnh build và chạy

```powershell
dotnet build BasicWebApi.slnx
dotnet run --project src/BasicWebApi --urls http://localhost:5081
```

## Kiểm thử nhanh bằng `curl.exe`

```powershell
curl.exe -i http://localhost:5081/
curl.exe -i "http://localhost:5081/hello/An?language=vi"
curl.exe -i "http://localhost:5081/hello/An?language=en"
curl.exe -i "http://localhost:5081/hello/An?language=ja"
curl.exe -i -X POST http://localhost:5081/greetings -H "Content-Type: application/json" -d '{\"name\":\"Binh\"}'
curl.exe -i -X POST http://localhost:5081/greetings -H "Content-Type: application/json" -d '{\"name\":\"\"}'
```
