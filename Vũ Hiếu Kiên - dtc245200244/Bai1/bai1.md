# Bài 1 – Tổng quan hệ thống và các mô hình trao đổi thông điệp (MEP)

- **Họ tên:** Vũ Hiếu Kiên
- **MSSV:** `dtc245200244`
- **Lớp:** CNTT K23C
- **Học phần:** Phát triển ứng dụng phân tán

---

## 1. Cài đặt môi trường (Bước 1–4)

- **Phiên bản .NET SDK:** `.NET 10.0.201`
- **Công cụ:** Visual Studio Code (đã cài đặt `C# Dev Kit`, `C#`, `.NET Install Tool`, `REST Client`) & Git.
- **Kết quả kiểm tra môi trường:**
  ```text
  PS> dotnet --version
  10.0.201
  PS> dotnet run
  Hello, World!
  ```

---

## 2. MepDemo – 5 mô hình trao đổi thông điệp (Bước 7)

### Mô tả ngắn gọn 5 mô hình trao đổi thông điệp (MEP):
1. **Request–Response (Yêu cầu – Phản hồi đồng bộ):** Bên gửi gửi yêu cầu và đứng chờ đến khi nhận được phản hồi trực tiếp từ bên nhận (ví dụ: gọi REST API lấy thông tin tài khoản).
2. **Fire-and-Forget / One-way (Một chiều):** Bên gửi phát thông điệp đi và tiếp tục công việc ngay mà không chờ kết quả xử lý (ví dụ: ghi log sự kiện, gửi telemetry).
3. **Message Queue (Hàng đợi thông điệp – Point-to-Point):** Bên gửi đẩy tác vụ vào hàng đợi trung gian (`Channel`/`Queue`), một worker đọc ra và xử lý bất đồng bộ (ví dụ: hàng đợi chấm bài tự động trong EduSubmit).
4. **Publish–Subscribe (Xuất bản – Đăng ký):** Bên phát (Publisher) công bố một sự kiện một lần, tất cả các bên đăng ký quan tâm (Subscribers) đều đồng thời nhận được bản sao sự kiện đó (ví dụ: khi bài nộp chấm xong thì vừa lưu bảng điểm, vừa gửi email thông báo).
5. **Streaming (Luồng dữ liệu liên tục):** Dữ liệu được truyền từng phần tử theo thời gian thực qua `IAsyncEnumerable<T>` ngay khi sinh ra thay vì đợi gom thành một gói lớn (ví dụ: truyền log tiến độ chấm từng test case).

### Trả lời 3 câu hỏi Bước 7:
1. **Sự khác nhau giữa Request–Response và Queue:** Trong Request–Response, Client bị khóa (chờ đồng bộ) cho đến khi Server xử lý xong; còn với Queue, bên gửi chỉ cần đưa thông điệp vào hàng đợi là xong ngay, Worker sẽ lấy ra xử lý sau giúp chịu tải cao và tách rời thời gian sống của hai tiến trình.
2. **Sự khác nhau giữa Queue và Publish–Subscribe:** Với Queue, mỗi thông điệp chỉ được xử lý bởi **một** người nhận (Point-to-Point); còn với Publish–Subscribe, một sự kiện phát ra được gửi tới **tất cả** các bên đã đăng ký (One-to-Many).
3. **Khi nào dùng Streaming thay cho Request–Response:** Khi kết quả trả về có kích thước lớn hoặc sinh ra dần theo thời gian (như kết quả chấm từng test case, luồng video/audio, tiến trình AI sinh văn bản), giúp bên nhận hiển thị ngay từng phần mà không phải đợi toàn bộ tác vụ hoàn tất.

---

## 3. BasicWebApi – Dịch vụ HTTP đầu tiên (Bước 8)

### Kết quả thực thi trên Terminal 2 (PowerShell):
```powershell
PS> curl.exe http://localhost:5081/
{"service":"BasicWebApi","status":"running","now":"2026-11-12T09:32:54Z"}

PS> curl.exe "http://localhost:5081/hello/An?language=en"
{"message":"Hello, An!","language":"en"}

PS> irm -Method Post http://localhost:5081/greetings -ContentType application/json -Body '{"name":"Binh"}'
message
-------
Xin chao Binh, chuc ban hoc tot!

PS> irm -Method Post http://localhost:5081/greetings -ContentType application/json -Body '{"name":""}'
# Trả về HTTP 400 Bad Request: {"error":"Truong 'name' khong duoc de trong"}
```

### Trả lời 3 câu hỏi mục 8.4:
1. **Dữ liệu đi vào API bằng những con đường nào?** Qua 3 con đường chính: **Route parameter** (`/hello/{name}`), **Query string** (`?language=en`), và **Request Body JSON** (`POST /greetings` với `GreetingRequest`).
2. **Tại sao `POST /greetings` với tên hợp lệ trả về `201 Created`, còn tên rỗng trả về `400 Bad Request`?** Vì `201 Created` báo hiệu yêu cầu hợp lệ và máy chủ đã khởi tạo thành công kết quả mới; còn khi `name` rỗng là lỗi dữ liệu đầu vào từ phía máy khách nên máy chủ từ chối xử lý ở biên hệ thống với mã `400 Bad Request`.
3. **Vì sao trong PowerShell phải gõ `curl.exe` thay vì `curl`?** Vì trong Windows PowerShell 5.1, `curl` mặc định là bí danh (alias) của lệnh `Invoke-WebRequest` có cú pháp tham số khác với công cụ `curl.exe` chuẩn của hệ điều hành.

