# Bài 1 - Tổng quan hệ thống và MEP

**Họ tên:** Nguyễn Thành Long  
**MSSV:** DTC245200171  
**Lớp:** CNTT K23C  

---

## 1. Cài đặt (Bước 4)
- Kiểm tra phiên bản .NET SDK: `.NET 10.0`
- Kiểm tra phiên bản Git: `git version 2.x`
- Khởi tạo và chạy dự án console `HelloPtudpt`: Biên dịch và thực thi thành công, terminal hiển thị kết quả: `Hello, World!`

---

## 2. MepDemo (Bước 7)
### Trả lời 3 câu hỏi:
1. **Ở MEP 3 (Queue-based Worker Pool), mỗi bài nộp sub-x được mấy worker xử lý?**
   - Mỗi bài nộp chỉ được **đúng 1 worker duy nhất** xử lý.
   - *Giải thích:* Đây là mô hình cạnh tranh nhận việc (Competing Consumers Pattern). Các worker cùng lắng nghe hàng đợi chung (Queue); worker nào rảnh trước sẽ lấy thông điệp ra xử lý, giúp cân bằng tải và đảm bảo một bài nộp không bị xử lý trùng lặp.

2. **Ở MEP 4 (Publish-Subscribe / Event-Driven), một sự kiện được gửi tới mấy dịch vụ?**
   - Một sự kiện nộp bài được đồng loạt gửi tới **toàn bộ 3 dịch vụ** đăng ký nhận tin (Notification Service, Audit Service, Analytics Service).
   - *Giải thích:* Mô hình hướng sự kiện (Event-Driven) phân phối một-nhiều; bên phát (Publisher) bắn sự kiện lên kênh chung (Topic), và tất cả các bên đăng ký (Subscribers) đều nhận được bản sao độc lập của sự kiện để xử lý theo nghiệp vụ riêng.

3. **Mỗi MEP 1 câu mô tả:**
   - **MEP 1 (Request - Response):** Mô hình đồng bộ hai chiều, bên gửi (Client) gửi yêu cầu và đợi bên nhận (Server) xử lý rồi trả kết quả về.
   - **MEP 2 (One-Way / Fire & Forget):** Mô hình trao đổi một chiều, client gửi thông điệp đi và tiếp tục ngay công việc mà không cần chờ bất kỳ phản hồi nào.
   - **MEP 3 (Queue-based Worker Pool):** Sử dụng hàng đợi trung gian để lưu đệm thông điệp, cho phép nhiều worker cùng chia nhau xử lý tải bất đồng bộ.
   - **MEP 4 (Publish - Subscribe):** Mô hình phân phối một-nhiều, sự kiện từ bên phát được đồng thời chuyển đến tất cả các bên đăng ký quan tâm.
   - **MEP 5 (Streaming):** Dòng dữ liệu liên tục được truyền tuần tự theo thời gian thực qua một kết nối mở kéo dài giữa hai đầu cuối thay vì gửi toàn bộ một lần.

---

## 3. BasicWebApi (Bước 8)
- Ảnh chụp REST Client: 200, 201, 400

### Build và chạy BasicWebApi

![Build thành công và BasicWebApi chạy tại localhost:5081](docs/images/basicwebapi-build-run.png)

### Kiểm tra API — 200 OK

![GET / trả về 200 OK và thông tin BasicWebApi](docs/images/basicwebapi-200-ok.png)

### Tạo lời chào hợp lệ — 201 Created

![POST /greetings với tên Binh trả về 201 Created](docs/images/basicwebapi-201-created.png)

### Kiểm tra dữ liệu không hợp lệ — 400 Bad Request

![Gửi tên rỗng và nhận phản hồi 400 Bad Request](docs/images/basicwebapi-400-bad-request.png)

### Trả lời câu hỏi mục 8.4

**1. Hai dòng `builder.Services.AddSingleton<GreetingService>()` và `var app = builder.Build();`, dòng nào phải đứng trước? Vì sao?**

Dòng `builder.Services.AddSingleton<GreetingService>();` phải đứng trước `var app = builder.Build();`. Cần đăng ký dịch vụ trước khi xây dựng ứng dụng để hệ thống dependency injection (DI) tạo bộ cung cấp dịch vụ có chứa `GreetingService`. Khi endpoint POST cần tham số `GreetingService service`, DI sẽ cung cấp đối tượng đã đăng ký. Sau khi gọi `Build()`, danh sách đăng ký dịch vụ trở thành chỉ đọc, nên không thể gọi `AddSingleton` để bổ sung dịch vụ nữa. `Singleton` nghĩa là một đối tượng dịch vụ được dùng chung trong suốt vòng đời ứng dụng.

**2. Trong `/hello/{name}?language=en`, tham số `name` lấy từ đâu, `language` lấy từ đâu?**

Tham số `name` lấy từ đường dẫn URL (route), tại vị trí `{name}`; tham số `language` lấy từ chuỗi truy vấn (query string), sau dấu `?`. Ví dụ, với `/hello/An?language=en`, giá trị `name` là `An` và `language` là `en`, nên API trả lời `Hello, An!`. Trong mã hiện tại, nếu không truyền `language` hoặc giá trị khác `en` thì API trả lời bằng tiếng Việt.

