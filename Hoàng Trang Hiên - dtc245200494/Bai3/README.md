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
| ![Bước 5 - Api](img/Bước%205%20-%20thêm%20phát%20rộng%20và%20nhóm%20-%20Api.png) | ![Bước 5 - Client](img/Bước%205%20-%20thêm%20phát%20rộng%20và%20nhóm%20-%20Client.png) |
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

## III. Các bước thực hành EduSubmit Realtime (Phần 3)

### Bước 1: Tạo solution mới với hai project như Bài 2, thêm gói cho máy khách
![Bước 1 - tạo solution mới với hai project như Bài 2, thêm gói cho máy khách](img/Bước%201%20-%20tạo%20solution%20mới%20với%20hai%20project%20như%20Bài%202,%20thêm%20gói%20cho%20máy%20khách.png)
* **Thao tác:**
  * Tạo solution `RealtimeSubmissionSystem.slnx`.
  * Tạo project máy chủ `Submission.RealtimeApi` (template web, chạy cổng 5000).
  * Tạo project máy khách `Submission.RealtimeClient` (template console).
  * Cài đặt gói `Microsoft.AspNetCore.SignalR.Client --version 10.0.15` cho máy khách.
  * Cấu hình `Directory.Build.props` và `.gitignore` để build thành công 0 Warning, 0 Error.

---

### Bước 2: Dán phần REST của Bài 2, thêm UpdatedAt và một khóa dữ liệu
![Bước 2 - dán phần REST của Bài 2, thêm UpdatedAt và khóa dữ liệu](img/Bước%202%20-%20dán%20phần%20REST%20của%20Bài%202,%20thêm%20UpdatedAt%20và%20khóa%20dữ%20liệu.png)
* **Thao tác:**
  * Kế thừa toàn bộ endpoint REST Bài 2: `GET /api/assignments`, `POST /api/assignments/{id}/submissions`, `GET /api/submissions/{id}`.
  * Thêm thuộc tính `UpdatedAt` vào `SubmissionDto` để lưu thời điểm cập nhật trạng thái gần nhất.
  * Thêm `submissionsLock` với từ khóa `lock` để bảo vệ tài nguyên danh sách khi tác vụ nền và HTTP request cùng truy cập đồng thời.

---

### Bước 3: Hub mỏng chỉ làm một việc, đưa kết nối vào nhóm của lượt nộp
![Bước 3 - hub mỏng đưa kết nối vào nhóm của lượt nộp](img/Bước%203%20-%20hub%20mỏng%20đưa%20kết%20nối%20vào%20nhóm%20của%20lượt%20nộp.png)
* **Thao tác:**
  * Đăng ký SignalR và ánh xạ hub: `app.MapHub<SubmissionHub>("/hubs/submissions")`.
  * Khai báo `SubmissionHub` với phương thức duy nhất `WatchSubmission(submissionId)` đưa máy khách vào nhóm `submission:{id}`.
  * Kiểm tra bắt tay thành công với `curl -X POST "http://localhost:5000/hubs/submissions/negotiate?negotiateVersion=1"` trả về HTTP 200 và `connectionId`.

---

### Bước 4: POST nhận thêm IHubContext và khởi động chấm bài ở nền
![Bước 4 kiểm tra - REST thấy trạng thái đổi dần](img/Bước%204%20kiểm%20tra%20-%20REST%20thấy%20trạng%20thái%20đổi%20dần%20từ%20received%20sang%20processing%20và%20graded.png)
* **Thao tác:**
  * Tại endpoint POST, inject `IHubContext<SubmissionHub> hubContext`.
  * Khởi động tác vụ nền bất đồng bộ `_ = SimulateGradingAsync(...)` không `await` để trả về HTTP 201 Created ngay lập tức.
  * Tác vụ nền dùng `Task.Delay`: sau 1 giây cập nhật `processing` và đẩy vào nhóm; sau 2 giây tiếp cập nhật `graded` và đẩy vào nhóm.
