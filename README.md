# Phát triển ứng dụng phân tán (CNTT K23C)
### Trường Đại học Công nghệ Thông tin & Truyền thông - Đại học Thái Nguyên

Thư mục nộp bài tập và dự án của sinh viên lớp Phát triển ứng dụng phân tán (K23.K1.D2.2627.CNTT K23C).

## Hướng dẫn nộp bài (Fork + Pull Request)
> Sinh viên chỉ có quyền **xem** repo này, **không push trực tiếp** được. Mọi bài nộp đều gửi qua **Pull Request (PR)**; giảng viên kiểm tra rồi mới gộp vào.

1. **Fork repo:** bấm nút **Fork** (góc trên bên phải) → **Create fork**. Bạn sẽ có bản sao riêng: `<tài-khoản-github>/CNTTK23C`.
2. **Thêm bài vào fork của mình:** trong thư mục `[Họ và tên] - [Mã sinh viên]` của mình, tạo thư mục bài (`Bai1/`, `BaiTap1/`, `BTL/`, ...) rồi đưa bài vào.
   - Trên web: vào thư mục của mình → **Add file → Upload files** → **Commit changes**.
   - Hoặc dùng git:
     ```bash
     git clone https://github.com/<tài-khoản-github>/CNTTK23C.git
     cd CNTTK23C
     # chép bài vào thư mục "<Họ và tên> - <Mã sinh viên>/Bai1/"
     git add .
     git commit -m "Bai1 - <Mã sinh viên> - <Họ và tên>"
     git push
     ```
3. **Tạo Pull Request:** ở trang fork, bấm **Contribute → Open pull request**. Tiêu đề PR **bắt buộc** theo mẫu: `Bai1 - dtc2452xxxxx - Nguyễn Văn A` (bài 2 thì `Bai2 - ...`). Bấm **Create pull request** là xong.
4. **Sửa / bổ sung bài:** không tạo PR mới, chỉ cần commit thêm vào fork, PR cũ tự cập nhật. Trước khi làm bài mới, bấm **Sync fork** để fork có nội dung mới nhất.

### Ví dụ: tạo thư mục bài và dự án

Giả sử sinh viên **Nguyễn Văn A – dtc245200xxx**. Trong thư mục của mình, mỗi bài/dự án là một thư mục con:

```
Nguyễn Văn A - dtc245200xxx/
├── Bai1/
│   ├── README.md
│   └── (mã nguồn, báo cáo ...)
├── Bai2/
│   └── ...
└── DuAn/
    └── ...
```

Tạo thư mục và nộp bằng git:

```bash
cd "Nguyễn Văn A - dtc245200xxx"
mkdir Bai2          # dự án thì: mkdir DuAn
# chép bài vào Bai2/
git add .
git commit -m "Bai2 - dtc245200xxx - Nguyễn Văn A"
git push
```

Sau đó mở Pull Request với tiêu đề `Bai2 - dtc245200xxx - Nguyễn Văn A`.

### Cập nhật fork bằng git pull (trước khi làm bài mới)

Chỉ làm một lần, để máy biết repo gốc của lớp:

```bash
git remote add upstream https://github.com/PTUDPTK23/CNTTK23C.git
```

Mỗi lần trước khi làm bài mới:

```bash
git checkout main
git pull upstream main    # lấy nội dung mới nhất từ repo của lớp
git push origin main      # đẩy bản đã cập nhật lên fork của bạn
```

Nếu làm trên web thì bấm **Sync fork → Update branch** ở trang fork.

### Quy định
- Chỉ thêm/sửa file **trong thư mục của chính mình**. PR sửa thư mục của bạn khác hoặc file chung (README, .github, ...) sẽ bị từ chối.
- Hệ thống tự gắn nhãn theo thời điểm mở/cập nhật PR: **Đúng hạn**, **Muộn**, **Quá hạn**.
- **Bài 1:** đúng hạn trước **19:00 ngày 05/11/2026**; nộp muộn đến **23:59 ngày 05/11/2026**; sau thời điểm này tính là **quá hạn**.

