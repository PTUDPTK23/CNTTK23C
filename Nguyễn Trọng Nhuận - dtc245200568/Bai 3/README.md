# BÀI TẬP 3: GIAO TIẾP THỜI GIAN THỰC VỚI SIGNALR

- **Họ và tên:** Nguyễn Trọng Nhuận
- **Mã sinh viên:** `dtc245200568`
- **Lớp:** CNTT K23C
- **Môn học:** Phát triển ứng dụng phân tán
- **Giảng viên:** TS. Dương Thu Mây

---

## 1. TRẢ LỜI 3 CÂU HỎI ĐẦU GIỜ (Slide 65)

### Câu 1: Máy chủ có thể chủ động báo cho máy khách mà không cần máy khách hỏi không?
> **Trả lời:**  
> **Có.** Khi máy khách thiết lập một kết nối liên tục, hai chiều (persistent connection qua WebSocket của thư viện SignalR), máy chủ có thể chủ động đẩy (Push) dữ liệu tới máy khách bất kỳ lúc nào thông qua `Clients.Group(...).SendAsync`. Nếu cần phát thông điệp từ bên ngoài Hub (như trong background task `SimulateGradingAsync` hoặc endpoint Minimal API), ASP.NET Core cung cấp `IHubContext<THub>` được tiêm qua Dependency Injection (DI) để làm việc này.

### Câu 2: Đã có REST rồi, vì sao còn cần thêm SignalR? Hai thứ chia việc thế nào?
> **Trả lời:**  
> - **REST:** Phù hợp cho mô hình **Yêu cầu – Phản hồi (Request-Response)**, xử lý các thao tác tạo/sửa/xóa tài nguyên có mã trạng thái HTTP rõ ràng (200, 201, 400, 404). REST đóng vai trò là **Nguồn sự thật duy nhất (Single Source of Truth - SSOT)**.
> - **SignalR:** Bổ sung kênh thời gian thực theo mô hình **Đẩy dữ liệu (Push / Pub-Sub)**, giúp máy chủ báo ngay các thay đổi phát sinh sau yêu cầu ban đầu (ví dụ: trạng thái chấm bài từ `received` → `processing` → `graded`) mà máy khách không cần phải gửi request thăm dò liên tục (Polling) gây lãng phí băng thông và tài nguyên CPU.
> - **Nguyên tắc phối hợp:** *"Push để nhanh, REST để chắc"*. SignalR không thay thế REST mà hỗ trợ thông báo tức thì.

### Câu 3: Nếu đứt mạng đúng lúc máy chủ báo "đã chấm xong" (graded) thì sao?
> **Trả lời:**  
> Thông điệp SignalR lúc mất kết nối sẽ bị mất vì SignalR không lưu hàng đợi tin nhắn chờ gửi lại. Tuy nhiên, nhờ cấu hình `WithAutomaticReconnect()`, khi có mạng máy khách sẽ tự động kết nối lại (nhận `ConnectionId` mới). Lúc này, theo nguyên lý **REST là nguồn sự thật**, máy khách chỉ cần gọi lại endpoint REST `GET /api/submissions/{id}` để đồng bộ và lấy về trạng thái chính xác, mới nhất (`graded`).

---

## 2. KẾT QUẢ CHẠY MÁY KHÁCH (8 DÒNG CHUẨN) (Slide 57 & 71)

Lệnh thực thi:
```bash
dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV001
```

Kết quả in ra đúng chuẩn 8 dòng:
```text
Da ket noi SignalR.
Chon bai tap: 1 - Bai tap SignalR
Da tao luot nop: 85104c09-d131-47fd-8ac1-29e25ae120eb
Trang thai REST ban dau: received
Dang cho thong bao trang thai tu may chu...
Thong bao SignalR: 85104c09-... -> processing luc 08:00:45
Thong bao SignalR: 85104c09-... -> graded luc 08:00:47
Trang thai doc lai bang REST: graded
```

*(Ghi chú: Hai dòng thông báo SignalR cách nhau khoảng 2 giây, đúng theo tiến trình tác vụ chấm nền).*

---

## 3. BẢNG 5 THÍ NGHIỆM PHÂN TÁN (DỰ ĐOÁN, KẾT QUẢ THẬT VÀ BÀI HỌC) (Slide 58, 59)

