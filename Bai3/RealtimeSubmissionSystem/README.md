# Bài 3: Giao tiếp thời gian thực với SignalR (EduSubmit)

## 1. Giải thích kiến trúc (3 câu hỏi đầu giờ)

### Câu 1: Tại sao cần máy chủ chủ động báo thay vì máy khách liên tục hỏi?

- **Vấn đề của việc hỏi liên tục (Polling):** Nếu máy khách phải liên tục gửi yêu cầu `GET` mỗi giây để kiểm tra xem bài đã chấm xong chưa, hệ thống sẽ lãng phí băng thông mạng, làm tăng tải CPU/RAM cho máy chủ bởi các yêu cầu rỗng (khi trạng thái chưa đổi) và vẫn tồn tại độ trễ giữa hai lần hỏi.
- **Lợi ích của máy chủ chủ động báo (Push qua SignalR):** Máy chủ giữ kết nối hai chiều nhẹ với máy khách. Ngay tại khoảnh khắc tác vụ chấm bài hoàn tất, máy chủ chủ động đẩy thông báo xuống ngay lập tức, giúp cập nhật tức thì (real-time) và tiết kiệm tài nguyên hệ thống.

### Câu 2: REST và SignalR chia việc với nhau như thế nào?

- **REST API (Kênh dữ liệu chuẩn & Nghiệp vụ chính):** Chịu trách nhiệm cung cấp danh sách bài tập (`GET /api/assignments`), tiếp nhận yêu cầu nộp bài (`POST /api/assignments/{id}/submissions`), và đóng vai trò là **nguồn dữ liệu chuẩn xác cuối cùng** khi máy khách gọi `GET /api/submissions/{id}` để chốt trạng thái.
- **SignalR (Kênh báo hiệu sự kiện tức thì):** Chỉ đảm nhận việc truyền tải thông điệp thông báo siêu nhẹ (`SubmissionStatusMessage` gồm 3 trường: `SubmissionId`, `Status`, `UpdatedAt`) tới đúng nhóm theo dõi (`submission:{id}`) để báo cho máy khách biết _sớm hơn_ khi trạng thái chuyển sang `processing` hoặc `graded`.

### Câu 3: Điều gì xảy ra nếu đứt mạng đúng lúc máy chủ báo `graded`?

- **Hiện tượng xảy ra:** SignalR truyền tin nhắn sự kiện theo thời gian thực và không lưu trữ hàng đợi tin nhắn cho các kết nối đang ngoại tuyến. Do đó, nếu máy khách bị đứt mạng đúng khoảnh khắc máy chủ gửi thông báo `graded`, máy khách sẽ **bị lỡ mất thông báo này**.
- **Cách hệ thống khắc phục:**
  1. Máy khách cấu hình `.WithAutomaticReconnect()` để tự động thiết lập lại kết nối mạng.
  2. Máy khách sử dụng cơ chế chờ có giới hạn thời gian (`Task.WhenAny` 10 giây) để không bị treo vô hạn.
  3. Sau khi kết thúc chờ (dù nhận được thông báo hay hết giờ), máy khách luôn áp dụng "quy tắc vàng": gọi lại **REST API (`GET /api/submissions/{id}`)** để đọc trực tiếp trạng thái `graded` mới nhất từ dữ liệu gốc của máy chủ.

---

## 2. Bảng 5 thí nghiệm kiểm chứng (Bước 7)

|  STT  | Thí nghiệm thực hiện                                                                                          | Dự đoán                                                                                                             | Kết quả thật                                                                                                                                          | Giải thích                                                                                                                                                                                                    |
| :---: | :------------------------------------------------------------------------------------------------------------ | :------------------------------------------------------------------------------------------------------------------ | :---------------------------------------------------------------------------------------------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **1** | **Hai máy khách cùng nộp bài đồng thời** (`SV001` và `SV002` chạy ở 2 cửa sổ Terminal khác nhau)              | Mỗi máy khách chỉ nhận thông báo trạng thái của riêng lượt nộp do mình tạo ra.                                      | Đúng như dự đoán: `SV001` chỉ nhận log của `SV001`, `SV002` chỉ nhận log của `SV002`.                                                                 | Máy khách chỉ gọi `WatchSubmission` với `created.Id` của riêng mình nên được xếp vào nhóm riêng `submission:{id}`; máy chủ dùng `Clients.Group(...)` nên không gửi nhầm sang nhóm khác.                       |
| **2** | **Nộp bài qua REST nhưng không gọi `WatchSubmission`** (bỏ qua bước tham gia nhóm trên Hub)                   | Máy khách không nhận được thông báo từ SignalR, chờ hết 10 giây và vẫn đọc được `graded` qua REST.                  | Sau 10 giây màn hình in `"Qua thoi gian cho thong bao graded."`, sau đó dòng cuối vẫn in `"Trang thai doc lai bang REST: graded"`.                    | Kết nối chưa được thêm vào nhóm `submission:{id}` trên Hub nên không nhận sự kiện push; tuy nhiên tác vụ nền trên server vẫn chấm xong nên REST API vẫn trả về đúng trạng thái cuối.                          |
| **3** | **Chạy máy khách khi máy chủ chưa bật** (tắt server ở Terminal 1 rồi chạy client)                             | Máy khách báo lỗi từ chối kết nối ngay khi cố gắng kết nối tới Hub/API.                                             | Chương trình dừng lại và ném ngoại lệ `HttpRequestException` (No connection could be made / Connection refused).                                      | Không có tiến trình nào lắng nghe tại cổng `5200` nên mọi yêu cầu kết nối SignalR và HTTP đều bị hệ điều hành từ chối ngay lập tức.                                                                           |
| **4** | **Tắt máy chủ giữa chừng** (nhấn `Ctrl + C` tắt server ngay khi client vừa nhận được trạng thái `processing`) | Máy khách cố gắng tự kết nối lại, không nhận được `graded`, hết 10s thì báo quá giờ và lỗi ở bước gọi REST cuối.    | `WithAutomaticReconnect()` kích hoạt thử nối lại nhưng thất bại; sau 10s in `"Qua thoi gian cho thong bao graded."` và lỗi kết nối ở lệnh `GET` cuối. | Tác vụ chấm bài giả lập chạy trong bộ nhớ RAM của máy chủ; khi tắt máy chủ, tiến trình nền bị hủy ngang nên trạng thái `graded` không bao giờ được tạo ra.                                                    |
| **5** | **Nộp nhiều bài liên tiếp trong thời gian ngắn** (gửi 3–5 request `POST` liên tục)                            | Máy chủ phản hồi `201 Created` ngay lập tức cho từng bài, không bị treo và chấm song song đúng cho tất cả lượt nộp. | Tất cả request đều nhận `201` tức thì và lần lượt nhận đủ 2 thông báo `processing` $\rightarrow$ `graded` với đúng `Id` tương ứng.                    | Tác vụ chấm bài chạy bất đồng bộ ở nền (`_ = SimulateGradingAsync(...)`) không chặn luồng nhận HTTP, đồng thời khóa `lock (submissionsLock)` đảm bảo cập nhật danh sách an toàn khi nhiều luồng ghi cùng lúc. |