---

## Danh sách sinh viên

| STT | Mã sinh viên | Họ và tên | Thư mục |
| :---: | :---: | :--- | :--- |
| 1 | `dtc245200360` | Đặng Hoàng Anh | [Đặng Hoàng Anh - dtc245200360](./Đặng Hoàng Anh - dtc245200360) |
| 2 | `dtc245200964` | Phàn Ngọc Anh | [Phàn Ngọc Anh - dtc245200964](./Phàn Ngọc Anh - dtc245200964) |
| 3 | `dtc245200257` | Lương Hải Anh | [Lương Hải Anh - dtc245200257](./Lương Hải Anh - dtc245200257) |
| 4 | `dtc245200383` | Nguyễn Vũ Đức Anh | [Nguyễn Vũ Đức Anh - dtc245200383](./Nguyễn Vũ Đức Anh - dtc245200383) |
| 5 | `dtc245140150` | Lê Sơn Bá | [Lê Sơn Bá - dtc245140150](./Lê Sơn Bá - dtc245140150) |
| 6 | `dtc245200416` | Tô Thanh Biên | [Tô Thanh Biên - dtc245200416](./Tô Thanh Biên - dtc245200416) |
| 7 | `dtc245200392` | Nguyễn Ngọc Chi | [Nguyễn Ngọc Chi - dtc245200392](./Nguyễn Ngọc Chi - dtc245200392) |
| 8 | `dtc245320189` | Chu Thị Chinh | [Chu Thị Chinh - dtc245320189](./Chu Thị Chinh - dtc245320189) |
| 9 | `dtc245120051` | Nguyễn Hải Đăng | [Nguyễn Hải Đăng - dtc245120051](./Nguyễn Hải Đăng - dtc245120051) |
| 10 | `dtc245200452` | Nguyễn Tuấn Đạt | [Nguyễn Tuấn Đạt - dtc245200452](./Nguyễn Tuấn Đạt - dtc245200452) |
| 11 | `dtc245200214` | Trần Quang Dự | [Trần Quang Dự - dtc245200214](./Trần Quang Dự - dtc245200214) |
| 12 | `dtc245200406` | Nguyễn Đức Dũng | [Nguyễn Đức Dũng - dtc245200406](./Nguyễn Đức Dũng - dtc245200406) |
| 13 | `dtc245201007` | Nguyễn Văn Dũng | [Nguyễn Văn Dũng - dtc245201007](./Nguyễn Văn Dũng - dtc245201007) |
| 14 | `dtc245200272` | Nguyễn Thanh Hải | [Nguyễn Thanh Hải - dtc245200272](./Nguyễn Thanh Hải - dtc245200272) |
| 15 | `dtc245200494` | Hoàng Trang Hiên | [Hoàng Trang Hiên - dtc245200494](./Hoàng Trang Hiên - dtc245200494) |
| 16 | `dtc245200354` | Đàm Việt Hoàng | [Đàm Việt Hoàng - dtc245200354](./Đàm Việt Hoàng - dtc245200354) |
| 17 | `dtc245200477` | Phan Ngô Huy Hoàng | [Phan Ngô Huy Hoàng - dtc245200477](./Phan Ngô Huy Hoàng - dtc245200477) |
| 18 | `dtc245201006` | Đinh Bách Hợp | [Đinh Bách Hợp - dtc245201006](./Đinh Bách Hợp - dtc245201006) |
| 19 | `dtc245200362` | Bùi Duy Hưng | [Bùi Duy Hưng - dtc245200362](./Bùi Duy Hưng - dtc245200362) |
| 20 | `dtc245200483` | Phạm Vũ Quang Hưng | [Phạm Vũ Quang Hưng - dtc245200483](./Phạm Vũ Quang Hưng - dtc245200483) |
| 21 | `dtc245200085` | Hoàng Ngọc Huy | [Hoàng Ngọc Huy - dtc245200085](./Hoàng Ngọc Huy - dtc245200085) |
| 22 | `dtc245180013` | Trần Thị Thu Huyền | [Trần Thị Thu Huyền - dtc245180013](./Trần Thị Thu Huyền - dtc245180013) |
| 23 | `dtc245200637` | Đồng Quốc Khánh | [Đồng Quốc Khánh - dtc245200637](./Đồng Quốc Khánh - dtc245200637) |
| 24 | `dtc245200210` | Lâm Tiến Khởi | [Lâm Tiến Khởi - dtc245200210](./Lâm Tiến Khởi - dtc245200210) |
| 25 | `dtc245200484` | Hoàng Trung Kiên | [Hoàng Trung Kiên - dtc245200484](./Hoàng Trung Kiên - dtc245200484) |
| 26 | `dtc245200244` | Vũ Hiếu Kiên | [Vũ Hiếu Kiên - dtc245200244](./Vũ Hiếu Kiên - dtc245200244) |
| 27 | `dtc245200598` | Nguyễn Mạnh Kiên | [Nguyễn Mạnh Kiên - dtc245200598](./Nguyễn Mạnh Kiên - dtc245200598) |
| 28 | `dtc245200390` | Thân Tuấn Linh | [Thân Tuấn Linh - dtc245200390](./Thân Tuấn Linh - dtc245200390) |
| 29 | `dtc245200048` | Nguyễn Ngọc Linh | [Nguyễn Ngọc Linh - dtc245200048](./Nguyễn Ngọc Linh - dtc245200048) |
| 30 | `dtc245200171` | Nguyễn Thành Long | [Nguyễn Thành Long - dtc245200171](./Nguyễn Thành Long - dtc245200171) |
| 31 | `dtc245200652` | Triệu Văn Nam | [Triệu Văn Nam - dtc245200652](./Triệu Văn Nam - dtc245200652) |
| 32 | `dtc245200251` | Trương Thị Bích Ngọc | [Trương Thị Bích Ngọc - dtc245200251](./Trương Thị Bích Ngọc - dtc245200251) |
| 33 | `dtc245200568` | Nguyễn Trọng Nhuận | [Nguyễn Trọng Nhuận - dtc245200568](./Nguyễn Trọng Nhuận - dtc245200568) |
| 34 | `dtc245200324` | Nguyễn Đức Phúc | [Nguyễn Đức Phúc - dtc245200324](./Nguyễn Đức Phúc - dtc245200324) |
| 35 | `dtc245200495` | Trần Bình Quân | [Trần Bình Quân - dtc245200495](./Trần Bình Quân - dtc245200495) |
| 36 | `dtc245200572` | Lê Hoàng Quân | [Lê Hoàng Quân - dtc245200572](./Lê Hoàng Quân - dtc245200572) |
| 37 | `dtc245200225` | Dương Minh Quang | [Dương Minh Quang - dtc245200225](./Dương Minh Quang - dtc245200225) |
| 38 | `dtc245200533` | Phan Khắc Quang | [Phan Khắc Quang - dtc245200533](./Phan Khắc Quang - dtc245200533) |
| 39 | `dtc245200486` | Nguyễn Thanh Sơn | [Nguyễn Thanh Sơn - dtc245200486](./Nguyễn Thanh Sơn - dtc245200486) |
| 40 | `dtc245200867` | Cấn Văn Thái | [Cấn Văn Thái - dtc245200867](./Cấn Văn Thái - dtc245200867) |
| 41 | `dtc245200040` | Chu Thị Thảo | [Chu Thị Thảo - dtc245200040](./Chu Thị Thảo - dtc245200040) |
| 42 | `dtc245200137` | Nguyễn Thị Thanh Thảo | [Nguyễn Thị Thanh Thảo - dtc245200137](./Nguyễn Thị Thanh Thảo - dtc245200137) |
| 43 | `dtc245340007` | Nguyễn Thị Quỳnh Thơ | [Nguyễn Thị Quỳnh Thơ - dtc245340007](./Nguyễn Thị Quỳnh Thơ - dtc245340007) |
| 44 | `dtc245200428` | Vũ Ngọc Anh Thuyên | [Vũ Ngọc Anh Thuyên - dtc245200428](./Vũ Ngọc Anh Thuyên - dtc245200428) |
| 45 | `dtc245200573` | Lý Kiều Trang | [Lý Kiều Trang - dtc245200573](./Lý Kiều Trang - dtc245200573) |
| 46 | `dtc245200321` | Hoàng Anh Tú | [Hoàng Anh Tú - dtc245200321](./Hoàng Anh Tú - dtc245200321) |
| 47 | `dtc245200587` | Ma Thị Thanh Tú | [Ma Thị Thanh Tú - dtc245200587](./Ma Thị Thanh Tú - dtc245200587) |
| 48 | `dtc245200424` | Ma Thị Uyên | [Ma Thị Uyên - dtc245200424](./Ma Thị Uyên - dtc245200424) |
| 49 | `dtc245200377` | Hoàng Đức Việt | [Hoàng Đức Việt - dtc245200377](./Hoàng Đức Việt - dtc245200377) |
| 50 | `dtc245200602` | Nguyễn Viết Cường | [Nguyễn Viết Cường - dtc245200602](./Nguyễn Viết Cường - dtc245200602) |

