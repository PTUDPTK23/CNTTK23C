# Bài 3 - Giao tiếp thời gian thực với SignalR

**Sinh viên:** Nguyễn Vũ Đức Anh
**MSSV:** dtc245200383
**SDK:** .NET 10

## Nội dung

- `PingSignalR`: Hub, kết nối, broadcast và group với một API và nhiều console client.
- `RealtimeSubmissionSystem`: REST tạo/đọc lượt nộp; SignalR đẩy cập nhật trạng thái vào group riêng của từng lượt.

## 1. PingSignalR

Mở hai terminal trong thư mục `Bai3\PingSignalR`.

Terminal 1 - API:

```powershell
dotnet run --project Ping.Api/Ping.Api.csproj --launch-profile http
```

Server chạy tại `http://localhost:5200`, Hub tại `/hubs/ping`.

Terminal 2 và 3 - hai client:

```powershell
dotnet run --project Ping.Client/Ping.Client.csproj
```

Menu client:

- `1`: Echo - chỉ client gửi nhận được `ReceiveEcho`.
- `2`: Broadcast - tất cả client đang kết nối nhận `ReceiveBroadcast`.
- `3`: tham gia group.
- `4`: gửi tin nhắn tới group.
- `0`: thoát.

### Thí nghiệm và kết quả cần ghi nhận

| Thí nghiệm | Cách thao tác | Kết quả mong đợi |
|---|---|---|
| Caller | Mở hai client, ở một client chọn `1` | Chỉ client gọi nhận Echo |
| All | Mở hai client, chọn `2` ở một client | Cả hai client nhận Broadcast |
| Group | Cho cả hai client vào `lop1`, gửi tin vào `lop1` | Thành viên `lop1` nhận tin |
| Group riêng | Một client vào `lop1`, client khác vào `lop2`; gửi đến `lop1` | Chỉ thành viên `lop1` nhận tin |
| Tên sự kiện | Tạm đổi tên `On` ở client cho khác tên `SendAsync`, chạy thử rồi khôi phục | Handler không chạy nếu tên sự kiện không khớp |

SignalR group gắn với connection hiện tại; sau khi kết nối lại, client cần gọi `JoinGroup` lại. `WithAutomaticReconnect` không tự thử kết nối lần đầu nếu server chưa chạy.

Kiểm tra negotiate:

```powershell
curl.exe -i -X POST "http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1"
```

Đã kiểm thử với hai client: Echo chỉ về caller; Broadcast đến cả hai; tin nhắn vào một group không đến client chỉ tham gia group khác.

## 2. RealtimeSubmissionSystem

Mở terminal tại `Bai3\RealtimeSubmissionSystem`.

Terminal 1 - API:

```powershell
dotnet run --project EduSubmit.Api/EduSubmit.Api.csproj --launch-profile http
```

API chạy tại `http://localhost:5000`; SignalR Hub ở `/hubs/submissions`.

Terminal 2 - client:

```powershell
dotnet run --project EduSubmit.Client/EduSubmit.Client.csproj
```

Luồng demo:

1. Client đăng ký lắng nghe `SubmissionStatusChanged`, rồi kết nối SignalR.
2. Client dùng REST `POST /api/submissions` để tạo lượt nộp ở trạng thái `received`.
3. Client gọi `WatchSubmission(id)` để tham gia group `submission:{id}`.
4. Client gọi REST `GET` để đọc trạng thái hiện tại.
5. Tác vụ nền lưu `processing`, rồi `graded`; sau mỗi lần lưu mới phát sự kiện đến group tương ứng.
6. Client đọc REST lần cuối để xác nhận trạng thái, phòng trường hợp bỏ lỡ sự kiện.

Dữ liệu hiện được giữ trong bộ nhớ; khởi động lại API sẽ xóa các lượt nộp. Đây là demo cho bài học, chưa dùng database.

### Bộ kiểm thử HTTP

Mở `RealtimeSubmissionSystem\EduSubmit.Api\requests.http` bằng REST Client trong VS Code. Chạy lần lượt 9 request; request 4 dùng ID trả về từ request 3.

| Ca | Kết quả mong đợi |
|---|---|
| Danh sách | `200 OK` |
| OpenAPI | `200 OK` |
| Tạo submission hợp lệ | `201 Created`, status `received` |
| Đọc submission vừa tạo | `200 OK` |
| StudentName rỗng | `400 Bad Request` |
| AssignmentName rỗng | `400 Bad Request` |
| ID không tồn tại | `404 Not Found` |
| StudentName null | `400 Bad Request` |
| SignalR negotiate | `200 OK`, có connection id và transports |

Đã chạy end-to-end bằng console client: REST tạo submission, client tham gia group, nhận `processing` và `graded`, rồi REST xác nhận trạng thái cuối là `graded`. Đã kiểm tra các phản hồi lỗi `400`, `404` và negotiate `200`.

## 3. Build và Git

Chạy build cho cả hai solution:

```powershell
dotnet build PingSignalR/PingSignalR.slnx
dotnet build RealtimeSubmissionSystem/RealtimeSubmissionSystem.slnx
```

Không build solution trong khi API cùng project đang chạy; dừng server bằng `Ctrl+C` trước để tránh khóa file `.exe`.

Kiểm tra trước khi commit:

```powershell
git status
git diff --check
```

`.gitignore` loại trừ `bin/`, `obj/`, `.vs/` và file user-specific.

## 4. Ảnh minh chứng cần bổ sung

Lưu ảnh chụp thật vào thư mục `anh/` (không đưa ảnh giả hoặc ảnh mẫu):

1. `buoc1-cau-truc.png` - solution và hai project.
2. `buoc2-negotiate.png` - negotiate trả connection id và transports.
3. `buoc3-ping-echo-broadcast.png` - hai client, Echo và Broadcast.
4. `buoc4-ping-group.png` - gửi group và phân biệt thành viên.
5. `buoc5-edusubmit-rest.png` - POST trả `201`, GET trả trạng thái.
6. `buoc6-edusubmit-realtime.png` - client nhận `processing`, `graded`.
7. `buoc7-build.png` - build hai solution thành công.

## 5. Đẩy lên GitHub

Kiểm tra remote:

```powershell
git remote -v
```

Nếu chưa có remote, thêm URL repository GitHub của bài:

```powershell
git remote add origin https://github.com/<tai-khoan>/<repository>.git
git push -u origin main
```

Tạo Pull Request với tiêu đề theo yêu cầu lớp, ví dụ:

```text
Bai3 - dtc245200383 - Nguyễn Vũ Đức Anh
```
