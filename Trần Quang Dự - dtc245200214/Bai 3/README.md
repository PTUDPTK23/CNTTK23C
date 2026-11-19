# Bài 3: Giao tiếp thời gian thực với SignalR

* **Họ và tên:** Trần Quang Dự
* **MSSV:** dtc245200214
* **Lớp:** CNTT K23C

---

## I. Trả lời 3 câu hỏi đầu giờ

1. **Máy chủ có thể chủ động báo cho máy khách mà không cần máy khách hỏi không?**
   * **Trả lời:** Có, nếu máy khách mở sẵn một kết nối hai chiều lâu dài (WebSocket thông qua thư viện SignalR). Máy chủ chủ động đẩy thông điệp qua `Clients.Group(...).SendAsync(...)` (sử dụng `IHubContext` khi gửi từ tác vụ nền ở bên ngoài Hub).

2. **Đã có REST rồi, vì sao còn cần thêm SignalR? Hai thứ chia việc thế nào?**
   * **Trả lời:** REST hoạt động theo mô hình yêu cầu–phản hồi (chỉ máy khách hỏi được máy chủ), dùng để tạo và đọc tài nguyên với mã trạng thái rõ ràng (`201`, `400`, `404`). SignalR bổ sung chiều máy chủ chủ động đẩy các thay đổi trạng thái phát sinh sau yêu cầu ban đầu. SignalR không thay thế REST mà phối hợp cùng REST.

3. **Nếu đứt mạng đúng lúc máy chủ báo "đã chấm xong" thì sao?**
   * **Trả lời:** Thông điệp đó sẽ bị mất vì SignalR không lưu trữ tin nhắn chờ gửi lại. Khi có mạng, `WithAutomaticReconnect()` tự nối lại với một `ConnectionId` mới; máy khách cần đăng ký lại nhóm theo dõi và gọi lại REST (`GET /api/submissions/{id}`) để lấy trạng thái mới nhất (**REST là nguồn sự thật**).

---

## II. Thực hành PingSignalR

### Bước 1: Khởi tạo Solution, Project và cài gói thư viện
![Build Solution](<anh/Screenshot 2026-11-19 145515.jpg>)
* **Thấy:** Lệnh `dotnet build` báo `Build succeeded` cho cả 2 project `Ping.Client` và `Ping.Api` sau khi cài đặt gói `Microsoft.AspNetCore.SignalR.Client` phiên bản `10.0.15`.
* **Giải thích:** Đã khởi tạo thành công cấu trúc solution `PingSignalR` và thiết lập thư viện SignalR Client cho ứng dụng console.

### Bước 2: Cấu hình Hub và kiểm tra bắt tay negotiate
![Negotiate](<anh/Screenshot 2026-11-19 144909.jpg>)
* **Thấy:** Gọi `POST /hubs/ping/negotiate?negotiateVersion=1` trả về `connectionId` (`qRcc_jFa_rIys9XxGwRdoQ`) cùng 3 phương thức truyền tải `WebSockets`, `ServerSentEvents`, `LongPolling`. Gọi `GET` trực tiếp vào `/hubs/ping` trả về `400 Bad Request - Connection ID required`.
* **Giải thích:** Máy chủ đã đăng ký dịch vụ SignalR (`AddSignalR`) và ánh xạ tuyến đường `/hubs/ping` (`MapHub`), yêu cầu phải qua bước bắt tay lấy `connectionId` trước khi kết nối.

### Bước 3 & 4: Phương thức Echo và kết nối từ máy khách
![Echo](<anh/Screenshot 2026-11-19 152342.png>)
* **Thấy:** Máy khách `An` kết nối thành công đến `PingApi` và nhận phản hồi `[Echo] Echo: Xin chao SignalR tu An`.
* **Giải thích:** Máy khách gọi `InvokeAsync("Echo", ...)` lên Hub, Hub dùng `Clients.Caller.SendAsync("EchoResponse", ...)` để gửi phản hồi riêng cho người vừa gọi.

### Bước 5: Phát rộng (Broadcast) và Nhóm (Group)
* **Màn hình của máy khách An:**
  ![Client An nhận Broadcast và Group](<anh/Screenshot 2026-11-19 153422.png>)
* **Màn hình của máy khách Bình:**
  ![Client Bình gửi Broadcast và Group](<anh/Screenshot 2026-11-19 153451.png>)
* **Thấy:** Cửa sổ của `An` nhận được thông báo tham gia nhóm, tin `[Group]` và tin `[Broadcast]` từ `Binh`, nhưng không thấy tin `[Echo]` của `Binh`.
* **Giải thích:** `Clients.All` phát thông điệp tới toàn bộ kết nối đang mở, `Clients.Group` chỉ gửi tới các thành viên trong nhóm `lop-hoc-01`, còn `Clients.Caller` chỉ gửi riêng cho người gọi.

### Bước 6: Vòng đời kết nối và tự kết nối lại
* **Trước khi ngắt kết nối máy chủ (`ConnectionId`: `uRtP4XSb`):**
  ![Trước khi ngắt kết nối](<anh/Screenshot 2026-11-19 154618.jpg>)
* **Sau khi tắt và khởi động lại máy chủ (`ConnectionId` mới: `dqb0DPdm`):**
  ![Tự động kết nối lại](<anh/Screenshot 2026-11-19 154820.png>)
