# Báo cáo thực hành Phần 3: EduSubmit (Lát cắt 2)

## 1. Thông tin sinh viên & Đề tài
- **Môn học:** Phát triển ứng dụng phân tán
- **Sinh viên:** Nguyễn Thị Quỳnh Thơ - MSV: dtc245340007
- **Chủ đề:** Phần 3 - Áp dụng sang hệ thống nộp bài trực tuyến **EduSubmit**
- **Solution:** `RestfulClientServer.slnx` (Cổng API: **5000**)

---

## 2. Thiết kế hợp đồng API (Theo khung 5 câu hỏi)

1. **Tài nguyên (Danh từ):**
   - Bài tập: `/api/assignments`
   - Lượt nộp (tài nguyên con): `/api/assignments/{assignmentId}/submissions`
   - Lượt nộp theo định danh duy nhất: `/api/submissions/{submissionId}`
2. **Phương thức HTTP (Động từ):**
   - `GET /api/assignments`: Đọc danh sách bài tập.
   - `GET /api/assignments/{assignmentId}`: Đọc chi tiết một bài tập.
   - `POST /api/assignments/{assignmentId}/submissions`: Nộp bài (tạo mới tài nguyên con).
   - `GET /api/submissions/{submissionId}`: Đọc lại lượt nộp (theo header Location).
3. **DTO (Hợp đồng dữ liệu):**
   - DTO đầu vào: `CreateSubmissionRequest(StudentCode, RepositoryUrl)` (không cho client gửi Id hay Status).
   - DTO đầu ra: `SubmissionDto(Id, AssignmentId, StudentCode, RepositoryUrl, Status, ReceivedAt)` (Id do server sinh bằng `Guid.NewGuid()`).
4. **Mã trạng thái & Kiểm tra lỗi:**
   - 3 cổng kiểm tra ở endpoint POST:
     - Bài tập có tồn tại không? → `404 ASSIGNMENT_NOT_FOUND`
     - Bài tập còn mở không? → `400 ASSIGNMENT_CLOSED`
     - Dữ liệu hợp lệ? → `400 INVALID_SUBMISSION`
     - Nộp trùng sinh viên cho cùng 1 bài? → `409 DUPLICATE_SUBMISSION`
   - Trả về mã lỗi thống nhất qua `ApiError(Code, Message)`.
5. **Kịch bản máy khách (4 nấc):**
   - Nấc 1: Lấy danh sách bài tập đang mở.
   - Nấc 2: POST nộp bài, kiểm tra `StatusCode == HttpStatusCode.Created` trước khi đọc body.
   - Nấc 3: Đọc lại chi tiết theo header `Location`.
   - Nấc 4: Cố ý gửi sai thông tin để bắt `400 Bad Request` và in `ApiError` mà không làm sập client.

---

## 3. Nhật ký chạy máy khách thực tế

```text
Danh sach bai tap dang mo:
 - 1: Bai tap REST API
 - 2: Bai tap Client

Da tao luot nop: ab6247e7-7a4e-47f2-ba82-c12836b3c06d
Trang thai ban dau: received
Doc lai tai header Location: /api/submissions/ab6247e7-7a4e-47f2-ba82-c12836b3c06d
Doc lai luot nop thanh cong: SV001 - https://github.com/dtc245340007-collab/EduSubmit

Thu gui yeu cau loi (thieu link repo):
Thu gui yeu cau loi: 400 BadRequest
INVALID_SUBMISSION: Thieu ma sinh vien hoac link repository.
```

---

## 4. Tệp kiểm thử `requests.http`

Tệp bao phủ đầy đủ 7 ca kiểm thử theo yêu cầu:
1. `GET /api/assignments` → 200 OK
2. `GET /api/assignments/1` → 200 OK
3. `GET /api/assignments/999` → 404 Not Found
4. `POST /api/assignments/1/submissions` → 201 Created + Header Location
5. `GET /api/submissions/{{id}}` → 200 OK (đọc theo Location)
6. `POST /api/assignments/1/submissions` (thiếu dữ liệu) → 400 Bad Request
7. `POST /api/assignments/3/submissions` (bài đã đóng) → 400 ASSIGNMENT_CLOSED
