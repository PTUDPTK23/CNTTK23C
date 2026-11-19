# BÀI 3: GIAO TIẾP THỜI GIAN THỰC VỚI SIGNALR

- **Họ và tên:** Phan Ngô Huy Hoàng
- **Mã sinh viên:** DTC245200477
- **Lớp:** CNTTK23C - Trường Đại học Công nghệ Thông tin và Truyền thông (ICTU)
- **Môn học:** Phát triển ứng dụng phân tán
- **Giảng viên hướng dẫn:** TS. Dương Thu Mây

---

## 1. Cấu trúc thư mục

```text
Phan Ngô Huy Hoàng - dtc245200477/
└── Bai3/
    ├── README.md                          # Nhật ký, câu trả lời và báo cáo thí nghiệm
    ├── PingSignalR/                       # Phần 1: Demo cơ chế PingSignalR
    │   ├── PingSignalR.slnx
    │   ├── .gitignore
    │   └── src/
    │       ├── Ping.Api/                  # Máy chủ SignalR Hub (cổng 5200)
    │       └── Ping.Client/               # Máy khách Console SignalR
    └── RealtimeSubmissionSystem/          # Phần 2: Hệ thống nộp bài EduSubmit Realtime
        ├── RealtimeSubmissionSystem.slnx
        ├── requests.http                  # 9 ca kiểm thử REST & Bắt tay SignalR
        ├── .gitignore
        └── src/
            ├── Submission.RealtimeApi/    # Máy chủ REST + SignalR Hub (cổng 5000)
            └── Submission.RealtimeClient/ # Máy khách phối hợp REST + SignalR
```

---

## 2. Trả lời 3 câu hỏi đầu giờ (Slide 65)

| STT | Câu hỏi | Câu trả lời chi tiết |
|:---:|:---|:---|
| **1** | **Máy chủ có thể chủ động báo cho máy khách mà không cần máy khách hỏi không?** | **Có.** Khi máy khách mở sẵn một kết nối lâu dài hai chiều (thông qua WebSocket/SignalR), máy chủ có thể chủ động đẩy (push) thông điệp về máy khách bất kỳ lúc nào bằng lệnh `Clients.Group(...).SendAsync()`, `Clients.Caller.SendAsync()`, `Clients.All.SendAsync()`. Khi cần gửi từ bên ngoài Hub (chẳng hạn trong Minimal API endpoint hoặc tác vụ nền), ASP.NET Core cung cấp `IHubContext<THub>`. |
| **2** | **Đã có REST rồi, vì sao còn cần thêm SignalR? Hai thứ chia việc thế nào?** | REST tuân theo mô hình Yêu cầu - Phản hồi (Request - Response), máy chủ không thể tự gửi dữ liệu nếu máy khách không hỏi trước. Nếu dùng Polling lặp lại để kiểm tra trạng thái, phần lớn request là thừa thãi và gây trễ.<br>**Phân chia trách nhiệm:**<br>- **REST (Nguồn sự thật):** Dùng để tạo tài nguyên (`POST` trả về `201 Created`), truy vấn trạng thái hiện tại (`GET`).<br>- **SignalR (Kênh thông báo nhanh):** Dùng để đẩy các sự kiện thay đổi phát sinh sau khi tạo tài nguyên (ví dụ: chuyển từ `received` $\rightarrow$ `processing` $\rightarrow$ `graded`). SignalR **không thay thế** REST mà bổ sung chiều máy chủ báo tin. |
| **3** | **Nếu đứt mạng đúng lúc máy chủ báo "đã chấm xong" (graded) thì sao?** | Thông điệp SignalR đó sẽ bị mất vì SignalR không tự lưu trữ tin nhắn chờ. Khi kết nối mạng phục hồi, cơ chế `WithAutomaticReconnect` sẽ tự động kết nối lại (với một `ConnectionId` mới). Khi đó, quy tắc vàng là: **Máy khách luôn gọi lại REST `GET /api/submissions/{id}` để đọc trạng thái chính thức mới nhất** ("Push để nhanh, REST để chắc"). |

