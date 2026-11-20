# Yêu cầu hệ thống — Aurelia Online Auction

**Phiên bản:** 1.0  
**Cập nhật lần cuối:** 2026-11-19  
**Trạng thái:** Đã triển khai và xác minh

## 1. Mục đích

Aurelia là một nền tảng đấu giá trực tuyến dạng sản phẩm thực tế. Người đặt giá duyệt qua các lô được chọn lọc và đặt giá theo thời gian thực; quản trị viên quản lý danh mục, phiên đấu giá, người dùng và lịch sử kiểm toán. Dự án minh họa một kiến trúc .NET phân tán thực tế: REST API, dịch vụ xác thực giá gRPC độc lập, PostgreSQL với giao dịch rõ ràng và khóa dòng, phân phối sự kiện qua RabbitMQ bằng transactional outbox, cơ chế fan-out thời gian thực SignalR và một worker kiểm toán riêng biệt.

## 2. Tác nhân

| Tác nhân | Mô tả | Khả năng |
| --- | --- | --- |
| Khách truy cập | Người dùng chưa xác thực | Xem trang chủ, danh sách đấu giá, chi tiết lô, lịch sử đặt giá và kết quả |
| Người đấu giá | Người dùng đã xác thực với vai trò `Bidder` | Mọi thao tác của khách truy cập, cộng thêm đăng ký, đăng nhập, đặt giá, xem hồ sơ, lượt đặt giá, phòng và thành tích thắng |
| Quản trị viên | Người dùng đã xác thực với vai trò `Admin` | Quản lý sản phẩm và phiên đấu giá, kết thúc phiên, quản lý người dùng, xem bảng điều khiển và nhật ký kiểm toán |
| Hub Consumer (hệ thống) | Dịch vụ nền | Tiêu thụ sự kiện RabbitMQ và chuyển tiếp tới SignalR hub của API |
| Audit Worker (hệ thống) | Dịch vụ nền | Ghi lại bản ghi kiểm toán bất biến cho mỗi sự kiện được chấp nhận |

## 3. Yêu cầu chức năng

### 3.1 Xác thực và tài khoản

- **FR-AUTH-1** Khách truy cập có thể đăng ký bằng email, tên hiển thị và mật khẩu (tối thiểu 8 ký tự).
- **FR-AUTH-2** Mật khẩu được lưu dưới dạng băm BCrypt; mật khẩu dạng rõ ràng không bao giờ được lưu trữ hoặc ghi log.
- **FR-AUTH-3** Đăng nhập trả về JWT đã ký (HS256, hết hạn sau 8 giờ) chứa mã người dùng, email, tên hiển thị và vai trò.
- **FR-AUTH-4** Các endpoint yêu cầu xác thực từ chối token thiếu, hết hạn hoặc bị chỉnh sửa với mã `401`.
- **FR-AUTH-5** Các endpoint bảo vệ theo vai trò trả về `403` cho vai trò không phù hợp.
- **FR-AUTH-6** Người đấu giá có thể xem hồ sơ với các số liệu tổng hợp cùng lịch sử đặt giá, phòng và thành tích thắng.

### 3.2 Danh mục và phiên đấu giá

- **FR-CAT-1** Bất kỳ ai cũng có thể liệt kê và tìm kiếm sản phẩm, phiên đấu giá, lọc theo trạng thái/danh mục và sắp xếp kết quả.
- **FR-CAT-2** Bất kỳ ai cũng có thể mở chi tiết phiên với thông tin sản phẩm, giá hiện tại, giá hợp lệ kế tiếp, mốc thời gian đếm ngược, người tham gia, các lượt đặt giá gần đây và người thắng/kết quả.
- **FR-CAT-3** Quản trị viên có thể tạo, cập nhật, xóa sản phẩm và tạo phiên đấu giá.
- **FR-CAT-4** Sản phẩm đã được phiên đấu giá tham chiếu không thể xóa (`409`).
- **FR-CAT-5** Phiên đấu giá có trạng thái `Scheduled`, `Active`, `Ended` hoặc `Cancelled`; bộ lập lịch tự động kích hoạt và đóng phiên.
- **FR-CAT-6** Việc đóng phiên chọn lượt giá cao nhất (đồng hòa giải quyết bằng số thứ tự nhỏ nhất), đánh dấu lượt thắng, ghi nhận người thắng và phát ra `AuctionEnded`.
- **FR-CAT-7** Quản trị viên có thể kết thúc ngay một phiên đấu giá.

