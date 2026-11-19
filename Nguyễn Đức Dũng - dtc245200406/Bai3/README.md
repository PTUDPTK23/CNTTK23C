# Báo Cáo Thực Hành Bài 3: Giao Tiếp Thời Gian Thực Với SignalR
**Môn học:** Phát triển ứng dụng phân tán  
**Giảng viên:** TS. Dương Thu Mây  
**Sinh viên thực hiện:** Nguyễn Đức Dũng  
**Mã sinh viên:** DTC245200406  
**Lớp:** CNTT K23C  

---

## MỤC LỤC
1. [Trả lời 3 câu hỏi đầu giờ](#1-trả-lời-3-câu-hỏi-đầu-giờ)
2. [Phần 1: Thực hành PingSignalR (Bước 1 - 7)](#2-phần-1-thực-hành-pingsignalr-bước-1---7)
   - [Kiến trúc & Hợp đồng Hub](#kiến-trúc--hợp-đồng-hub)
   - [Kết quả thực thi máy khách](#kết-quả-thực-thi-máy-khách)
   - [Thí nghiệm vòng đời kết nối & tự động kết nối lại](#thí-nghiệm-vòng-đời-kết-nối--tự-động-kết-nối-lại)
   - [3 thí nghiệm lỗi điển hình](#3-thí-nghiệm-lỗi-điển-hình)
   - [Bảng tổng hợp 4 phạm vi gửi](#bảng-tổng-hợp-4-phạm-vi-gửi)
3. [Phần 2: Ứng dụng EduSubmit Realtime (Bước 1 - 8)](#3-phần-2-ứng-dụng-edusubmit-realtime-bước-1---8)
   - [Khung 4 câu hỏi thiết kế thời gian thực](#khung-4-câu-hỏi-thiết-kế-thời-gian-thực)
   - [So sánh PingSignalR và EduSubmit Realtime](#so-sánh-pingsignalr-và-edusubmit-realtime)
   - [Kết quả chạy thực tế EduSubmit (8 dòng)](#kết-quả-chạy-thực-tế-edusubmit-8-dòng)
   - [Bảng 5 thí nghiệm phân tán](#bảng-5-thí-nghiệm-phân-tán)
   - [Kiểm thử 9 ca trong requests.http](#kiểm-thử-9-ca-trong-requestshttp)
4. [Phần 3: Câu hỏi ôn tập và thảo luận](#4-phần-3-câu-hỏi-ôn-tập-và-thảo-luận)
5. [Phần 4: Bài tập mở rộng (Bài tập 2 & 3 - LoiGiai)](#5-phần-4-bài-tập-mở-rộng-bài-tập-2--3---loigiai)

---

## 1. Trả lời 3 câu hỏi đầu giờ

### Câu 1: Máy chủ có thể chủ động báo cho máy khách mà không cần máy khách hỏi không?
**Trả lời:**  
Có thể. Máy chủ hoàn toàn có thể chủ động đẩy dữ liệu (push) về máy khách nếu máy khách chủ động mở và duy trì sẵn một kết nối hai chiều lâu dài (persistent connection) như **WebSocket** thông qua thư viện **SignalR**.  
Khi có sự kiện phát sinh, máy chủ gửi thông điệp thông qua phương thức `Clients.Group(...).SendAsync` hoặc `Clients.All.SendAsync`. Nếu phát sinh từ bên ngoài Hub (ví dụ: Controller, Minimal API endpoint, Background Service), máy chủ sẽ sử dụng interface `IHubContext<THub>` được hệ thống Dependency Injection cung cấp để gửi thông điệp.

### Câu 2: Đã có REST rồi, vì sao còn cần thêm SignalR? Hai thứ chia việc thế nào?
**Trả lời:**  
- **Vì sao cần thêm SignalR:** REST hoạt động theo mô hình *yêu cầu - phản hồi (Request - Response)* trên nền HTTP, trong đó máy khách luôn phải là bên chủ động mở lời. Nếu chỉ dùng REST để cập nhật trạng thái chấm bài, máy khách phải liên tục gửi request thăm dò (polling), gây lãng phí băng thông, tải máy chủ cao và tạo độ trễ. SignalR giải quyết vấn đề này bằng cách thiết lập kênh truyền thông 2 chiều giúp máy chủ báo ngay lập tức khi trạng thái thay đổi.
- **Cách phân chia nhiệm vụ:**  
  - **REST:** Đảm nhiệm tạo tài nguyên mới (`POST /api/assignments/{id}/submissions` trả về mã `201 Created` kèm `Location`), đọc danh sách hoặc truy vấn trạng thái hiện tại (`GET /api/submissions/{id}`). REST đóng vai trò là **"nguồn sự thật duy nhất" (Single Source of Truth)**.
  - **SignalR:** Đảm nhiệm thông báo các sự kiện bất đồng bộ phát sinh sau đó (`received` $\rightarrow$ `processing` $\rightarrow$ `graded`), đẩy ngay tức thì về đúng nhóm máy khách liên quan mà không cần máy khách phải hỏi. SignalR không thay thế REST mà bổ sung chiều báo tin.

### Câu 3: Nếu đứt mạng đúng lúc máy chủ báo "đã chấm xong" thì sao?
**Trả lời:**  
- Khi mất kết nối mạng tại thời điểm máy chủ gửi thông báo `graded`, thông điệp thời gian thực đó sẽ **bị mất** vì SignalR không tự lưu đệm (buffer) tin nhắn chưa gửi.
- Cơ chế `WithAutomaticReconnect()` ở máy khách sẽ tự động thử kết nối lại theo các khoảng thời gian (0s, 2s, 10s, 30s). Sau khi kết nối lại thành công, máy khách nhận được một `ConnectionId` mới và bị rời khỏi các Group trước đó. Máy khách cần tham gia lại Group (`WatchSubmission`).
- **Nguyên tắc an toàn:** Sau khi hết thời gian chờ hoặc sau khi kết nối lại, máy khách luôn gọi lại API REST: `GET /api/submissions/{id}` để đọc trạng thái chính xác và mới nhất từ cơ sở dữ liệu máy chủ ("Push để nhanh, REST để chắc").

---

## 2. Phần 1: Thực hành PingSignalR (Bước 1 - 7)

### Kiến trúc & Hợp đồng Hub
- **Cổng dịch vụ:** `http://localhost:5200`
- **Đường dẫn Hub:** `/hubs/ping`
- **Hợp đồng phương thức & thông điệp:**
  | Máy khách gọi (`InvokeAsync`) | Máy chủ gửi về (`On`) | Phạm vi nhận | Ý nghĩa |
  | :--- | :--- | :--- | :--- |
  | `Echo(message)` | `EchoResponse` | `Clients.Caller` | Trả lời riêng cho người vừa gọi |
  | `Broadcast(message)` | `BroadcastMessage` | `Clients.All` | Phát rộng cho toàn bộ kết nối đang mở |
  | `JoinGroup(groupName)` | `GroupMessage` | `Clients.Group` | Gia nhập nhóm, thông báo cho cả nhóm |
  | `SendToGroup(group, msg)` | `GroupMessage` | `Clients.Group` | Gửi tin nhắn nội bộ trong nhóm |
  | *(Tự động khi nối/ngắt)* | `SystemMessage` | `Clients.All` | Báo sự kiện kết nối/ngắt kết nối |

### Kết quả thực thi máy khách
Khi khởi chạy Máy chủ (`Ping.Api`) và hai máy khách An và Bình:
- **Terminal máy khách An (`DTC245200406`):**
  ```text
  DTC245200406 da ket noi den PingApi. ConnectionId: h6U6E-W4
  [System] Client h6U6E-W4 da ket noi.
  [Echo] Echo: Xin chao SignalR tu DTC245200406
  [Group] h6U6E-W4 da tham gia nhom lop-hoc-01
  [Group] Thong bao nhom tu DTC245200406
  Dang cho thong diep... Nhan Enter de thoat.
  [Broadcast] Xin chao tat ca tu DTC245200406
  ```
- **Xác thực phạm vi gửi:** An và Bình nhìn thấy tin `[Group]` và `[Broadcast]` của nhau, nhưng không nhìn thấy tin `[Echo]` của đối phương vì `Echo` gửi riêng cho `Clients.Caller`.

### Thí nghiệm vòng đời kết nối & tự động kết nối lại
- Cấu hình máy khách sử dụng `.WithAutomaticReconnect()`.
- Khi máy chủ bị tắt tạm thời (Ctrl+C) và bật lại:
  1. Máy khách chuyển sang trạng thái: `[Lifecycle] Dang ket noi lai...`
  2. Khi máy chủ hoạt động lại, máy khách in: `[Lifecycle] Da ket noi lai: <ConnectionId_Moi>`
  3. Máy chủ ghi nhận sự kiện kết nối mới qua `OnConnectedAsync()`.
  4. Lịch thử lại mặc định: 0 giây $\rightarrow$ 2 giây $\rightarrow$ 10 giây $\rightarrow$ 30 giây (sau 4 lần nếu không thành công sẽ kích hoạt sự kiện `Closed`).

### 3 thí nghiệm lỗi điển hình
| Thí nghiệm lỗi | Thao tác thực hiện | Hiện tượng quan sát được | Bài học rút ra |
| :--- | :--- | :--- | :--- |
| **1. Máy chủ chưa chạy** | Chạy máy khách khi `Ping.Api` chưa bật | Báo lỗi `HttpRequestException: No connection could be made because the target machine actively refused it (localhost:5200)` | `StartAsync` ném ngoại lệ ngay lập tức khi không kết nối được ban đầu. `WithAutomaticReconnect` chỉ có tác dụng bảo vệ những kết nối ĐÃ từng mở thành công. |
| **2. Sai đường dẫn hub** | Sửa `WithUrl` thành `http://localhost:5200/hub/ping` (thiếu `s`) | Báo lỗi `HttpRequestException: Response status code does not indicate success: 404 (Not Found)` | Lỗi xuất hiện ngay tại bước bắt tay thương lượng (`negotiate`). URL máy khách gọi bắt buộc phải khớp chính xác với `MapHub<THub>("/...")` trên máy chủ. |
| **3. Sai tên thông điệp** | Đổi tên đăng ký: `connection.On("EchoRespone")` (thiếu chữ `s`) | Chương trình chạy bình thường, không báo bất kỳ lỗi nào, nhưng máy khách hoàn toàn không in dòng `[Echo]` | Sai tên thông điệp chiều xuống (`SendAsync` $\leftrightarrow$ `On`) sẽ gây "im lặng" (silent failure). Khi máy khách không nhận được dữ liệu, việc đầu tiên cần kiểm tra là đối chiếu từng ký tự tên thông điệp giữa hai phía. |

### Bảng tổng hợp 4 phạm vi gửi
| Phạm vi | Ai nhận | Ví dụ thực tế trong hệ thống |
| :--- | :--- | :--- |
| `Clients.All` | Mọi kết nối đang mở | Máy chủ thông báo bảo trì toàn hệ thống; công bố bài tập mới. |
| `Clients.Caller` | Duy nhất máy khách vừa gọi Hub | Phản hồi xác nhận gọi hàm thành công; trả về kết quả truy vấn riêng. |
| `Clients.Group(groupName)` | Tất cả máy khách trong nhóm | Đẩy tiến độ và kết quả chấm bài đến nhóm `submission:{id}`. |
| `Clients.Client(connectionId)` | Một kết nối cụ thể theo mã ID | Gửi cảnh báo bảo mật riêng biệt hoặc định tuyến phiên làm việc đặc thù. |

---

## 3. Phần 2: Ứng dụng EduSubmit Realtime (Bước 1 - 8)

### Khung 4 câu hỏi thiết kế thời gian thực
1. **Sự kiện nào kích hoạt thông điệp?**  
   $\rightarrow$ Trạng thái lượt nộp bài thay đổi trong tiến trình chấm nền: từ `received` $\rightarrow$ `processing` $\rightarrow$ `graded` (hoặc `failed`).
2. **Gửi cho ai?**  
   $\rightarrow$ Gửi đến đúng nhóm các máy khách đang quan sát lượt nộp đó: `Clients.Group("submission:{id}")`.
3. **Gửi kèm dữ liệu gì (tối thiểu)?**  
   $\rightarrow$ `SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt)`. Chỉ truyền thông tin thay đổi cần thiết để cập nhật giao diện, không truyền toàn bộ thực thể.
4. **Mất kết nối thì lấy lại trạng thái thế nào?**  
   $\rightarrow$ Gọi lại API REST: `GET /api/submissions/{id}` (REST là nguồn sự thật duy nhất).

### So sánh PingSignalR và EduSubmit Realtime
| Khái niệm | PingSignalR (Phần 1) | EduSubmit Realtime (Phần 2) |
| :--- | :--- | :--- |
| **Hub & Tuyến** | `PingHub`, `/hubs/ping` (Port 5200) | `SubmissionHub`, `/hubs/submissions` (Port 5000) |
| **Quản lý nhóm** | `JoinGroup("lop-hoc-01")` | `WatchSubmission(id)` $\rightarrow$ nhóm `submission:{id}` |
| **Tên thông điệp** | `EchoResponse`, `GroupMessage`, `BroadcastMessage` | `SubmissionStatusChanged` |
| **Dữ liệu gửi kèm** | Kiểu chuỗi đơn giản (`string`) | Đối tượng DTO có cấu trúc (`SubmissionStatusMessage`) |
| **Bên kích hoạt gửi** | Máy khách gọi trực tiếp hàm Hub | **Tác vụ nền (SimulateGrading) gửi qua `IHubContext<SubmissionHub>`** |
| **REST đi kèm** | Chỉ có `GET /` | Toàn bộ RESTful API (`GET`, `POST 201`, `GET {id}`) |

### Kết quả chạy thực tế EduSubmit (8 dòng)
Khởi chạy `Submission.RealtimeApi` (Cổng 5000) và thực thi máy khách `Submission.RealtimeClient`:
```text
Da ket noi SignalR.
Chon bai tap: 1 - Bai tap SignalR
Da tao luot nop: efc400f5-d5fa-46ef-8274-3441b5254e66
Trang thai REST ban dau: received
Dang cho thong bao trang thai tu may chu...
Thong bao SignalR: efc400f5-d5fa-46ef-8274-3441b5254e66 -> processing luc 16:50:51
Thong bao SignalR: efc400f5-d5fa-46ef-8274-3441b5254e66 -> graded luc 16:50:53
Trang thai doc lai bang REST: graded
```
*Nhận xét:* Hai thông báo SignalR cách nhau đúng 2 giây (theo tiến trình chấm bài nền). Sau khi nhận trạng thái `graded`, máy khách tự động đối chiếu lại bằng REST và kết thúc an toàn.

### Bảng 5 thí nghiệm phân tán
| # | Thí nghiệm | Dự đoán hiện tượng | Kết quả chạy thực tế | Bài học & Giải thích |
| :-: | :--- | :--- | :--- | :--- |
| **1** | Chạy 2 máy khách SV001 và SV002 cùng lúc | Mỗi máy khách chỉ nhận thông báo trạng thái của bài nộp do chính mình tạo ra. | Cả hai máy nhận đúng thông báo lượt nộp của mình, hoàn toàn không bị lẫn dữ liệu của nhau. | Việc sử dụng nhóm `submission:{id}` giúp cô lập thông điệp, bảo vệ tính riêng tư và tiết kiệm băng thông mạng. |
| **2** | Chạy máy khách khi máy chủ chưa chạy | Kết nối thất bại ngay từ bước khởi tạo `StartAsync`. | Ném ngoại lệ `HttpRequestException: Connection refused (localhost:5000)`. | Cơ chế thử lại `WithAutomaticReconnect` không áp dụng cho lần bắt tay ban đầu; máy chủ bắt buộc phải chạy trước. |
| **3** | Sửa WithUrl thành `/hub/submissions` (thiếu `s`) | Máy khách không tìm thấy tuyến endpoint và ném lỗi 404. | Ném ngoại lệ `HttpRequestException: Response status code does not indicate success: 404 (Not Found)`. | Quá trình thương lượng (`negotiate`) thất bại do sai đường dẫn URL ánh xạ trong `app.MapHub`. |
| **4** | Sửa `On("SubmissionStatusChanged")` thành `On("SubmissionStatusChange")` | Máy khách không nhận được thông báo, chờ đến khi hết hạn thời gian (timeout 10s). | Máy khách không in tin SignalR; sau 10 giây in dòng: `Qua thoi gian cho thong bao graded.`, sau đó REST đọc được `graded`. | Tên sự kiện sai khiến máy khách bỏ qua thông điệp. Tuy nhiên, REST vẫn lưu trạng thái đúng, chứng minh vai trò "nguồn sự thật". |
| **5** | Chờ 4 giây rồi mới gọi `WatchSubmission` | Máy khách bỏ lỡ cả hai thông báo `processing` và `graded` do vào nhóm muộn. | Không nhận được thông báo SignalR nào; kết thúc bằng việc đọc REST thành công trạng thái `graded`. | SignalR không gửi lại các thông điệp đã phát sinh trước khi máy khách tham gia nhóm. Trình tự đúng phải là: Đăng ký nghe $\rightarrow$ Kết nối $\rightarrow$ Tạo bài nộp $\rightarrow$ Vào nhóm $\rightarrow$ Chốt bằng REST. |

### Kiểm thử 9 ca trong requests.http
Tất cả 9 ca kiểm thử trong file `RealtimeSubmissionSystem/requests.http` đều đạt chuẩn:
1. `GET /` $\rightarrow$ `200 OK` (Kiểm tra trạng thái hoạt động của dịch vụ).
2. `GET /api/assignments` $\rightarrow$ `200 OK` (Lấy danh sách các bài tập).
3. `POST /api/assignments/1/submissions` $\rightarrow$ `201 Created` kèm tiêu đề `Location` và trạng thái `received`.
4. `GET /api/submissions/{id}` $\rightarrow$ `200 OK` (Đọc trạng thái bài nộp qua REST).
5. `POST /api/assignments/1/submissions` (thiếu `repositoryUrl`) $\rightarrow$ `400 Bad Request` (`INVALID_SUBMISSION`).
6. `POST /api/assignments/99/submissions` $\rightarrow$ `404 Not Found` (`ASSIGNMENT_NOT_FOUND`).
7. `GET /api/submissions/00000000-...` $\rightarrow$ `404 Not Found` (`SUBMISSION_NOT_FOUND`).
8. `POST /hubs/submissions/negotiate?negotiateVersion=1` $\rightarrow$ `200 OK` kèm danh sách cơ chế truyền tải (`WebSockets`, `ServerSentEvents`, `LongPolling`).
9. `POST /hub/submissions/negotiate?negotiateVersion=1` $\rightarrow$ `404 Not Found` (Xác thực đường dẫn Hub sai).

---

## 4. Phần 3: Câu hỏi ôn tập và thảo luận

### Câu hỏi ôn tập
1. **Polling khác Push ở số yêu cầu và độ trễ thế nào?**  
   - *Polling:* Số yêu cầu rất lớn ($N \times \text{chu kỳ}$), phần lớn là yêu cầu thừa không mang dữ liệu mới; độ trễ phụ thuộc vào chu kỳ lặp (ví dụ thăm dò mỗi 2s thì độ trễ tối đa là 2s).  
   - *Push:* Số thông điệp chỉ đúng bằng số sự kiện thực tế phát sinh (100% hữu ích); độ trễ gần như bằng 0 (ngay tức thì khi máy chủ có sự kiện).
2. **Phân biệt 3 MEP (Message Exchange Pattern):**  
   - *Yêu cầu - phản hồi (Request-Reply):* Do máy khách khởi tạo, 1 yêu cầu nhận 1 phản hồi (REST).
   - *Đẩy dữ liệu (Push):* Do máy chủ khởi tạo khi có sự kiện, gửi các thông điệp đơn lẻ không định kỳ (SignalR).
   - *Truyền luồng (Streaming):* Do một hoặc cả hai bên khởi tạo, dữ liệu gửi liên tục thành một chuỗi (SignalR stream, gRPC stream).
3. **WebSocket và SignalR là giao thức hay thư viện?**  
   - *WebSocket* là một **giao thức mạng tầng ứng dụng (Protocol)** chuẩn hóa bởi IETF (RFC 6455).  
   - *SignalR* là một **thư viện/framework cấp cao** của Microsoft bao bọc WebSocket và cung cấp các tính năng quản lý kết nối, nhóm, chuyển đổi định dạng và truyền tải dự phòng.
4. **Các cơ chế truyền tải dự phòng của SignalR:**  
   Theo thứ tự ưu tiên: (1) **WebSockets** $\rightarrow$ (2) **Server-Sent Events (SSE)** $\rightarrow$ (3) **Long Polling**.
5. **Vì sao tạo nhóm `submission:{id}` thay vì gửi tất cả?**  
   Để đảm bảo tính riêng tư (không để sinh viên thấy thông tin chấm bài của nhau) và tối ưu hóa băng thông mạng (tránh tình trạng hàng ngàn tin nhắn vô ích gửi đến những máy không liên quan).
6. **`IHubContext` cho phép làm gì?**  
   Cho phép phát thông điệp SignalR từ bên ngoài phạm vi vòng đời của lớp Hub (như từ Minimal API, Controller, BackgroundService, Worker).
7. **`On<T>` dùng để làm gì? Bỏ đi thì sao?**  
   Dùng để đăng ký phương thức callback phía máy khách lắng nghe một sự kiện từ máy chủ và deserialize JSON payload sang kiểu `T`. Nếu bỏ đi, máy khách sẽ bỏ qua thông điệp từ máy chủ mà không xử lý.
8. **Vì sao không dùng SignalR để tạo lượt nộp bài?**  
   Tạo lượt nộp là thao tác tạo tài nguyên nghiệp vụ, cần xác thực nghiêm ngặt, trả về mã trạng thái HTTP chuẩn mực (`201`, `400`, `404`) và cần lưu trữ bền vững. REST là chuẩn kiến trúc phù hợp nhất cho việc quản lý vòng đời tài nguyên.
9. **`WithAutomaticReconnect` có tác dụng gì?**  
   Tự động phát hiện khi kết nối bị gián đoạn và thực hiện kết nối lại theo lịch trình (0s, 2s, 10s, 30s) mà không làm sập ứng dụng máy khách.
10. **Mất kết nối đúng lúc báo graded thì làm gì?**  
    Chờ kết nối lại hoặc hết thời gian chờ, sau đó gọi REST `GET /api/submissions/{id}` để đồng bộ trạng thái chính xác từ máy chủ.

### Thảo luận
- **Hàng trăm sinh viên cùng theo dõi: nhóm theo ID hay phát tất cả rồi tự lọc?**  
  $\rightarrow$ Nhóm theo ID (`Clients.Group`) tiết kiệm băng thông vượt trội. Nếu có 100 sinh viên, phát tất cả sẽ tạo $100 \times 200 = 20.000$ thông điệp qua mạng (lãng phí 99%). Dùng nhóm chỉ tạo đúng 200 thông điệp đến đúng đích.
- **2 tình huống Polling hợp lý hơn SignalR:**  
  1. Dữ liệu thay đổi rất chậm hoặc định kỳ cố định (ví dụ: tỷ giá ngoại tệ cập nhật mỗi giờ 1 lần, dự báo thời tiết cập nhật mỗi 30 phút).  
  2. Môi trường mạng khắt khe (firewall, proxy công ty chặn các kết nối persistent dài hạn và không hỗ trợ giao thức WebSocket).
- **Vấn đề khi scale-out nhiều bản sao API và giải pháp:**  
  $\rightarrow$ Vấn đề: Danh sách kết nối và Group được lưu trữ trong bộ nhớ RAM của từng máy chủ đơn lẻ (In-Memory). Nếu sinh viên kết nối vào Server A nhưng tác vụ chấm bài diễn ra ở Server B, Server B sẽ không thể gửi tin nhắn đến sinh viên đó.  
  $\rightarrow$ Giải pháp: Sử dụng **SignalR Backplane** (phổ biến nhất là Redis Backplane hoặc Azure SignalR Service) làm kênh truyền thông điệp trung gian giữa các máy chủ.

---

## 5. Phần 4: Bài tập mở rộng (Bài tập 2 & 3 - LoiGiai)

Thư mục `RealtimeSubmissionSystem/LoiGiai/` chứa lời giải mở rộng đạt điểm thưởng tối đa:
- **`Submission.LoiGiaiApi.cs`:** Kiểm tra URL bài nộp, nếu không bắt đầu bằng `https://` sẽ chuyển sang trạng thái cuối cùng là `failed`.
- **`Submission.LoiGiaiClient.cs`:**  
  - Sử dụng `ConcurrentDictionary<Guid, TaskCompletionSource<string>>` để theo dõi đồng thời 2 lượt nộp khác nhau trên cùng 1 kết nối SignalR duy nhất.
  - Xử lý đổi màu chữ hiển thị cảnh báo đỏ khi nhận trạng thái `failed`.
  - In kết quả đối chiếu chính xác giữa SignalR và REST:
    ```text
    SV001: SignalR = graded, REST = graded
    SV002: SignalR = failed, REST = failed
    ```

---
*Báo cáo được hoàn thành đầy đủ theo đúng cấu trúc và tiêu chí đánh giá của bài giảng.*