---

## 4. MiniDistributedSystem – Hai tiến trình giao tiếp qua mạng (Bước 9)

### Kết quả chạy 2 tiến trình độc lập:
- **Terminal 1 (`Submission.Api` tại cổng 5000):**
  ```text
  info: Microsoft.Hosting.Lifetime[14]
        Now listening on: http://localhost:5000
  info: Microsoft.Hosting.Lifetime[0]
        Application started. Press Ctrl+C to shut down.
  ```
- **Terminal 2 (`Submission.Client`):**
  ```text
  Danh sach bai tap:
  - 1: Bai tap 1 (Tao API dau tien)
  - 2: Bai tap 2 (Goi API tu client)
  - 3: Bai tap 3 (Mo rong tra cuu luot nop theo Id)
  Da nop bai. Ma luot nop: 6a2d8d71-54e9-4d30-9f18-7d1e42a10c92
  Trang thai: received
  Tra cuu lai luot nop: MSSV=dtc245200244, BaiTap=1, TrangThai=received
  ```

### Trả lời 3 câu hỏi Bước 9:
1. **Tại sao `Submission.Client` và `Submission.Api` phải khai báo lại các `record` DTO?** Vì hai chương trình chạy ở hai tiến trình độc lập, không chia sẻ bộ nhớ và không tham chiếu mã nguồn trực tiếp của nhau; chúng chỉ thống nhất "hợp đồng dữ liệu" qua định dạng chuỗi JSON truyền trên mạng.
2. **Nếu đổi cổng của API mà không đổi tham số chạy Client thì điều gì xảy ra?** Client vẫn gửi yêu cầu tới cổng cũ (`localhost:5000`), nơi không có tiến trình nào lắng nghe, dẫn đến lỗi kết nối mạng (`HttpRequestException: No connection could be made because the target machine actively refused it`).
3. **Header `Location` trong phản hồi `201 Created` dùng để làm gì?** Cung cấp đường dẫn URI định danh của lượt nộp vừa được tạo (`/api/submissions/{id}`), giúp Client biết chính xác địa chỉ để tra cứu lại trạng thái của lượt nộp đó.

---

## 5. Thí nghiệm lỗi (Bước 10)

| # | Kịch bản thí nghiệm | Dự đoán (ghi trước khi chạy) | Kết quả thực tế (mã lỗi / thông báo) | Đúng/Sai | Giải thích nguyên nhân |
| :-: | :--- | :--- | :--- | :-: | :--- |
| a | Tắt `Submission.Api` (Ctrl+C) rồi chạy `Submission.Client` | Chương trình Client báo lỗi không kết nối được tới máy chủ cổng 5000 | `Unhandled exception. System.Net.Http.HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5000)` | Đúng | Máy chủ đã dừng tiến trình nên không còn socket lắng nghe ở cổng 5000; hệ điều hành từ chối kết nối TCP ngay ở tầng mạng. |
| b | Chạy `Submission.Client` với cổng sai (`http://localhost:5999`) | Client ném ngoại lệ `HttpRequestException` từ chối kết nối ở cổng 5999 | `HttpRequestException: No connection could be made because the target machine actively refused it. (localhost:5999)` | Đúng | Trong hệ phân tán, hai thành phần phải thống nhất địa chỉ (URL/cổng). Gọi sai cổng dẫn tới lỗi kết nối mạng tương tự khi máy chủ tắt. |
| c | Gửi `POST /api/submissions` với `studentCode: ""` (rỗng) | Máy chủ từ chối và trả về mã trạng thái `400 Bad Request` | `HTTP/1.1 400 Bad Request` kèm body `{"message":"Du lieu nop bai khong hop le"}` | Đúng | Khối kiểm tra `string.IsNullOrWhiteSpace(request.StudentCode)` tại biên API phát hiện dữ liệu thiếu nên chủ động trả `Results.BadRequest(...)`. |
| d | Gửi `POST /api/submissions` nhưng bỏ header `Content-Type: application/json` | Máy chủ trả về lỗi không hỗ trợ kiểu dữ liệu body (`415`) | `HTTP/1.1 415 Unsupported Media Type` | Đúng | ASP.NET Core Minimal API yêu cầu header `Content-Type: application/json` để biết cần giải tuần tự (deserialize) body JSON thành đối tượng `CreateSubmissionRequest`. |
| e | Gửi `GET /api/submissions/00000000-0000-0000-0000-000000000000` | Máy chủ trả về mã `404 Not Found` | `HTTP/1.1 404 Not Found` kèm body `{"message":"Khong tim thay luot nop"}` | Đúng | Mã GUID đúng khuôn dạng nhưng không khớp với phần tử nào trong danh sách `submissions` trên bộ nhớ máy chủ. |

