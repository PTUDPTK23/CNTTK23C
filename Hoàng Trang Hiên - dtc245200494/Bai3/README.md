# Báo cáo Thực hành Bài 3: Giao tiếp thời gian thực với SignalR

- **Sinh viên:** Hoàng Trang Hiên
- **Mã sinh viên:** dtc245200494
- **Lớp:** CNTTK23C
- **Học phần:** Phát triển ứng dụng phân tán
- **Giảng viên:** TS. Dương Thu Mây

---

## I. Trả lời 3 câu hỏi đầu giờ (Slide 65)

### 1. Máy chủ có thể chủ động báo cho máy khách mà không cần máy khách hỏi không?
**Có.** Máy khách mở sẵn một kết nối lâu dài (Persistent Connection) tới máy chủ thông qua giao thức WebSocket (hoặc fallback sang Server-Sent Events, Long Polling qua thư viện SignalR). Khi có sự kiện phát sinh, máy chủ chủ động đẩy (push) thông điệp về máy khách bằng `Clients.Caller.SendAsync`, `Clients.Group(...).SendAsync` hoặc `Clients.All.SendAsync`. Khi sự kiện phát sinh ở bên ngoài hub (như trong background worker hoặc HTTP endpoint), máy chủ sử dụng `IHubContext<THub>` để phát tin.

### 2. Đã có REST rồi, vì sao còn cần thêm SignalR? Hai thứ chia việc thế nào?
REST và SignalR không thay thế nhau mà bổ trợ cho nhau:
* **REST (Nguồn sự thật - Single Source of Truth):** Đảm nhiệm mô hình Yêu cầu - Phản hồi (Request - Response). Dùng để tạo tài nguyên (POST 201 Created), truy vấn trạng thái (GET 200 OK), xử lý lỗi nghiệp vụ rõ ràng (400, 404, 409).
* **SignalR (Kênh thông báo tức thời - Fast Push):** Đảm nhiệm mô hình Đẩy dữ liệu (Push). Dùng để thông báo ngay lập tức các thay đổi trạng thái phát sinh bất đồng bộ sau yêu cầu ban đầu (ví dụ: `received -> processing -> graded`).
* **Nguyên tắc vàng:** "Push để nhanh, REST để chắc". Khi mất kết nối hoặc tải lại trang, máy khách luôn gọi REST để chốt lại trạng thái chính xác nhất.

### 3. Nếu đứt mạng đúng lúc máy chủ báo "đã chấm xong" thì sao?
* Do SignalR không lưu trữ hàng đợi tin nhắn chưa gửi (không đảm bảo phân phối tin cậy mức ứng dụng), thông điệp realtime đó sẽ bị mất.
* Nhờ cơ chế `WithAutomaticReconnect()`, khi mạng khôi phục, máy khách sẽ tự động kết nối lại (được cấp `ConnectionId` mới). Lúc này máy khách chủ động gọi lại `WatchSubmission` để vào lại nhóm và gọi `GET /api/submissions/{id}` qua REST để lấy trạng thái cuối cùng ("graded") mà không sợ bị sai lệch dữ liệu.

---

## II. Các bước thực hành PingSignalR (Kèm ảnh minh chứng)

### Bước 2: Máy chủ chỉ cần thêm hai dòng để có một hub SignalR
![Bước 2 - máy chủ chỉ cần thêm hai dòng để có một hub SignalR](img/Bước%202%20-%20máy%20chủ%20chỉ%20cần%20thêm%20hai%20dòng%20để%20có%20một%20hub%20SignalR.png)
* **Thao tác:** Trong `src/Ping.Api/Program.cs`, thêm dịch vụ SignalR qua `builder.Services.AddSignalR()`, ánh xạ endpoint qua `app.MapHub<PingHub>("/hubs/ping")`, và khai báo lớp `sealed class PingHub : Hub { }`.
* **Kết quả kiểm tra:** Chạy máy chủ tại cổng 5200, dùng curl kiểm tra endpoint bắt tay `curl.exe -X POST "http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1"`, máy chủ trả về `connectionId`, `connectionToken` và danh sách `availableTransports` (WebSockets, ServerSentEvents, LongPolling).

---

### Bước 3: Phương thức Echo, máy chủ trả lời riêng người vừa gọi
![Bước 3 - phương thức Echo, máy chủ trả lời riêng người vừa gọi](img/Bước%203%20-%20phương%20thức%20Echo,%20máy%20chủ%20trả%20lời%20riêng%20người%20vừa%20gọi.png)
* **Thao tác:** Bổ sung phương thức `Echo(string message)` vào lớp `PingHub`:
  ```csharp
  public async Task Echo(string message)
  {
      await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
  }
  ```