### 3.3 Đặt giá

- **FR-BID-1** Chỉ người đấu giá đã xác thực mới được đặt giá.
- **FR-BID-2** Mỗi lượt giá được xác thực bởi dịch vụ gRPC Bid Validation độc lập trước khi API ghi dữ liệu.
- **FR-BID-3** API kiểm tra lại lượt giá trong giao dịch ghi bằng cùng một bộ quy tắc domain.
- **FR-BID-4** Lượt giá chỉ được chấp nhận khi phiên còn hoạt động, còn trong thời gian mở, người đặt giá còn hoạt động, không phải quản trị viên hay người tạo phiên, và số tiền cao hơn giá hiện tại ít nhất bằng bước giá tối thiểu.
- **FR-BID-5** Số tiền được làm tròn đến hai chữ số thập phân; số lẻ thập phân không hợp lệ bị từ chối.
- **FR-BID-6** Các lượt giá đồng thời được tuần tự hóa bằng `SELECT ... FOR UPDATE` trên dòng phiên đấu giá. Tối đa một trong hai lượt cùng thời điểm ở cùng mức giá có thể thành công.
- **FR-BID-7** Một lượt giá được chấp nhận cập nhật giá/phiên bản của phiên, chèn lượt giá và bản aggregate người tham gia, đồng thời chèn thông điệp outbox `BidAccepted` trong **một** giao dịch cơ sở dữ liệu.
- **FR-BID-8** Lịch sử đặt giá công khai và phân trang, mới nhất trước.
- **FR-BID-9** Mọi lượt giá được chấp nhận đều hiển thị với các client đã kết nối qua SignalR mà không cần polling.

### 3.4 Thời gian thực

- **FR-RT-1** Client kết nối tới `/hubs/auction` bằng JWT.
- **FR-RT-2** Hub cung cấp các nhóm theo phiên đấu giá; client tham gia một phòng cụ thể.
- **FR-RT-3** Sự kiện `BidAccepted` và `AuctionEnded` được phát tới nhóm của phiên.
- **FR-RT-4** Trình duyệt cập nhật giá hiện tại, giá hợp lệ kế tiếp, luồng lượt giá và đồng hồ đếm ngược từ thông điệp SignalR.
- **FR-RT-5** SignalR là cơ chế cập nhật trực tiếp duy nhất; giao diện không dùng polling.

### 3.5 Nhắn tin và kiểm toán

- **FR-MSG-1** Lượt giá được chấp nhận và phiên kết thúc được publish tới RabbitMQ với giao hàng ít nhất một lần.
- **FR-MSG-2** Việc publish dùng mẫu transactional outbox để sự cố broker không làm mất sự kiện đã chấp nhận.
- **FR-MSG-3** Hai queue bền vững tiêu thụ sự kiện: `auction.hub` và `auction.audit`.
- **FR-MSG-4** Consumer xác nhận thủ công, thử lại tối đa ba lần và đưa thông điệp vào dead-letter khi cạn kiệt lần thử.
- **FR-MSG-5** Consumer loại trùng theo event id, nên việc giao lại không tạo bản ghi kiểm toán hay broadcast trùng lặp.
- **FR-MSG-6** Audit Worker ghi mọi sự kiện vào `audit_logs`; quản trị viên có thể phân trang và lọc nhật ký.

### 3.6 Quản trị

- **FR-ADM-1** Bảng điều khiển hiển thị tổng số, khối lượng lượt giá 14 ngày, phân bố trạng thái phiên, phiên đấu giá top và lượt giá gần đây.
- **FR-ADM-2** Quản trị viên có thể tìm người dùng, đổi vai trò hoặc trạng thái hoạt động.
- **FR-ADM-3** Quản trị viên không thể gỡ quyền quản trị của chính mình hay khóa tài khoản của mình.
- **FR-ADM-4** Bảng điều khiển quản trị gồm các trang: bảng điều khiển, sản phẩm, phiên đấu giá, người dùng và kiểm toán.

