# THƯ MỤC THỰC HÀNH BÀI 2

Thư mục này chứa đầy đủ toàn bộ mã nguồn thực hành theo tài liệu bài giảng môn **Phát triển ứng dụng phân tán** (TS. Dương Thu Mây):

1. [CatalogApi](./CatalogApi/)
   - Gồm máy chủ `Catalog.Api` (lắng nghe cổng 5100) và máy khách `Catalog.Client` (4 nấc).
   - Tệp kiểm thử [requests.http](./CatalogApi/requests.http) đầy đủ 8 ca test.
   - Các tệp dữ liệu kiểm thử: [sp-hop-le.json](./CatalogApi/sp-hop-le.json), [sp-sai.json](./CatalogApi/sp-sai.json).
   - Đầy đủ lịch sử Git commit theo từng bước.

2. [RestfulClientServer](./RestfulClientServer/) 
   - Gồm máy chủ `Submission.Api` (lắng nghe cổng 5000) và máy khách `Submission.Client` (4 nấc).
   - Tệp kiểm thử [requests.http](./RestfulClientServer/requests.http) đầy đủ bao phủ các mã trạng thái: 200, 201, 400, 404.
   - Báo cáo chi tiết [README.md](./RestfulClientServer/README.md) trả lời đầy đủ 3 câu hỏi lý thuyết và bảng phá thử 10 ca kiểm tra.
   - Đầy đủ lịch sử Git commit theo từng bước chuẩn chỉnh.