* **Giải thích:** Sử dụng `Clients.Caller` để đảm bảo thông điệp phản hồi chỉ được gửi ngược lại cho chính máy khách vừa phát lệnh gọi.

---

### Bước 4.1: Máy khách xây kết nối, đăng ký nghe, rồi mở kết nối
![Bước 4.1 - máy khách xây kết nối, đăng ký nghe, rồi mở kết nối](img/Bước%204.1%20-%20máy%20khách%20xây%20kết%20nối,%20đăng%20ký%20nghe,%20rồi%20mở%20kết%20nối.png)
* **Thao tác:** Trong `src/Ping.Client/Program.cs`, khởi tạo đối tượng kết nối bằng `HubConnectionBuilder`, kích hoạt `WithAutomaticReconnect()`, đăng ký hàm xử lý sự kiện `connection.On<string>("EchoResponse", ...)` trước khi gọi `await connection.StartAsync()`.

---

### Bước 4.2: Gọi Echo bằng InvokeAsync rồi chạy hai tiến trình
![Bước 4.2 - gọi Echo bằng InvokeAsync rồi chạy hai tiến trình](img/Bước%204.2%20-%20gọi%20Echo%20bằng%20InvokeAsync%20rồi%20chạy%20hai%20tiến%20trình.png)
* **Thao tác:** Gọi `await connection.InvokeAsync("Echo", $"Xin chao SignalR tu {name}");` và dùng `Console.ReadLine()` giữ tiến trình sống để tiếp tục nhận thông điệp.
* **Kết quả:** Máy khách in ra `[Echo] Echo: Xin chao SignalR tu An`.

---

### Bước 5: Thêm phát rộng và nhóm, ba phạm vi gửi trong cùng một hub
| Phía Máy chủ (Ping.Api) | Phía Máy khách (Ping.Client) |
|:---:|:---:|
| ![Bước 5 - Api](img/Bước%205%20-%20thêm%20phát%20rộng%20và%20nhóm%20(Api).png) | ![Bước 5 - Client](img/Bước%205%20-%20thêm%20phát%20rộng%20và%20nhóm%20(Client).png) |
* **Thao tác:**
  * Tại Hub: Thêm `Broadcast(message)` sử dụng `Clients.All`, `JoinGroup(groupName)` sử dụng `Groups.AddToGroupAsync`, và `SendToGroup(groupName, message)` sử dụng `Clients.Group`.
  * Tại Client: Đăng ký nghe sự kiện `BroadcastMessage`, `GroupMessage`, đồng thời gọi các hàm tương ứng trên hub.

---

### Bước 5.2: Dán ba đoạn code vào đúng chỗ trong hai tệp Program.cs
![Bước 5.2 - dán ba đoạn code vào đúng chỗ](img/Bước%205.2%20-%20dán%20ba%20đoạn%20code%20vào%20đúng%20chỗ%20trong%20hai%20tệp%20Program.cs.png)
* **Thao tác:** Rà soát vị trí đặt code: các phương thức hub phải nằm trong cặp ngoặc `{}` của lớp `PingHub`; các hàm `connection.On` phải đặt trước `StartAsync()`; các lệnh `InvokeAsync` đặt sau `StartAsync()`.

---

### Bước 5.3: Chạy ba terminal theo thứ tự để An và Bình thấy tin của nhau
| Terminal 1: Máy chủ chạy trước | Terminal 2: An chạy thứ hai | Terminal 3: Bình chạy sau cùng |
|:---:|:---:|:---:|
| ![T1 Server](img/Bước%205.3%20-%20terminal%201%20máy%20chủ%20chạy%20trước.png) | ![T2 An](img/Bước%205.3%20-%20terminal%202%20An%20chạy%20thứ%20hai.png) | ![T3 Binh](img/Bước%205.3%20-%20terminal%203%20Bình%20chạy%20sau%20cùng.png) |
* **Kết quả:**
  * Terminal 2 (An) nhận được tin thông báo vào nhóm và tin broadcast của Bình (`[Group]`, `[Broadcast]`).
  * Terminal 2 (An) không nhận được tin `[Echo]` của Bình vì `Clients.Caller` chỉ trả lời riêng cho người gọi.

---