---

## 6. Dự án EduSubmit tự làm và Bài mở rộng (Bước 11–12)

- **Các phần đã thực hiện:**
  1. Bổ sung bài tập mẫu thứ 3 vào danh sách `assignments`.
  2. Lưu danh sách lượt nộp trong bộ nhớ bằng `var submissions = new List<SubmissionResult>();`.
  3. Hoàn thành endpoint mở rộng `GET /api/submissions/{id:guid}` trả `200 OK` khi tìm thấy và `404 Not Found` khi không tồn tại.
  4. Cập nhật `Submission.Client` tự động gọi lại `GET /api/submissions/{result.Id}` sau khi nộp bài thành công để kiểm chứng dữ liệu đã lưu trên Server.
- **Sơ đồ luồng thông điệp:**
  ```text
  Client --(1. HTTP GET /api/assignments)--------------> Api : 200 OK + Danh sách 3 bài tập (JSON)
  Client --(2. HTTP POST /api/submissions + JSON)------> Api : 201 Created + Location + SubmissionResult
  Client --(3. HTTP GET /api/submissions/{id})---------> Api : 200 OK + Chi tiết lượt nộp vừa tạo
  ```

---

## 7. Khó khăn gặp phải và Câu hỏi tự kiểm tra (Phụ lục 4)

### Khó khăn & Cách giải quyết:
- **Lệnh `curl` trong PowerShell báo lỗi:** Do PowerShell ánh xạ `curl` vào `Invoke-WebRequest`. Giải quyết bằng cách gọi rõ `curl.exe` hoặc sử dụng tệp `requests.http` với extension REST Client.
- **Dữ liệu lượt nộp mất khi khởi động lại API:** Do danh sách `submissions` đang lưu trên RAM (`List<SubmissionResult>`). Khi tắt tiến trình `Submission.Api`, bộ nhớ của tiến trình được giải phóng.

### Trả lời câu hỏi tự kiểm tra (Phụ lục 4):
1. **Ứng dụng phân tán khác ứng dụng nguyên khối (monolithic) ở điểm nào? Ranh giới nằm ở đâu?**
   - Ứng dụng nguyên khối chạy trong một tiến trình duy nhất, các thành phần gọi hàm trực tiếp qua bộ nhớ chung. Ứng dụng phân tán gồm nhiều tiến trình độc lập (có thể trên nhiều máy khác nhau) phối hợp với nhau hoàn toàn qua thông điệp mạng. Trong Bài 1, ranh giới nằm ở giao thức **HTTP/JSON qua mạng TCP/IP** giữa hai tiến trình `Submission.Client` và `Submission.Api`.
2. **Kể tên 5 mô hình trao đổi thông điệp (MEP). EduSubmit ở Bài 1 dùng mô hình nào?**
   - 5 MEP: (1) Request–Response, (2) Fire-and-Forget (One-way), (3) Message Queue (Point-to-Point), (4) Publish–Subscribe, (5) Streaming.
   - EduSubmit ở Bài 1 sử dụng mô hình **Request–Response đồng bộ qua HTTP**.
3. **Phân biệt mô hình hàng đợi (Queue) và Publish–Subscribe:**
   - **Hàng đợi (Queue):** Quan hệ 1–1 (một thông điệp đưa vào hàng đợi chỉ được lấy ra và xử lý bởi duy nhất một worker). Ví dụ: hàng đợi chấm điểm bài nộp sinh viên.
   - **Publish–Subscribe:** Quan hệ 1–nhiều (một thông điệp sự kiện phát ra được phân phối tới tất cả các dịch vụ đăng ký nhận). Ví dụ: sự kiện "Đã chấm xong" đồng thời kích hoạt dịch vụ gửi email và dịch vụ cập nhật bảng xếp hạng.
4. **Mã 200, 201, 400, 404 có ý nghĩa gì? Khi nào API nên trả mỗi mã?**
   - `200 OK`: Yêu cầu xử lý thành công, trả kèm dữ liệu (dùng cho `GET` lấy danh sách hoặc chi tiết).
   - `201 Created`: Đã khởi tạo thành công một tài nguyên mới trên máy chủ (dùng cho `POST` tạo lượt nộp mới, kèm header `Location`).
   - `400 Bad Request`: Yêu cầu không hợp lệ do lỗi dữ liệu từ phía máy khách (thiếu mã sinh viên, thiếu URL repo, sai định dạng).
   - `404 Not Found`: Không tìm thấy tài nguyên theo định danh yêu cầu (mã bài tập hoặc mã lượt nộp GUID không tồn tại).
5. **Vì sao máy khách luôn phải kiểm tra mã trạng thái trước khi đọc dữ liệu phản hồi?**
   - Vì qua mạng yêu cầu có thể thất bại (`4xx`, `5xx`). Khi thành công, body JSON mang cấu trúc dữ liệu kết quả (`SubmissionResult`), nhưng khi thất bại, body mang thông báo lỗi hoặc rỗng. Kiểm tra mã trạng thái trước giúp máy khách xử lý lỗi an toàn, tránh giải tuần tự sai kiểu dữ liệu hoặc làm sập chương trình.