## 4. Yêu cầu phi chức năng

| ID | Yêu cầu |
| --- | --- |
| NFR-1 | **Đúng đắn dưới đồng thời** — không có hai lượt giá nào được chấp nhận cùng một mức giá; đảm bảo bằng khóa dòng cơ sở dữ liệu cộng ràng buộc duy nhất `(auction_id, sequence)`. |
| NFR-2 | **Độ bền** — lượt giá đã commit không bao giờ bị mất, ngay cả khi RabbitMQ hoặc consumer không khả dụng tại thời điểm commit. |
| NFR-3 | **An ninh** — xác thực JWT bearer, kiểm tra vai trò, băm BCrypt, truy vấn EF tham số hóa, endpoint nội bộ bảo vệ bằng khóa chia sẻ, danh sách cho phép CORS, không cam kết bí mật sản xuất. |
| NFR-4 | **Khả năng quan sát** — log có cấu trúc, endpoint sức khỏe cho cả bốn dịch vụ, nhật ký kiểm toán, Swagger. |
| NFR-5 | **Hiệu năng** — danh sách phiên được lập chỉ mục theo `(status, end_time_utc)`, tra cứu lượt giá theo phiên, consumer giới hạn prefetch; độ trễ p95 của API dưới 250 ms cho lưu lượng demo cục bộ. |
| NFR-6 | **Khả năng sử dụng** — giao diện cao cấp đáp ứng, biểu mẫu dễ tiếp cận, trạng thái tải/trống/lỗi, modal thân thiện bàn phím, chế độ xem 3D và ảnh. |
| NFR-7 | **Khả năng di chuyển** — chạy cục bộ trên Windows/Linux với .NET 10, PostgreSQL 17, RabbitMQ 4 và Node 24, hoặc với Docker Compose. |
| NFR-8 | **Khả năng kiểm tra** — quy tắc domain được kiểm tra đơn vị, hợp đồng nhắn tin được kiểm tra, đường ghi được kiểm tra tích hợp với PostgreSQL thực tế. |

## 5. Ràng buộc và giả định

- Frontend nói chuyện với API qua HTTP và kết nối trực tiếp tới SignalR hub; không có máy chủ mock nào.
- Dịch vụ gRPC dùng chung cơ sở dữ liệu PostgreSQL nhưng chỉ thực hiện xác thực ở chế độ chỉ đọc (read-only).
- API sở hữu migration schema và tạo dữ liệu mẫu (`Database:AutoMigrate`).
- Xác minh email, thanh toán, vận chuyển và mở rộng chống sniping nằm ngoài phạm vi của phiên bản này.
- Thông tin đăng nhập phát triển trong `appsettings*.json` được xác định rõ là chỉ dành cho phát triển; khi triển khai phải ghi đè bằng biến môi trường hoặc secret store.
- Docker Compose được cung cấp để khởi động bằng một lệnh. Trên máy chủ không có ảo hóa phần cứng, có thể chạy cùng một stack ở chế độ cục bộ (xem README).

## 6. Tiêu chí chấp nhận

1. `dotnet build` thành công với không cảnh báo và không lỗi.
2. `npm run build` thành công với TypeScript nghiêm ngặt.
3. Tất cả bài kiểm tra tự động đều đậm qua trên một phiên bản PostgreSQL thực tế.
4. Người đấu giá có thể đăng nhập, mở phiên trực tiếp, đặt lượt giá hợp lệ và thấy lượt đó được xác thực qua gRPC, được lưu trữ, được publish tới RabbitMQ, được broadcast qua SignalR và được ghi vào `audit_logs`.
5. Các lượt giá không hợp lệ, quá thấp, tự đấu giá, của quản trị viên, của người dùng bị khóa và của phiên đã đóng đều bị từ chối với mã lỗi được ghi nhận.
6. Các lượt giá đồng thời cùng số tiền chỉ có đúng một lượt thành công.
7. Frontend không có điều khiển chết: mọi thao tác nhìn thấy đều gọi API hoặc thay đổi trạng thái UI thực tế.