* **Kết quả kiểm tra:** Gọi curl GET nhiều lần liên tiếp thấy trạng thái đổi dần từ `received` $\rightarrow$ `processing` $\rightarrow$ `graded`.

---

### Bước 5 & 6: Máy khách mở hai kênh, tạo lượt nộp bằng REST, theo dõi bằng SignalR, chốt lại bằng REST
![Bước 5 & 6 - máy khách mở hai kênh và theo dõi lượt nộp](img/Bước%205%20&%206%20-%20máy%20khách%20mở%20hai%20kênh%20và%20theo%20dõi%20lượt%20nộp.png)
* **Thao tác:**
  * Khởi tạo đồng thời `HttpClient` (kênh REST) và `HubConnection` (kênh SignalR).
  * Đăng ký lắng nghe sự kiện `connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", ...)`.
  * Dùng `TaskCompletionSource` làm chuông báo hiệu khi nhận được trạng thái `graded`.
  * Trình tự: `POST nộp bài` $\rightarrow$ `lấy ID` $\rightarrow$ `gọi WatchSubmission vào nhóm` $\rightarrow$ `chờ chuông SignalR` $\rightarrow$ `gọi GET đọc lại chốt kết quả` $\rightarrow$ `tự động kết thúc`.

---

### Bước 6 Kết quả: Hai cập nhật đến từ máy chủ mà máy khách không hề hỏi
![Bước 6 kết quả - máy khách nhận hai cập nhật thời gian thực](img/Bước%206%20kết%20quả%20-%20máy%20khách%20nhận%20hai%20cập%20nhật%20thời%20gian%20thực%20từ%20máy%20chủ.png)
* **Kết quả hiển thị tại máy khách (đúng chuẩn 8 dòng):**
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

---

### Bước 7: Bảng 5 thí nghiệm phân tán (Slide 58 - 59)
| STT | Thí nghiệm | Dự đoán | Kết quả chạy thật | Bài học phân tán |
|:---:|:---|:---|:---|:---|
| **1** | Chạy 2 máy khách SV001 và SV002 cùng lúc | Mỗi máy khách chỉ nhận thông báo về bài nộp của chính mình. | Cửa sổ SV001 chỉ in thông báo bài SV001, SV002 chỉ in thông báo bài SV002. | Nhóm `submission:{id}` giúp cô lập phạm vi gửi, đảm bảo tính riêng tư dữ liệu và tiết kiệm băng thông mạng. |
| **2** | Chạy máy khách khi máy chủ chưa chạy | Báo lỗi kết nối ngay lập tức do không tìm thấy endpoint. | `HttpRequestException: Connection refused (localhost:5000)` | Lần kết nối đầu tiên (`StartAsync`) không tự động thử lại. `WithAutomaticReconnect` chỉ có tác dụng khi kết nối ĐÃ từng được mở thành công. |
| **3** | Sửa `WithUrl` thành `/hub/submissions` (thiếu chữ 's') | Trả về mã lỗi 404 Not Found. | `HttpRequestException: Response status code does not indicate success: 404 (Not Found)` | Lỗi xảy ra ngay ở bước bắt tay (`negotiate`). Tuyến URL ở máy khách phải khớp 100% với cấu hình `MapHub` ở máy chủ. |
| **4** | Sửa `On("SubmissionStatusChanged")` thành `On("SubmissionStatusChange")` | Không in được thông báo trạng thái, không báo lỗi biên dịch. | Chương trình chạy bình thường, im lặng không in thông báo, 10s sau báo "Qua thoi gian cho thong bao graded". | SignalR là "hợp đồng theo tên chuỗi". Lệch tên sự kiện ở chiều hub gửi xuống máy khách sẽ bị bỏ qua trong im lặng, rất khó phát hiện. |
| **5** | Chờ 4 giây rồi mới gọi `WatchSubmission` | Không nhận được thông điệp thời gian thực nào. | Không nhận được thông báo SignalR nào (`processing`/`graded`). Nhưng khi gọi REST đọc lại thì vẫn ra `graded`. | Vào nhóm muộn sẽ bị lỡ tin nhắn do SignalR không giữ tin cũ. REST chính là nguồn sự thật giúp cứu vãn tình trạng lỡ tin. |

