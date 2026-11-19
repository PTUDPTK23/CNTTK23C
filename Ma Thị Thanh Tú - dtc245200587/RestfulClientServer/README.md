Vì sao URI dùng danh tử, còn hành động do phương thức HTTP đảm nhận?

Trả lời: Vì URI đại diện cho tài nguyên (danh từ, ví dụ /assignments, /submissions), còn phương thức HTTP (GET, POST, PUT, DELETE) biểu diễn hành động thao tác lên tài nguyên đó.
"Stateless" thể hiện ở đâu trong code đã viết?

Trả lời: Thể hiện ở việc mỗi HTTP request từ client gửi lên server đều độc lập, chứa đủ thông tin (hoặc lấy từ DB/Memory) mà server không lưu trạng thái phiên làm việc (session) của client giữa các lần gọi.
Vì sao client và server mỗi bên khai báo DTO riêng mà vẫn khớp nhau?

Trả lời: Vì chúng tuân thủ chung một "hợp đồng" dữ liệu JSON (tên trường và kiểu dữ liệu) khi truyền tải qua mạng, giúp lỏng lẻo sự phụ thuộc mã nguồn giữa hai bên.