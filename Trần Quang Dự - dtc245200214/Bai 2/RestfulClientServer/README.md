# Bài 2: RESTful API cho tương tác Client-Server (EduSubmit)

- **Sinh viên thực hiện:** Trần Quang Dự
- **Mã sinh viên:** DTC245200214
- **Lớp:** CNTT K23C - ICTU

---

## 1. Nhật ký lệnh đã chạy và kết quả

### Các lệnh khởi tạo & chạy hệ thống
```powershell
# Tạo solution và 2 project
dotnet new sln -n RestfulClientServer
dotnet new web -n Submission.Api -o src/Submission.Api
dotnet new console -n Submission.Client -o src/Submission.Client

# Chạy máy chủ API ở cổng 5000 (Terminal 1)
dotnet run --project src/Submission.Api --urls http://localhost:5000

# Chạy máy khách Console 4 nấc (Terminal 2)
dotnet run --project src/Submission.Client -- http://localhost:5000