---

### Bước 8: requests.http gom các ca REST và bước bắt tay của hub
![Bước 8 - requests.http gom 9 ca REST và bước bắt tay của hub](img/Bước%208%20-%20requests.http%20gom%209%20ca%20REST%20và%20bước%20bắt%20tay%20của%20hub.png)
1. `GET /`: Kiểm tra máy chủ đang chạy (200 OK).
2. `GET /api/assignments`: Lấy danh sách bài tập (200 OK).
3. `POST /api/assignments/1/submissions`: Tạo lượt nộp hợp lệ (201 Created + header `Location`).
4. `GET /api/submissions/{id}`: Đọc lại trạng thái lượt nộp (200 OK, thấy đổi dần `received` $\rightarrow$ `processing` $\rightarrow$ `graded`).
5. `POST /api/assignments/1/submissions` thiếu dữ liệu: Thiếu studentCode/repositoryUrl (400 Bad Request).
6. `POST /api/assignments/999/submissions`: Nộp vào bài tập không tồn tại (404 Not Found).
7. `GET /api/submissions/{guid_khong_ton_tai}`: Đọc bài nộp không tồn tại (404 Not Found).
8. `POST /hubs/submissions/negotiate?negotiateVersion=1`: Bắt tay SignalR hợp lệ (200 OK + `connectionId`).
9. `POST /hub/submissions/negotiate?negotiateVersion=1`: Sai đường dẫn hub (404 Not Found).

---

## IV. Vận dụng nâng cao (Bài tập 2 - Điểm cộng Slide 62)

### Xử lý trạng thái failed và cảnh báo trực quan
![Vận dụng - bài tập mở rộng trạng thái failed với cảnh báo đỏ](img/Vận%20dụng%20-%20bài%20tập%20mở%20rộng%20trạng%20thái%20failed%20với%20cảnh%20báo%20đỏ.png)
* **Máy chủ:** Kiểm tra `RepositoryUrl`. Nếu không bắt đầu bằng `https://`, tác vụ chấm bài sẽ chuyển trạng thái cuối cùng thành `failed`.
* **Máy khách:** Khi nhận trạng thái `failed`, máy khách đổi màu console sang màu đỏ và phát cảnh báo: `!!! CANH BAO: luot nop ... cham LOI (failed)`.
* **Đối soát REST:** Khi gọi lại `GET /api/submissions/{id}`, REST cũng xác nhận trạng thái cuối cùng là `failed`.

---

## V. Thảo luận & Mở rộng (Slide 68)

* **Hàng trăm sinh viên cùng theo dõi:** Sử dụng nhóm `Clients.Group("submission:{id}")` là giải pháp tối ưu. Nó đảm bảo tính riêng tư tuyệt đối (sinh viên này không xem được bài của sinh viên khác) và tiết kiệm băng thông tối đa (máy chủ chỉ gửi đúng 1 thông báo cho người quan tâm, thay vì gửi cho hàng trăm kết nối để máy khách tự lọc).
* **Tình huống nên dùng Polling thay vì SignalR:**
  1. Dữ liệu rất ít biến động (như bảng giá định kỳ, thời tiết), độ trễ vài chục giây là chấp nhận được.
  2. Hệ thống chạy trong môi trường mạng doanh nghiệp khắt khe chặn kết nối lâu dài và nâng cấp giao thức WebSocket.
* **Mở rộng quy mô (Scale-out):** Khi triển khai nhiều máy chủ API phía sau Load Balancer, các kết nối WebSocket được phân tán ở nhiều máy chủ khác nhau. Cần sử dụng một **Backplane** tập trung (như Redis Backplane hoặc Azure SignalR Service) làm kênh trao đổi tin nhắn trung gian để thông điệp phát ở máy chủ A có thể chuyển tiếp tới máy khách đang kết nối ở máy chủ B.