**3. Vì sao POST hợp lệ trả `Results.Created` (201) chứ không phải `Results.Ok` (200)?**

POST `/greetings` hợp lệ tạo một lời chào mới có `id`, `name` và `createdAt`, nên trả `201 Created` để thể hiện tài nguyên mới đã được tạo. `Results.Created($"/greetings/{greeting.Id}", greeting)` còn trả dữ liệu lời chào trong body và đặt header `Location` thành đường dẫn của tài nguyên mới, ví dụ `/greetings/3`. `200 OK` chỉ cho biết yêu cầu thành công nói chung, không diễn đạt rõ việc tạo tài nguyên mới. Trong bài mẫu hiện tại, đường dẫn `Location` được trả về nhưng chưa có endpoint GET tương ứng để truy xuất lời chào theo ID.

### 8.5. Thêm ngôn ngữ tiếng Nhật

Đã bổ sung xử lý `language=ja` cho endpoint `/hello/{name}`. Khi truy cập `http://localhost:5081/hello/An?language=ja`, API trả về `{"message":"Konnichiwa, An!"}` như yêu cầu.

![Kiểm tra language=ja trên trình duyệt: Konnichiwa, An!](docs/images/basicwebapi-8-5-tieng-nhat.png)

### 8.6. Debug và quan sát dữ liệu

Đặt breakpoint tại dòng `var greeting = service.Create(request.Name.Trim());`, chạy API bằng debugger và gửi POST `/greetings` với dữ liệu `{"name":"Binh"}`. Chương trình dừng tại breakpoint. Sau khi nhấn F10, dòng tạo lời chào được thực hiện và chương trình dừng trước lệnh `return Results.Created(...)`.

Trong khung Autos, đối tượng `greeting` có `Id = 1`, `Name = "Binh"` và thời điểm tạo `CreatedAt`. Điều này cho thấy dữ liệu đầu vào đã được xử lý để tạo đối tượng kết quả trước khi API trả phản hồi. F10 dùng để chạy từng dòng, F5 để tiếp tục và Shift + F5 để dừng debug.

![Debug BasicWebApi: breakpoint, dòng thực thi và dữ liệu greeting trong Autos](docs/images/basicwebapi-8-6-debug.png)

---

## 4. MiniDistributedSystem (Bước 9)
### Kết quả chạy thực nghiệm:
Hệ thống gồm hai tiến trình độc lập chạy trên 2 terminal:
- **Terminal 1 (Máy chủ Submission.Api)**: Lắng nghe tại `http://localhost:5000`.
- **Terminal 2 (Máy khách Submission.Client)**: Kết nối tới API, lấy danh sách bài tập và gửi lượt nộp bài.
- Terminal 2 in ra:
  ```text
  Danh sach bai tap:
  - 1: Bai tap 1
  - 2: Bai tap 2
  Da nop bai. Ma luot nop: <GUID>
  Trang thai: received
  ```

### Trả lời 3 câu hỏi:
1. **Client gọi những endpoint nào của API? Mỗi lần gọi là một cặp request–response gồm method, URL, mã trạng thái gì?**
   - *Lần 1*: Method `GET`, URL `/api/assignments`, phản hồi mã `200 OK` kèm mảng danh sách bài tập.
   - *Lần 2*: Method `POST`, URL `/api/submissions`, phản hồi mã `201 Created` kèm header `Location: /api/submissions/{id}` và body thông tin bài nộp.
2. **Client có tham chiếu (dùng chung) code của API không? Hai bên "thống nhất" với nhau bằng gì?**
   - Client hoàn toàn **không tham chiếu** (không dùng chung project reference hay file dll) mã nguồn của API.
   - Hai bên thống nhất với nhau qua **Hợp đồng truyền thông** (Giao thức HTTP chuẩn) và **Hợp đồng dữ liệu** (Data Contract / DTO dạng JSON) được mỗi bên tự định nghĩa độc lập bằng các record C# tương ứng.
3. **Vì sao địa chỉ API được truyền qua tham số `-- http://localhost:5000` mà không viết cứng trong code?**
   - Tuân thủ nguyên lý liên kết lỏng (Loose Coupling) và 12-Factor App (tách biệt cấu hình khỏi mã nguồn). Giúp client có thể linh hoạt kết nối đến các môi trường khác nhau (Dev, Staging, Production) hoặc đổi IP/cổng mà không cần sửa code hay biên dịch lại.

---

## 5. Thí nghiệm lỗi (Bước 10)

