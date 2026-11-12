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
