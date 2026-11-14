# &#32;Bài 2 – EduSubmit

Họ tên: Đinh Bách Hợp · MSSV: dtc245201006

## 1. Cấu trúc

- `src/Submission.Api`: máy chủ, cổng 5000
- `src/Submission.Client`: máy khách console
- `requests.http`: 7 request test (200, 201, 400, 404)
- `sv001.json`, `rong.json`: body mẫu cho các ca thử

## 2. Lệnh đã chạy

```powershell
dotnet new sln -n RestfulClientServer
dotnet new web -n Submission.Api -o src/Submission.Api
dotnet new console -n Submission.Client -o src/Submission.Client
dotnet sln add src/Submission.Api src/Submission.Client
dotnet new gitignore
dotnet build
dotnet run --project src/Submission.Api --urls http://localhost:5000
dotnet run --project src/Submission.Client -- http://localhost:5000
curl.exe -i http://localhost:5000/api/assignments/999
curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions -H "Content-Type: application/json" -d "@sv001.json"
curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions -H "Content-Type: application/json" -d "@rong.json"
```

## 3. Ảnh kết quả

- Client in đủ 4 nấc: `../picture/client-4-nac.png`
- POST nộp bài trả 201 Created: `../picture/requests-201.png`

## 4. Bảng phá thử

| Ca | Yêu cầu | Kết quả thật | Ai trả |
| --- | --- | --- | --- |
| 1 | POST bài 1, sv001.json | 201 + Location | Handler |
| 2 | POST bài 1, rong.json | 400 INVALID\_SUBMISSION | Handler |
| 3 | POST bài 999 | 404 ASSIGNMENT\_NOT\_FOUND | Handler |
| 4 | Bỏ Content-Type | 415, body rỗng | Framework |
| 5 | JSON hỏng | 400, body văn bản | Framework |
| 6 | Thiếu repositoryUrl | 400 INVALID\_SUBMISSION | Handler |
| 7 | PUT /api/assignments/1 | 405, Allow: GET | Framework |
| 8 | POST vào bài đã đóng (đặt IsOpen = false) | 400 ASSIGNMENT\_CLOSED | Handler |
| 9 | GET lượt nộp theo Location | 200 | Handler |
| 10 | GET guid không tồn tại | 404 SUBMISSION\_NOT\_FOUND | Handler |
| 11 | Tắt rồi chạy lại máy chủ, GET id cũ | 404 (dữ liệu trong RAM bị mất) | Handler |
| 12 | Tắt máy chủ, chạy Client | Connection refused | Mạng |
| 13 | Chạy Client với cổng sai 5999 | Connection refused | Mạng |
| 14 | Thêm một bài tập mới vào dữ liệu mẫu của API | Client in thêm dòng mới, không sửa code Client | – |

## 5. Trả lời 3 câu hỏi

1. **Vì sao URI dùng danh từ, còn hành động do phương thức HTTP đảm nhận?** URI chỉ tên tài nguyên (`assignments`, `submissions`), còn việc cần làm do phương thức HTTP quyết định: GET để đọc, POST để tạo. Trong bài, `GET /api/assignments` đọc danh sách bài tập và `POST /api/assignments/1/submissions` tạo lượt nộp cho bài tập 1. Cùng URI `/api/assignments/1`, gọi bằng GET thì đọc, gọi bằng PUT thì bị từ chối 405 (ca 7). Nhờ vậy không cần đặt tên động từ như `/submitAssignment`.
2. **"Stateless" thể hiện ở đâu trong code?** Mỗi yêu cầu tự mang đủ thông tin để máy chủ xử lý, không dựa vào lần gọi trước. Khi nộp bài, id bài tập nằm trên URI (`/api/assignments/1/submissions`), còn `studentCode` và `repositoryUrl` nằm trong body JSON. Khi đọc lại, máy khách chỉ cần địa chỉ trong header `Location`. Máy chủ không nhớ phiên của máy khách giữa hai lần gọi. Dữ liệu lượt nộp mất khi tắt máy chủ (ca 11) vì nó nằm trong bộ nhớ, đó là dữ liệu của tài nguyên chứ không phải trạng thái phiên.
3. **Vì sao client và server khai báo DTO riêng mà vẫn khớp nhau?** DTO là hợp đồng JSON giữa hai bên, không phải lớp dùng chung. `Submission.Api` và `Submission.Client` là hai project độc lập, mỗi bên tự khai báo DTO trong code của mình. Hai bên khớp nhau nhờ tên trường và kiểu dữ liệu trong JSON (`studentCode`, `repositoryUrl`, `id`, `status`, và `code`, `message` của `ApiError`), không nhờ trùng tên lớp. Vì vậy mỗi bên sửa code nội bộ mà không ảnh hưởng bên kia, miễn JSON vẫn đúng hợp đồng.
