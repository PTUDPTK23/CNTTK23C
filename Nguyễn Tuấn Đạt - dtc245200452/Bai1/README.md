# Bài 1 – Tổng quan hệ thống và các mô hình trao đổi thông điệp (MEP)

- **Sinh viên:** Nguyễn Tuấn Đạt – **MSSV:** `dtc245200452` – Lớp **CNTT K23C**
- **Môn học:** Phát triển ứng dụng phân tán – GV: TS. Dương Thu Mây
- **Phạm vi hoàn thành:** Đầy đủ Bước 1 – Bước 12 (Cài đặt môi trường, `MepDemo`, `BasicWebApi` mục 8.1–8.6, `MiniDistributedSystem` / `EduSubmit` lát cắt 1, 4 thí nghiệm lỗi Bước 10 và bài mở rộng `GET /api/submissions/{id}` Bước 12).

## Cấu trúc thư mục

```text
Bai1/
├── .gitignore                        # Bỏ qua bin/, obj/, .vs/
├── README.md                         # Tài liệu tổng quan và hướng dẫn chạy
├── MepDemo/                          # Bước 7: Minh họa 5 mô hình MEP trong 1 tiến trình
│   ├── MepDemo.csproj
│   ├── Program.cs
│   └── README.md
├── BasicWebApi/                      # Mục 8 (TH 1.2): ASP.NET Core Minimal API
│   ├── BasicWebApi.slnx
│   ├── README.md
│   ├── requests.http                 # Kiểm thử 200, 201, 400 và language=ja
│   └── src/BasicWebApi/
├── MiniDistributedSystem/            # Bước 9–12: EduSubmit lát cắt 1 (2 tiến trình + mở rộng)
│   ├── MiniDistributedSystem.slnx
│   ├── README.md
│   ├── requests.http                 # Kiểm thử 200, 201, 400, 404
│   └── src/
│       ├── Submission.Api/           # Máy chủ API (cổng 5000, có GET /api/submissions/{id})
│       └── Submission.Client/        # Máy khách console (đọc lại lượt nộp Bước 12)
└── docs/
    ├── bai1.md                       # Nhật ký thực hành, câu hỏi, bảng 4 thí nghiệm lỗi
    └── images/                       # Ảnh chụp / kết xuất đầu ra thực nghiệm
```

## Hướng dẫn build và chạy

```powershell
# 1. MepDemo (5 mô hình trao đổi thông điệp)
dotnet run --project MepDemo

# 2. BasicWebApi (cổng 5081)
dotnet build BasicWebApi/BasicWebApi.slnx
dotnet run --project BasicWebApi/src/BasicWebApi --urls http://localhost:5081

# 3. MiniDistributedSystem (2 terminal độc lập)
dotnet build MiniDistributedSystem/MiniDistributedSystem.slnx
# Terminal 1 - Máy chủ:
dotnet run --project MiniDistributedSystem/src/Submission.Api --urls http://localhost:5000
# Terminal 2 - Máy khách:
dotnet run --project MiniDistributedSystem/src/Submission.Client -- http://localhost:5000
```

## Nhật ký và minh chứng

- Báo cáo thực hành chi tiết: [docs/bai1.md](docs/bai1.md)