| # | Thao tác | Dự đoán (TRƯỚC khi chạy) | Kết quả thực tế | Đúng/Sai | Giải thích nguyên nhân |
|---|---|---|---|---|---|
| **a** | Thêm bài tập thứ 3 vào mảng `assignments` trên API, chạy lại API rồi chạy Client | Client tự động hiển thị bài tập thứ 3 mà không cần sửa code Client | Client in ra đầy đủ cả 3 bài tập: `1: Bai tap 1`, `2: Bai tap 2`, `3: Bai tap 3` | **Đúng** | Client sử dụng `GetFromJsonAsync<List<Assignment>>()`, dữ liệu được phân tích động từ mảng JSON trả về. Việc thêm phần tử không làm thay đổi cấu trúc DTO nên client tự động đọc hết danh sách. |
| **b** | Tắt API (Ctrl+C), sau đó chạy Client | Client không kết nối được và báo lỗi mạng | Ném ngoại lệ `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5000)` | **Đúng** | Giao tiếp phân tán phụ thuộc vào đường truyền mạng và tính sẵn sàng của tiến trình server. Gọi nội bộ cùng tiến trình thì luôn có sẵn trong RAM, còn gọi qua mạng luôn có rủi ro máy chủ dừng hoạt động. |
| **c** | Đổi `StudentCode: ""` trên Client rồi chạy lại | API từ chối với mã lỗi 400, Client dừng do `EnsureSuccessStatusCode` | API trả về `400 Bad Request`. Client ném lỗi `HttpRequestException: Response status code does not indicate success: 400 (Bad Request)` | **Đúng** | `EnsureSuccessStatusCode()` tự ném ngoại lệ nếu mã HTTP &ge; 400. Máy chủ bắt buộc phải tự kiểm tra dữ liệu theo nguyên tắc Zero-Trust vì không thể tin tưởng hoàn toàn dữ liệu từ client gửi lên. |
| **d** | Đổi cổng API sang 5050 (`--urls http://localhost:5050`), Client vẫn gọi 5000 | Client báo lỗi chối từ kết nối tại cổng 5000 | Ném ngoại lệ `HttpRequestException` tại cổng 5000. Khắc phục: truyền lại tham số `dotnet run --project src/Submission.Client -- http://localhost:5050` thì chạy thành công | **Đúng** | Client thiết kế nhận URL từ `args[0]`, do đó có thể ghi đè địa chỉ kết nối linh hoạt lúc thực thi mà không cần sửa mã nguồn. |

---

## 6. Dự án EduSubmit tự làm (Bước 11-12)
### 6.1. Kiến trúc hệ thống
- Hai tiến trình độc lập hoàn toàn: `Submission.Api` (Máy chủ) và `Submission.Client` (Máy khách).
- Giao tiếp hoàn toàn qua chuẩn RESTful HTTP.

### 6.2. Triển khai bài mở rộng (Bước 12)
1. **Lưu trữ bài nộp tại API:** Khởi tạo danh sách `var submissions = new List<SubmissionResult>();` để lưu trữ trạng thái các lượt nộp bài trong bộ nhớ.
2. **Endpoint truy vấn theo mã định danh:**
   - Bổ sung `GET /api/submissions/{id:guid}`: Trả về `200 OK` nếu tìm thấy, hoặc `404 Not Found` nếu không tồn tại mã lượt nộp.
3. **Client xác minh:**
   - Sau khi gửi POST thành công, Client tự động gọi `GET /api/submissions/{result.Id}` để kiểm chứng lại thông tin đã lưu trên server.

### 6.3. Sơ đồ luồng trao đổi thông điệp (MEP)
```text
Client --[ HTTP GET  /api/assignments ]---------> Api : Request - Response (Lấy danh sách bài tập, trả về 200 OK)
Client --[ HTTP POST /api/submissions ]---------> Api : Request - Response (Gửi bài nộp, server lưu bộ nhớ và trả 201 Created + Header Location)
Client --[ HTTP GET  /api/submissions/{id} ]----> Api : Request - Response (Đọc lại thông tin lượt nộp, trả về 200 OK)
```

### 6.4. Tham khảo code mẫu
- Tự thiết kế và xây dựng toàn bộ Solution và logic API/Client theo đặc tả.
- Tham khảo code mẫu cách định nghĩa Record bất biến (Immutable Record) và cấu hình Header Location chuẩn RESTful.

---

## 7. Khó khăn gặp phải và cách giải quyết
1. **Xung đột cổng mạng (Address already in use / File is locked):**
   - *Nguyên nhân:* Tiến trình cũ vẫn đang chạy ngầm chiếm cổng `5081` hoặc `5000` hoặc khóa file thực thi exe.
   - *Cách giải quyết:* Dùng `Ctrl + C` hoặc lệnh `Stop-Process` trong PowerShell để tắt hoàn toàn tiến trình cũ trước khi chạy/build lại.
2. **Cấu hình cổng khi chạy Debug VS Code:**
   - *Nguyên nhân:* Khi chạy Debug bằng F5, ASP.NET Core sử dụng cổng mặc định `5001` (HTTPS) thay vì cổng `5081` truyền qua CLI.
   - *Cách giải quyết:* Đồng bộ địa chỉ `@api` trong file `requests.http` khớp với cổng debugger đang lắng nghe hoặc cấu hình trong `launchSettings.json`.
