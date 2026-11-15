# Báo cáo Thực hành Bài 2: RESTful API - EduSubmit

- **Họ và tên:** Phan Ngô Huy Hoàng
- **Mã sinh viên:** `dtc245200477`
- **Lớp:** CNTT K23C
- **Học phần:** Phát triển ứng dụng phân tán

---

## 1. Nhật ký thực hiện & Lệnh đã chạy

### 1.1. Cấu trúc thư mục & giải pháp
- Solution: `RestfulClientServer.slnx`
- Máy chủ API: `src/Submission.Api` (lắng nghe tại cổng cố định 5000)
- Máy khách: `src/Submission.Client` (gọi HTTP tới máy chủ)

### 1.2. Khởi chạy hệ thống hai tiến trình
- **Khởi động máy chủ API (Terminal 1):**
  ```powershell
  dotnet run --project src/Submission.Api --urls http://localhost:5000
  ```
- **Khởi động máy khách Client (Terminal 2):**
  ```powershell
  dotnet run --project src/Submission.Client -- http://localhost:5000
  ```

### 1.3. Kết quả 4 nấc của máy khách (Submission.Client)
```text
Danh sach bai tap dang mo:
- 1: Bai tap REST API
- 2: Bai tap Client

Da tao luot nop: 2f7e8a91-4c62-421f-80d1-12501a3dc8f2
Trang thai ban dau: received
Doc lai luot nop: SV001 - https://example.com/sv001/rest-api
Doc lai thanh cong: [2f7e8a91-4c62-421f-80d1-12501a3dc8f2] Ma SV: SV001

Thu gui yeu cau loi: 400 BadRequest
INVALID_SUBMISSION: Thieu ma sinh vien hoac kho ma nguon.
```

---

## 2. Bảng phá thử (Thí nghiệm phân tán)

| STT | Kịch bản thử nghiệm | Lệnh thực hiện | Kết quả quan sát | Bài học rút ra |
|:---:|:---|:---|:---|:---|
| 1 | **Chạy Client khi Server tắt** | Tắt Terminal 1 (`Ctrl+C`), chạy Client ở Terminal 2 | Báo lỗi `HttpRequestException: Connection refused (localhost:5000)` | Lỗi đặc thù hệ phân tán: tiến trình phía bên kia có thể vắng mặt / gặp sự cố bất cứ lúc nào. |
| 2 | **Sai cổng kết nối** | `dotnet run --project src/Submission.Client -- http://localhost:5999` | Báo lỗi `Connection refused (localhost:5999)` | Hai bên bắt buộc phải thống nhất địa chỉ cổng dịch vụ (URL) thông qua cấu hình. |
| 3 | **Khởi động lại Server** | `Ctrl+C` server rồi bật lại, đọc lại lượt nộp cũ | Trả về mã `404 SUBMISSION_NOT_FOUND` | Dữ liệu lưu trong RAM sẽ mất khi tiến trình máy chủ khởi động lại; cần CSDL bền vững. |
| 4 | **Client gửi dữ liệu sai** | POST với `StudentCode = ""` và `RepositoryUrl = ""` | Nhận mã `400 BadRequest`, đọc được mã lỗi `INVALID_SUBMISSION`, Client không sập | Client cần kiểm tra mã trạng thái trước khi đọc body để ứng biến linh hoạt. |

---

## 3. Trả lời ba câu hỏi giải thích

### Câu 1: Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?
- **Trả lời:** Trong kiến trúc RESTful, URI đại diện cho **tài nguyên** (danh từ, ví dụ: `/api/assignments`, `/api/submissions`), là đối tượng cần thao tác. 
- Các thao tác hay hành động nghiệp vụ đã được chuẩn hóa bởi các **phương thức HTTP (HTTP Methods)**:
  - `GET`: Đọc tài nguyên.
  - `POST`: Tạo mới tài nguyên con.
  - `PUT`/`PATCH`: Cập nhật tài nguyên.
  - `DELETE`: Xóa tài nguyên.
- Việc tách biệt này giúp API có hợp đồng rõ ràng, nhất quán, dễ mở rộng và dễ đoán cách dùng thay vì đặt tên động từ vào URI (như `/submitAssignment` hay `/doSubmit`).

### Câu 2: "Stateless" thể hiện ở đâu trong code đã viết?
- **Trả lời:** Tính chất Stateless (phi trạng thái) thể hiện ở chỗ: **Mỗi yêu cầu HTTP từ máy khách gửi lên đều tự mang đầy đủ thông tin độc lập** để máy chủ có thể hiểu và xử lý ngay mà không phụ thuộc vào bộ nhớ phiên (session) trước đó.
- Cụ thể trong code:
  - Mã bài tập `assignmentId` được truyền rõ ràng trên route URI: `/api/assignments/{assignmentId}/submissions`.
  - Thông tin sinh viên (`StudentCode`) và đường dẫn kho (`RepositoryUrl`) được gửi kèm đầy đủ trong body JSON của mỗi request.
  - Máy chủ không lưu session hay ghi nhớ ngữ cảnh hội thoại của client; nếu có nhiều máy chủ đặt sau bộ cân bằng tải (Load Balancer), bất kỳ máy chủ nào nhận được request cũng đều xử lý được ngay.

### Câu 3: Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?
- **Trả lời:** DTO (Data Transfer Object) đóng vai trò là **hợp đồng trao đổi dữ liệu qua mạng bằng định dạng chuẩn JSON**, không phải là class dùng chung (shared binary class).
- Hai tiến trình Client và Server là hai tiến trình độc lập, không tham chiếu project của nhau. Hai bên khớp được dữ liệu nhờ quy ước đặt tên thuộc tính và kiểu dữ liệu tương thích trong chuỗi JSON tuần hoàn:
  - Client serialize DTO thành chuỗi JSON gửi đi qua mạng.
  - Server deserialize chuỗi JSON đó thành DTO tương ứng của Server dựa theo tên trường (khớp case-insensitive hoặc camelCase).
  - Tách riêng DTO còn giúp bảo mật (máy khách không thể tự ý điền các trường nội bộ như `Id`, `Status` hay điểm số).