---

## 3. Khung 4 câu hỏi thiết kế cho EduSubmit Realtime (Slide 46)

1. **Sự kiện nào kích hoạt thông điệp?**
   - Trạng thái lượt nộp bài thay đổi: `received` $\rightarrow$ `processing` $\rightarrow$ `graded` (hoặc `failed`).
2. **Gửi cho ai?**
   - Chỉ gửi cho máy khách đang theo dõi đúng lượt nộp đó thông qua nhóm `submission:{id}` (`Clients.Group("submission:{id}")`), đảm bảo tính riêng tư và tiết kiệm băng thông.
3. **Gửi kèm dữ liệu gì (tối thiểu)?**
   - DTO tối thiểu gồm 3 trường: `record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt)`.
4. **Mất kết nối thì lấy lại trạng thái thế nào?**
   - Máy khách gọi lại API REST: `GET /api/submissions/{id}` để đồng bộ trạng thái mới nhất từ cơ sở dữ liệu/bộ nhớ.

---

## 4. Kết quả chạy máy khách EduSubmit (8 dòng chuẩn)

Khi khởi chạy máy chủ `Submission.RealtimeApi` (cổng 5000) và chạy máy khách `Submission.RealtimeClient`, kết quả in ra đúng chuẩn 8 dòng:

```text
Da ket noi SignalR.
Chon bai tap: 1 - Bai tap SignalR
Da tao luot nop: 2e1c071e-9271-4a57-b2e1-454378f8cb61
Trang thai REST ban dau: received
Dang cho thong bao trang thai tu may chu...
Thong bao SignalR: 2e1c071e-9271-4a57-b2e1-454378f8cb61 -> processing luc 14:40:01
Thong bao SignalR: 2e1c071e-9271-4a57-b2e1-454378f8cb61 -> graded luc 14:40:03
Trang thai doc lai bang REST: graded
```

*(Máy khách tự động thoát sau khi nhận xong trạng thái `graded`).*

---

## 5. Bảng 5 thí nghiệm phân tán (Slide 58 - 59)

| STT | Tình huống thí nghiệm | Dự đoán trước khi chạy | Kết quả chạy thực tế | Bài học rút ra |
|:---:|:---|:---|:---|:---|
| **1** | Chạy 2 máy khách SV001 và SV002 cùng lúc | Mỗi sinh viên chỉ nhận thông báo về lượt nộp của chính mình, không thấy bài của người kia. | Đúng như dự đoán: máy khách SV001 chỉ in trạng thái của bài SV001, SV002 chỉ in trạng thái của bài SV002. | Dùng SignalR Group `submission:{id}` giúp cô lập phạm vi nhận tin, bảo mật dữ liệu riêng tư và tiết kiệm lưu lượng mạng. |
| **2** | Chạy máy khách khi máy chủ chưa bật | Máy khách báo lỗi không kết nối được và dừng chương trình. | `HttpRequestException: Connection refused (localhost:5000)`. | Lần kết nối đầu tiên (`StartAsync`) không tự thử lại. `WithAutomaticReconnect` chỉ kích hoạt để cứu các kết nối đã từng mở thành công. |
| **3** | Sửa `WithUrl` thành `/hub/submissions` (thiếu `s`) | Lỗi 404 Not Found tại bước bắt tay negotiate. | `HttpRequestException: Response status code does not indicate success: 404 (Not Found)`. | Bắt tay negotiate gửi qua HTTP, đường dẫn client gọi bắt buộc phải khớp tuyệt đối với `MapHub` ở server. |
| **4** | Sửa `On("SubmissionStatusChanged")` thành sai tên | Máy khách không nhận được thông báo, chờ 10s rồi hết giờ. | Không xuất hiện lỗi ngoại lệ, sau 10 giây in: `"Qua thoi gian cho thong bao graded."`. | Chiều máy chủ đẩy xuống (Push) đối soát bằng chuỗi tên sự kiện. Nếu lệch một ký tự thì máy khách âm thầm bỏ qua, không báo lỗi. |
| **5** | Chờ 4 giây rồi mới gọi `WatchSubmission` | Quá thời gian chấm (3s), máy khách không nhận được sự kiện realtime qua SignalR, nhưng đọc lại REST vẫn ra `graded`. | Không nhận được thông báo SignalR nào, nhưng dòng REST cuối cùng vẫn in: `Trang thai doc lai bang REST: graded`. | Sự kiện realtime không được lưu lại; vào nhóm muộn sẽ lỡ tin. Tuy nhiên REST luôn là nguồn sự thật cuối cùng. |