* **Thấy:** Khi tắt máy chủ rồi bật lại, máy khách tự động in `[Lifecycle] Dang ket noi lai...`, sau đó in `[Lifecycle] Da ket noi lai: dqb0DPdm` cùng thông báo hệ thống với `ConnectionId` mới.
* **Giải thích:** `WithAutomaticReconnect()` giúp máy khách tự động khôi phục kết nối theo chu kỳ (0s, 2s, 10s, 30s) và máy chủ cấp phát `ConnectionId` mới sau khi kết nối lại.

### Bước 7: Thí nghiệm 3 lỗi điển hình
1. **Lỗi 1 - Máy chủ chưa chạy:**
   * **Hiện tượng & Giải thích:** Lỗi `HttpRequestException: Connection refused` ngay tại `StartAsync()` vì `WithAutomaticReconnect()` chỉ cứu được các kết nối đã mở thành công trước đó.
2. **Lỗi 2 - Sai đường dẫn Hub (`/hub/ping`):**
   * **Hiện tượng & Giải thích:** Lỗi `404 (Not Found)` ở bước bắt tay `negotiate` do đường dẫn phía máy khách không khớp với `MapHub` trên máy chủ.
3. **Lỗi 3 - Sai tên thông điệp nhận (`On("EchoRespone")`):**
   * **Hiện tượng & Giải thích:** Không báo lỗi biên dịch hay lỗi chạy nhưng chương trình im lặng, không in dòng `[Echo]` do tên sự kiện ở `SendAsync` và `On` không khớp nhau.

---

## III. Thực hành EduSubmit Realtime (RealtimeSubmissionSystem)

### 1. Kiểm tra API và quá trình chấm nền qua REST (Bước 2, 3 & 4)
* **Bước 2 — Tạo lượt nộp qua REST API (`201 Created`):**
  ![Tạo lượt nộp REST](<anh/Screenshot 2026-11-19 162453.jpg>)
* **Bước 3 — Kiểm tra bắt tay `negotiate` đúng và sai đường dẫn:**
  ![Negotiate EduSubmit](<anh/Screenshot 2026-11-19 163209.jpg>)
* **Bước 4 — Trạng thái tự chuyển từ `received` sang `graded` sau tác vụ chấm nền:**
  ![Trạng thái graded qua REST](<anh/Screenshot 2026-11-19 163541.jpg>)
  ### 1. Kết nối hai kênh (Bước 5)
![Kết nối SignalR EduSubmit](<anh/Screenshot 2026-11-19 164051.png>)

### 2. Kết quả chạy luồng đầy đủ 8 dòng (Bước 6)
![Kết quả EduSubmit 8 dòng](<anh/Screenshot 2026-11-19 164237.png>)

### 2. Kết quả chạy thực tế máy khách hoàn chỉnh (8 dòng)

```text
Da ket noi SignalR.
Chon bai tap: 1 Bai tap SignalR
Da tao luot nop: 5ad92bba-b7ba-46f8-a6f8-53986203c3d1
Trang thai REST ban dau: received
Dang cho thong bao trang thai tu may chu...
Thong bao SignalR: 5ad92bba-b7ba-46f8-a6f8-53986203c3d1 -> processing luc 09:42:26
Thong bao SignalR: 5ad92bba-b7ba-46f8-a6f8-53986203c3d1 -> graded luc 09:42:28
Trang thai doc lai bang REST: graded
```

### 3. Bảng tổng hợp 5 thí nghiệm phân tán (Bước 7)

| STT | Thí nghiệm | Dự đoán | Kết quả thật | Giải thích |
| :--- | :--- | :--- | :--- | :--- |
| 1 | Chạy 2 máy khách `SV001` và `SV002` cùng lúc | Mỗi người chỉ nhận thông báo của bài mình | Đúng như dự đoán, mỗi máy khách chỉ hiện lượt nộp vừa tạo | Nhóm `submission:{id}` trên máy chủ tách biệt người nhận theo từng lượt nộp |
| 2 | Chạy máy khách khi máy chủ chưa chạy | Báo lỗi không kết nối được | `HttpRequestException: Connection refused (localhost:5000)` | Lần kết nối đầu tiên ở `StartAsync()` thất bại ngay lập tức |
| 3 | Sửa `WithUrl` thành `/hub/submissions` | Báo lỗi không tìm thấy đường dẫn | `HttpRequestException: 404 (Not Found)` | Lỗi ngay tại bước bắt tay `negotiate` do sai tuyến đường `MapHub` |
| 4 | Sửa `On("SubmissionStatusChanged")` thành `On("SubmissionStatusChange")` | Không nhận được tin và hết giờ chờ | Không báo lỗi; sau 10 giây in `Qua thoi gian cho thong bao graded.` | Sai tên sự kiện chiều xuống khiến client bỏ qua thông điệp |
| 5 | Chờ 4 giây rồi mới gọi `WatchSubmission` | Lỡ thông báo đẩy nhưng đọc lại REST vẫn thấy kết quả | Không nhận tin SignalR nào; gọi `GET` cuối in `graded` | Vào nhóm muộn nên lỡ tin đẩy; REST là nguồn sự thật nên luôn lấy được trạng thái cuối |