## Hướng dẫn nộp dự án – Phát triển ứng dụng phân tán (CNTT K23C)

### 1. Quy định chung
- Dự án kéo dài **8 tuần**. Mỗi tuần nộp vào một thư mục riêng: `Tuan1`, `Tuan2`, … `Tuan8`.
- Mỗi sinh viên làm **đúng đề tài đã đăng ký**. Tên thư mục dự án ghi trong bảng ở mục 4, **không tự đặt hoặc đổi tên**.
- **Hạn nộp Tuần 1 của lớp K23C: 23h59 thứ Tư 18/11/2026.** Sau hạn này Tuần 1 không nhận bài. Hạn các tuần sau sẽ được thông báo.

### 2. Cấu trúc thư mục
Trong thư mục cá nhân của mình, dự án nằm trong `DuAn/`, bên trong là thư mục tên dự án (theo bảng mục 4), rồi đến từng tuần:

```
Nguyễn Văn A - dtc245200xxx/
└── DuAn/
    └── DT03_HeThongDatVeSuKienPhanTan/     <- tên dự án đúng theo bảng mục 4
        ├── Tuan1/
        │   ├── README.md
        │   └── (mã nguồn, báo cáo ...)
        └── Tuan2/ ...
```

### 3. Cách nộp
1. Tạo thư mục theo đúng đường dẫn ở bảng mục 4, đặt toàn bộ bài làm Tuần 1 vào `Tuan1/`.
2. Commit, push và mở **Pull Request** vào nhánh `main` của repo này (giống cách nộp Bài 1, Bài 2).
3. Đặt tên PR: `DuAn-Tuan1 - MSSV - Họ tên`
4. Mỗi tuần chỉ **một PR**. Giảng viên yêu cầu sửa thì **push thêm commit vào chính PR đó**, không mở PR mới. Việc sửa cũng phải xong trước hạn.
5. Chỉ thêm file trong thư mục của mình. PR sửa thư mục của bạn khác hoặc sai đường dẫn sẽ bị trả lại.