---

## 6. Kiểm thử 9 ca trong `requests.http` (Slide 61)

1. `GET /` $\rightarrow$ `200 OK` (Kiểm tra service running).
2. `GET /api/assignments` $\rightarrow$ `200 OK` (Danh sách bài tập).
3. `POST /api/assignments/1/submissions` $\rightarrow$ `201 Created` + Header `Location` + Body `status: "received"`.
4. `GET /api/submissions/{id}` $\rightarrow$ `200 OK` (Quan sát trạng thái đổi dần sang `processing` rồi `graded`).
5. `POST /api/assignments/1/submissions` (thiếu trường) $\rightarrow$ `400 Bad Request` (`INVALID_SUBMISSION`).
6. `POST /api/assignments/999/submissions` $\rightarrow$ `404 Not Found` (`ASSIGNMENT_NOT_FOUND`).
7. `GET /api/submissions/{guid rỗng}` $\rightarrow$ `404 Not Found` (`SUBMISSION_NOT_FOUND`).
8. `POST /hubs/submissions/negotiate?negotiateVersion=1` $\rightarrow$ `200 OK` kèm `connectionId` và danh sách transports.
9. `POST /hub/submissions/negotiate?negotiateVersion=1` $\rightarrow$ `404 Not Found` (sai route).

---

## 7. Câu hỏi ôn tập và mở rộng (Slide 68)

- **Polling vs Push:** Polling tốn rất nhiều request lặp vô ích (hơn 90% dữ liệu không đổi), gây lãng phí CPU và băng thông mạng, đồng thời tạo độ trễ. Push thiết lập 1 kết nối duy nhất, máy chủ chỉ gửi dữ liệu khi có thay đổi thực sự $\rightarrow$ độ trễ gần như bằng 0, tiết kiệm tài nguyên.
- **WebSocket vs SignalR:** WebSocket là giao thức mạng tầng ứng dụng (RFC 6455) duy trì kênh 2 chiều toàn công song. SignalR là thư viện cấp cao đóng gói bên trên WebSocket, bổ sung RPC, Hub, Groups, cơ chế tự động kết nối lại và tự động hạ cấp xuống Server-Sent Events / Long Polling nếu môi trường mạng chặn WebSocket.
- **IHubContext:** Đối tượng Hub chỉ sống trong phạm vi một lời gọi từ client. Muốn gửi tin nhắn thời gian thực từ API endpoint, Background Service, Worker ngoài Hub thì bắt buộc phải tiêm `IHubContext<THub>` qua Dependency Injection.
- **Vấn đề Scale-out (Nhiều bản sao API) & Cách khắc phục:**
  - *Vấn đề:* Mặc định, danh sách kết nối và Group được lưu trữ trong RAM của từng máy chủ. Khi chạy nhiều instance API (load balancing), client A kết nối tới Server 1, nhưng request nộp bài hoặc background job xử lý ở Server 2 thì Server 2 không thể gửi trực tiếp tới client A.
  - *Giải pháp:* Sử dụng cơ chế SignalR Backplane (phổ biến nhất là **Redis Backplane** hoặc **Azure SignalR Service**). Khi một máy chủ phát tin, tin nhắn được đẩy vào Redis Pub/Sub và lan truyền đến tất cả các instance máy chủ khác để gửi tới đúng client.