### Bước 6: Vòng đời kết nối, máy chủ biết ai vừa nối, máy khách biết khi rớt
![Bước 6 - vòng đời kết nối](img/Bước%206%20-%20vòng%20đời%20kết%20nối%20và%20thí%20nghiệm%20kết%20nối%20lại.png)
* **Thao tác:** Ghi đè `OnConnectedAsync` và `OnDisconnectedAsync` trên `PingHub`; đăng ký các sự kiện vòng đời `Reconnecting`, `Reconnected`, `Closed` ở máy khách.
* **Thí nghiệm tắt/bật lại máy chủ:**
  * Tắt Terminal 1: Máy khách lập tức kích hoạt `Reconnecting` và in `[Lifecycle] Dang ket noi lai...`.
  * Khởi động lại Terminal 1: Máy khách kích hoạt `Reconnected`, in `[Lifecycle] Da ket noi lai: ...` kèm một `ConnectionId` mới.

---

### Bước 7: Ba thí nghiệm lỗi, mỗi lỗi một bài học về kết nối phân tán
| Thí nghiệm 1: Máy chủ chưa chạy | Thí nghiệm 2: Sai đường dẫn hub | Thí nghiệm 3: Sai tên thông điệp |
|:---:|:---:|:---:|
| ![Lỗi 1](img/Bước%207%20-%20thí%20nghiệm%20lỗi%201%20Máy%20chủ%20chưa%20chạy.png) | ![Lỗi 2](img/Bước%207%20-%20thí%20nghiệm%20lỗi%202%20Sai%20đường%20dẫn%20hub.png) | ![Lỗi 3](img/Bước%207%20-%20thí%20nghiệm%20lỗi%203%20Sai%20tên%20thông%20điệp.png) |
* **Lỗi 1 (Máy chủ chưa chạy):** `HttpRequestException: Connection refused (localhost:5200)`. Bài học: `StartAsync` ném lỗi ngay; cơ chế tự kết nối lại chỉ cứu kết nối đã được mở thành công.
* **Lỗi 2 (Sai đường dẫn hub `/hub/ping` thiếu 's'):** `HttpRequestException: Response status code does not indicate success: 404 (Not Found)`. Bài học: Lỗi xảy ra ở bước negotiate do không khớp route `MapHub`.
* **Lỗi 3 (Sai tên thông điệp `EchoRespone`):** Không có lỗi ném ra, nhưng màn hình im lặng không in gì. Bài học: Khi không nhận được thông điệp, việc đầu tiên cần làm là đối soát tên chuỗi giữa hai phía.

---

## III. Hệ thống nộp bài thời gian thực EduSubmit (`RealtimeSubmissionSystem`)

### 1. Kết quả chạy máy khách EduSubmit (Slide 57, 71)
Máy khách `Submission.RealtimeClient` thực thi đúng chuẩn 8 dòng và tự động thoát:
```text
Da ket noi SignalR.
Chon bai tap: 1 - Bai tap SignalR
Da tao luot nop: ea702ca2-f52b-49cc-a675-851a81c743d1
Trang thai REST ban dau: received
Dang cho thong bao trang thai tu may chu...
Thong bao SignalR: ea702ca2-f52b-49cc-a675-851a81c743d1 -> processing
Thong bao SignalR: ea702ca2-f52b-49cc-a675-851a81c743d1 -> graded
Trang thai doc lai bang REST: graded
```