### 4. Tên dự án và thư mục nộp Tuần 1 của từng sinh viên
Sinh viên không có tên trong bảng (chưa đăng ký đề tài) liên hệ giảng viên trước hạn nộp.

| STT | MSSV | Họ và tên | Đề tài | Thư mục nộp Tuần 1 |
|---|---|---|---|---|
| 1 | dtc245200360 | Đặng Hoàng Anh | 12 – Hệ thống phê duyệt tài liệu phân tán | `Đặng Hoàng Anh - dtc245200360/DuAn/DT12_HeThongPheDuyetTaiLieuPhanTan/Tuan1/` |
| 2 | dtc245200964 | Phàn Ngọc Anh | 04 – Hệ thống đặt món và xử lý tại bếp | `Phàn Ngọc Anh - dtc245200964/DuAn/DT04_HeThongDatMonVaXuLyTaiBep/Tuan1/` |
| 3 | dtc245200257 | Lương Hải Anh | 17 – Hệ thống đặt chuyến xe | `Lương Hải Anh - dtc245200257/DuAn/DT17_HeThongDatChuyenXe/Tuan1/` |
| 4 | dtc245200383 | Nguyễn Vũ Đức Anh | 12 – Hệ thống phê duyệt tài liệu phân tán | `Nguyễn Vũ Đức Anh - dtc245200383/DuAn/DT12_HeThongPheDuyetTaiLieuPhanTan/Tuan1/` |
| 5 | dtc245140150 | Lê Sơn Bá | 04 – Hệ thống đặt món và xử lý tại bếp | `Lê Sơn Bá - dtc245140150/DuAn/DT04_HeThongDatMonVaXuLyTaiBep/Tuan1/` |
| 6 | dtc245200392 | Nguyễn Ngọc Chi | 17 – Hệ thống đặt chuyến xe | `Nguyễn Ngọc Chi - dtc245200392/DuAn/DT17_HeThongDatChuyenXe/Tuan1/` |
| 7 | dtc245320189 | Chu Thị Chinh | 18 – Hệ thống đăng ký sự kiện và check-in | `Chu Thị Chinh - dtc245320189/DuAn/DT18_HeThongDangKySuKienVaCheckIn/Tuan1/` |
| 8 | dtc245120051 | Nguyễn Hải Đăng | 20 – Hệ thống thư viện đặt mượn sách phân tán | `Nguyễn Hải Đăng - dtc245120051/DuAn/DT20_HeThongThuVienDatMuonSachPhanTan/Tuan1/` |
| 9 | dtc245200452 | Nguyễn Tuấn Đạt | 20 – Hệ thống thư viện đặt mượn sách phân tán | `Nguyễn Tuấn Đạt - dtc245200452/DuAn/DT20_HeThongThuVienDatMuonSachPhanTan/Tuan1/` |
| 10 | dtc245200214 | Trần Quang Dự | 07 – Hệ thống hỗ trợ yêu cầu khách hàng | `Trần Quang Dự - dtc245200214/DuAn/DT07_HeThongHoTroYeuCauKhachHang/Tuan1/` |
| 11 | dtc245201007 | Nguyễn Văn Dũng | 05 – Hệ thống xử lý tệp bất đồng bộ | `Nguyễn Văn Dũng - dtc245201007/DuAn/DT05_HeThongXuLyTepBatDongBo/Tuan1/` |
| 12 | dtc245200272 | Nguyễn Thanh Hải | 15 – Hệ thống nộp bài và chấm bài tự động | `Nguyễn Thanh Hải - dtc245200272/DuAn/DT15_HeThongNopBaiVaChamBaiTuDong/Tuan1/` |
| 13 | dtc245200494 | Hoàng Trang Hiên | 05 – Hệ thống xử lý tệp bất đồng bộ | `Hoàng Trang Hiên - dtc245200494/DuAn/DT05_HeThongXuLyTepBatDongBo/Tuan1/` |
| 14 | dtc245200354 | Đàm Việt Hoàng | 12 – Hệ thống phê duyệt tài liệu phân tán | `Đàm Việt Hoàng - dtc245200354/DuAn/DT12_HeThongPheDuyetTaiLieuPhanTan/Tuan1/` |
| 15 | dtc245200477 | Phan Ngô Huy Hoàng | 15 – Hệ thống nộp bài và chấm bài tự động | `Phan Ngô Huy Hoàng - dtc245200477/DuAn/DT15_HeThongNopBaiVaChamBaiTuDong/Tuan1/` |
| 16 | dtc245201006 | Đinh Bách Hợp | 01 – Hệ thống xử lý đơn hàng và thông báo trạng thái phân tán | `Đinh Bách Hợp - dtc245201006/DuAn/DT01_HeThongXuLyDonHangVaThongBaoTrangThaiPhanTan/Tuan1/` |
| 17 | dtc245200362 | Bùi Duy Hưng | 12 – Hệ thống phê duyệt tài liệu phân tán | `Bùi Duy Hưng - dtc245200362/DuAn/DT12_HeThongPheDuyetTaiLieuPhanTan/Tuan1/` |
| 18 | dtc245200483 | Phạm Vũ Quang Hưng | 03 – Hệ thống đặt vé sự kiện phân tán | `Phạm Vũ Quang Hưng - dtc245200483/DuAn/DT03_HeThongDatVeSuKienPhanTan/Tuan1/` |
| 19 | dtc245200085 | Hoàng Ngọc Huy | 17 – Hệ thống đặt chuyến xe | `Hoàng Ngọc Huy - dtc245200085/DuAn/DT17_HeThongDatChuyenXe/Tuan1/` |
| 20 | dtc245180013 | Trần Thị Thu Huyền | 03 – Hệ thống đặt vé sự kiện phân tán | `Trần Thị Thu Huyền - dtc245180013/DuAn/DT03_HeThongDatVeSuKienPhanTan/Tuan1/` |
| 21 | dtc245200637 | Đồng Quốc Khánh | 13 – Hệ thống quản lý tồn kho đa dịch vụ | `Đồng Quốc Khánh - dtc245200637/DuAn/DT13_HeThongQuanLyTonKhoDaDichVu/Tuan1/` |
| 22 | dtc245200484 | Hoàng Trung Kiên | 10 – Hệ thống đấu giá trực tuyến | `Hoàng Trung Kiên - dtc245200484/DuAn/DT10_HeThongDauGiaTrucTuyen/Tuan1/` |
| 23 | dtc245200244 | Vũ Hiếu Kiên | 19 – Hệ thống tra cứu dữ liệu học tập bằng GraphQL | `Vũ Hiếu Kiên - dtc245200244/DuAn/DT19_HeThongTraCuuDuLieuHocTapBangGraphQL/Tuan1/` |
| 24 | dtc245200598 | Nguyễn Mạnh Kiên | 04 – Hệ thống đặt món và xử lý tại bếp | `Nguyễn Mạnh Kiên - dtc245200598/DuAn/DT04_HeThongDatMonVaXuLyTaiBep/Tuan1/` |
| 25 | dtc245200048 | Nguyễn Ngọc Linh | 18 – Hệ thống đăng ký sự kiện và check-in | `Nguyễn Ngọc Linh - dtc245200048/DuAn/DT18_HeThongDangKySuKienVaCheckIn/Tuan1/` |
| 26 | dtc245200171 | Nguyễn Thành Long | 01 – Hệ thống xử lý đơn hàng và thông báo trạng thái phân tán | `Nguyễn Thành Long - dtc245200171/DuAn/DT01_HeThongXuLyDonHangVaThongBaoTrangThaiPhanTan/Tuan1/` |
| 27 | dtc245200652 | Triệu Văn Nam | 06 – Hệ thống giám sát cảm biến IoT | `Triệu Văn Nam - dtc245200652/DuAn/DT06_HeThongGiamSatCamBienIot/Tuan1/` |
| 28 | dtc245200251 | Trương Thị Bích Ngọc | 09 – Hệ thống đặt lịch khám và quản lý hàng đợi | `Trương Thị Bích Ngọc - dtc245200251/DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1/` |
| 29 | dtc245200568 | Nguyễn Trọng Nhuận | 15 – Hệ thống nộp bài và chấm bài tự động | `Nguyễn Trọng Nhuận - dtc245200568/DuAn/DT15_HeThongNopBaiVaChamBaiTuDong/Tuan1/` |
| 30 | dtc245200324 | Nguyễn Đức Phúc | 08 – Hệ thống theo dõi bưu gửi | `Nguyễn Đức Phúc - dtc245200324/DuAn/DT08_HeThongTheoDoiBuuGui/Tuan1/` |
| 31 | dtc245200495 | Trần Bình Quân | 08 – Hệ thống theo dõi bưu gửi | `Trần Bình Quân - dtc245200495/DuAn/DT08_HeThongTheoDoiBuuGui/Tuan1/` |
| 32 | dtc245200572 | Lê Hoàng Quân | 11 – Hệ thống gửi thông báo hàng loạt | `Lê Hoàng Quân - dtc245200572/DuAn/DT11_HeThongGuiThongBaoHangLoat/Tuan1/` |
| 33 | dtc245200225 | Dương Minh Quang | 19 – Hệ thống tra cứu dữ liệu học tập bằng GraphQL | `Dương Minh Quang - dtc245200225/DuAn/DT19_HeThongTraCuuDuLieuHocTapBangGraphQL/Tuan1/` |
| 34 | dtc245200533 | Phan Khắc Quang | 17 – Hệ thống đặt chuyến xe | `Phan Khắc Quang - dtc245200533/DuAn/DT17_HeThongDatChuyenXe/Tuan1/` |
| 35 | dtc245200486 | Nguyễn Thanh Sơn | 16 – Hệ thống chat hỗ trợ trực tuyến | `Nguyễn Thanh Sơn - dtc245200486/DuAn/DT16_HeThongChatHoTroTrucTuyen/Tuan1/` |
| 36 | dtc245200867 | Cấn Văn Thái | 06 – Hệ thống giám sát cảm biến IoT | `Cấn Văn Thái - dtc245200867/DuAn/DT06_HeThongGiamSatCamBienIot/Tuan1/` |
| 37 | dtc245200040 | Chu Thị Thảo | 18 – Hệ thống đăng ký sự kiện và check-in | `Chu Thị Thảo - dtc245200040/DuAn/DT18_HeThongDangKySuKienVaCheckIn/Tuan1/` |
| 38 | dtc245200137 | Nguyễn Thị Thanh Thảo | 07 – Hệ thống hỗ trợ yêu cầu khách hàng | `Nguyễn Thị Thanh Thảo - dtc245200137/DuAn/DT07_HeThongHoTroYeuCauKhachHang/Tuan1/` |
| 39 | dtc245340007 | Nguyễn Thị Quỳnh Thơ | 05 – Hệ thống xử lý tệp bất đồng bộ | `Nguyễn Thị Quỳnh Thơ - dtc245340007/DuAn/DT05_HeThongXuLyTepBatDongBo/Tuan1/` |
| 40 | dtc245200428 | Vũ Ngọc Anh Thuyên | 19 – Hệ thống tra cứu dữ liệu học tập bằng GraphQL | `Vũ Ngọc Anh Thuyên - dtc245200428/DuAn/DT19_HeThongTraCuuDuLieuHocTapBangGraphQL/Tuan1/` |
| 41 | dtc245200573 | Lý Kiều Trang | 09 – Hệ thống đặt lịch khám và quản lý hàng đợi | `Lý Kiều Trang - dtc245200573/DuAn/DT09_HeThongDatLichKhamVaQuanLyHangDoi/Tuan1/` |
| 42 | dtc245200321 | Hoàng Anh Tú | 06 – Hệ thống giám sát cảm biến IoT | `Hoàng Anh Tú - dtc245200321/DuAn/DT06_HeThongGiamSatCamBienIot/Tuan1/` |
| 43 | dtc245200587 | Ma Thị Thanh Tú | 20 – Hệ thống thư viện đặt mượn sách phân tán | `Ma Thị Thanh Tú - dtc245200587/DuAn/DT20_HeThongThuVienDatMuonSachPhanTan/Tuan1/` |
| 44 | dtc245200424 | Ma Thị Uyên | 12 – Hệ thống phê duyệt tài liệu phân tán | `Ma Thị Uyên - dtc245200424/DuAn/DT12_HeThongPheDuyetTaiLieuPhanTan/Tuan1/` |
| 45 | dtc245200377 | Hoàng Đức Việt | 03 – Hệ thống đặt vé sự kiện phân tán | `Hoàng Đức Việt - dtc245200377/DuAn/DT03_HeThongDatVeSuKienPhanTan/Tuan1/` |
