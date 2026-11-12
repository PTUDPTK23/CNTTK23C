# Bài 2: RESTful API cho tương tác Client-Server (EduSubmit)

## 1. Các lệnh đã chạy
- Tạo solution và project:
  ```powershell
  dotnet new sln -n RestfulClientServer
  dotnet new web -n Submission.Api -o src/Submission.Api
  dotnet new console -n Submission.Client -o src/Submission.Client
  dotnet sln add src/Submission.Api/Submission.Api.csproj
  dotnet sln add src/Submission.Client/Submission.Client.csproj
  dotnet new gitignore