### 2. Bảng 5 thí nghiệm phân tán (Slide 58 - 59)
| STT | Thí nghiệm | Dự đoán | Kết quả chạy thật | Bài học phân tán |
|:---:|:---|:---|:---|:---|
| **1** | Chạy 2 máy khách SV001 và SV002 cùng lúc | Mỗi máy khách chỉ nhận thông báo về bài nộp của chính mình. | Cửa sổ SV001 chỉ in thông báo bài SV001, SV002 chỉ in thông báo bài SV002. | Nhóm `submission:{id}` giúp cô lập phạm vi gửi, đảm bảo tính riêng tư dữ liệu và tiết kiệm băng thông mạng. |
| **2** | Chạy máy khách khi máy chủ chưa chạy | Báo lỗi kết nối ngay lập tức do không tìm thấy endpoint. | `HttpRequestException: Connection refused (localhost:5000)` | Lần kết nối đầu tiên (`StartAsync`) không tự động thử lại. `WithAutomaticReconnect` chỉ có tác dụng khi kết nối ĐÃ từng được mở thành công. |
| **3** | Sửa `WithUrl` thành `/hub/submissions` (thiếu chữ 's') | Trả về mã lỗi 404 Not Found. | `HttpRequestException: Response status code does not indicate success: 404 (Not Found)` | Lỗi xảy ra ngay ở bước bắt tay (`negotiate`). Tuyến URL ở máy khách phải khớp 100% với cấu hình `MapHub` ở máy chủ. |
| **4** | Sửa `On("SubmissionStatusChanged")` thành `On("SubmissionStatusChange")` | Không in được thông báo trạng thái, không báo lỗi biên dịch. | Chương trình chạy bình thường, im lặng không in thông báo, 10s sau báo "Qua thoi gian cho thong bao graded". | SignalR là "hợp đồng theo tên chuỗi". Lệch tên sự kiện ở chiều hub gửi xuống máy khách sẽ bị bỏ qua trong im lặng, rất khó phát hiện. |
| **5** | Chờ 4 giây rồi mới gọi `WatchSubmission` | Không nhận được thông điệp thời gian thực nào. | Không nhận được thông báo SignalR nào (`processing`/`graded`). Nhưng khi gọi REST đọc lại thì vẫn ra `graded`. | Vào nhóm muộn sẽ bị lỡ tin nhắn do SignalR không giữ tin cũ. REST chính là nguồn sự thật giúp cứu vãn tình trạng lỡ tin. |

### 3. Kiểm thử 9 ca trong `requests.http` (Slide 61)
1. `GET /`: Kiểm tra máy chủ (Mã 200 OK).
2. `GET /api/assignments`: Lấy danh sách bài tập (Mã 200 OK).
3. `POST /api/assignments/1/submissions`: Tạo lượt nộp bài hợp lệ (Mã 201 Created + header `Location`).
4. `GET /api/submissions/{id}`: Đọc lại trạng thái lượt nộp (Mã 200 OK, thấy chuyển từ `received` -> `processing` -> `graded`).
5. `POST /api/assignments/1/submissions` thiếu dữ liệu: Thiếu studentCode/repositoryUrl (Mã 400 Bad Request).
6. `POST /api/assignments/999/submissions`: Nộp vào bài tập không tồn tại (Mã 404 Not Found).
7. `GET /api/submissions/{guid_khong_ton_tai}`: Đọc bài nộp không tồn tại (Mã 404 Not Found).
8. `POST /hubs/submissions/negotiate?negotiateVersion=1`: Bắt tay SignalR hợp lệ (Mã 200 OK + `connectionId`).
9. `POST /hub/submissions/negotiate?negotiateVersion=1`: Sai đường dẫn hub (Mã 404 Not Found).

---

## IV. Bài tập mở rộng & Thảo luận (Slide 62, 68)

### 1. Bài tập mở rộng (Bài tập 2 - Trạng thái failed)
* Hệ thống kiểm tra `RepositoryUrl`. Nếu không bắt đầu bằng `https://`, tác vụ chấm bài sẽ chuyển trạng thái cuối cùng thành `failed`.
* Máy khách bắt trạng thái `failed` và in cảnh báo màu đỏ: `!!! CANH BAO: luot nop ... cham LOI (failed)`.

### 2. Thảo luận
* **Hàng trăm sinh viên cùng theo dõi:** Nên dùng nhóm `Clients.Group("submission:{id}")` thay vì `Clients.All`. Nhóm theo ID đảm bảo chỉ gửi đúng 1 thông báo cho người quan tâm, tiết kiệm tối đa băng thông so với việc broadcast cho toàn bộ kết nối rồi để máy khách tự lọc.
* **Tình huống nên dùng Polling thay vì SignalR:**
  1. Dữ liệu rất ít thay đổi (ví dụ tỷ giá ngân hàng, thời tiết cập nhật theo giờ), độ trễ vài chục giây không ảnh hưởng đến nghiệp vụ.
  2. Môi trường mạng khắt khe (firewall hoặc proxy chặn kết nối kéo dài và nâng cấp giao thức WebSocket).
* **Mở rộng nhiều bản sao (Scale-out):** Khi chạy nhiều instance API phía sau Load Balancer, bộ nhớ Hub ở các node là độc lập, kết nối của sinh viên ở server A sẽ không nhận được tin do server B phát ra. Cách giải quyết là sử dụng một **Backplane** tập trung (như Redis Backplane hoặc Azure SignalR Service) để định tuyến thông điệp giữa các server.