| STT | Thí nghiệm | Dự đoán trước khi chạy | Kết quả chạy thực tế | Bài học phân tán rút ra |
|:---:|:---|:---|:---|:---|
| **1** | **Chạy 2 máy khách SV001 và SV002 cùng lúc** | Mỗi máy khách chỉ nhận thông báo về lượt nộp của chính mình, không thấy lượt nộp của máy khác. | Mỗi máy khách chỉ thấy lượt nộp vừa tạo (SV001 nhận thông báo mã nộp của SV001; SV002 nhận thông báo mã nộp của SV002). | **Nhóm `submission:{id}` tách người nhận**, đảm bảo tính riêng tư dữ liệu và tránh bão thông điệp (broadcast storm). |
| **2** | **Chạy máy khách khi máy chủ chưa chạy** | Máy khách không thể kết nối tới Hub và ném lỗi từ chối kết nối ngay lập tức. | `HttpRequestException: Connection refused (localhost:5000)` (SocketException 10061). | **Lần nối đầu không được tự thử lại**. `WithAutomaticReconnect` chỉ kích hoạt để khôi phục các kết nối *đã* được mở thành công trước đó. |
| **3** | **Sửa WithUrl thành `/hub/submissions` (thiếu s)** | Gặp lỗi HTTP 404 ở bước bắt tay (negotiate) vì sai đường dẫn tuyến Hub. | `HttpRequestException: Response status code does not indicate success: 404 (Not Found)`. | **Lỗi ở bước negotiate**. Đường dẫn URL ở máy khách phải khớp tuyệt đối với `app.MapHub` trên máy chủ. |
| **4** | **Sửa On("SubmissionStatusChanged") thành On("SubmissionStatusChange") (thiếu d)** | Máy khách không khớp tên sự kiện nên không bắt được thông báo, bị quá thời gian (timeout) 10 giây. | Không có lỗi runtime nào xảy ra; sau 10 giây in: `Qua thoi gian cho thong bao graded.`. REST đọc lại vẫn thấy `graded`. | **Sai tên chiều xuống = im lặng**. SignalR âm thầm bỏ qua sự kiện không có listener. Bắt buộc phải có fallback đọc lại bằng REST. |
| **5** | **Chờ 4 giây rồi mới gọi `WatchSubmission`** | Máy khách gia nhập nhóm muộn nên bỏ lỡ các thông báo đẩy, nhưng REST đọc lại vẫn lấy được trạng thái cuối cùng. | Không nhận được thông điệp nào qua SignalR; sau đó REST đọc lại vẫn ra: `graded`. | **Vào nhóm muộn là lỡ tin**. Thông điệp thời gian thực có thể bị lỡ; **REST luôn là nguồn sự thật**. |

---

## 4. DANH SÁCH 9 CA KIỂM THỬ TRONG `requests.http` (Slide 61)

Tệp `RealtimeSubmissionSystem/requests.http` đã cấu hình đủ 9 ca với mã trạng thái HTTP mong đợi:

1. `GET /` $\rightarrow$ `200 OK` (Kiểm tra service running)
2. `GET /api/assignments` $\rightarrow$ `200 OK` (Lấy danh sách bài tập)
3. `POST /api/assignments/1/submissions` $\rightarrow$ `201 Created` kèm tiêu đề `Location` và `status = "received"`
4. `GET /api/submissions/{id}` $\rightarrow$ `200 OK` (Đọc lại trạng thái: đổi dần `received` $\rightarrow$ `processing` $\rightarrow$ `graded`)
5. `POST /api/assignments/1/submissions` thiếu dữ liệu $\rightarrow$ `400 Bad Request`
6. `POST /api/assignments/99/submissions` $\rightarrow$ `404 Not Found` (Bài tập không tồn tại)
7. `GET /api/submissions/00000000-0000-0000-0000-000000000000` $\rightarrow$ `404 Not Found` (Lượt nộp không tồn tại)
8. `POST /hubs/submissions/negotiate?negotiateVersion=1` $\rightarrow$ `200 OK` kèm `connectionId` và các phương thức truyền tải
9. `POST /hub/submissions/negotiate?negotiateVersion=1` $\rightarrow$ `404 Not Found` (Sai đường dẫn Hub)

---

## 5. PHẦN VẬN DỤNG: BÀI TẬP 2 VÀ BÀI TẬP 3 (Slide 62)

1. **Bài tập 2 (Xử lý trạng thái `failed`):**
   - Trên máy chủ: Nếu `RepositoryUrl` không bắt đầu bằng `https://`, tác vụ nền cập nhật trạng thái cuối thành `failed`.
   - Trên máy khách: Khi nhận sự kiện với trạng thái `failed`, in ra thông báo cảnh báo màu đỏ:
     `!!! CANH BAO: luot nop {id} cham LOI (failed)`.
2. **Bài tập 3 (Theo dõi 2 lượt nộp cùng lúc trên 1 kết nối):**
   - Sử dụng `ConcurrentDictionary<Guid, TaskCompletionSource<string>>` để định tuyến sự kiện cho từng bài nộp độc lập.
   - Chạy lệnh: `dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 --vandung`
   - Kết quả thực tế:
     ```text
     Da ket noi SignalR.
     Da tao SV001: de1e068b (received)
     Da tao SV002: ac030d0f (received)
     Thong bao SignalR: de1e068b -> processing
     Thong bao SignalR: ac030d0f -> processing
     Thong bao SignalR: de1e068b -> graded
     !!! CANH BAO: luot nop ac030d0f cham LOI (failed)
     SV001: SignalR = graded, REST = graded
     SV002: SignalR = failed, REST = failed
     ```

---
