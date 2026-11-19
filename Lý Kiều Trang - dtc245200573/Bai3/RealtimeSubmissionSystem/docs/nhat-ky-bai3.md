# Nhat ky thi nghiem SignalR - Bai 3 (Buoc 7)

| STT | Thi nghiem | Du doan | Ket qua that | Giai thich |
|---|---|---|---|---|
| 1 | Chay 2 may khach SV001 va SV002 cung luc | Moi may chi nhan tin cua luot nop minh tao | Moi may khach chi thay luot nop vua tao | Nhom `submission:{id}` tach biet nguoi nhan |
| 2 | Chay may khach khi may chu chua chay | Bao loi ket noi | `HttpRequestException: Connection refused (localhost:5000)` | `WithAutomaticReconnect` chi thu lai sau khi da ket noi thanh cong lan dau |
| 3 | Sua WithUrl thanh `/hub/submissions` | Loi 404 khi bat tay | `HttpRequestException: ... 404 (Not Found)` | Loi o buoc negotiate do khong khop `MapHub("/hubs/submissions")` |
| 4 | Sua `On("SubmissionStatusChanged")` thanh `On("SubmissionStatusChange")` | Khong bao loi nhung khong nhan duoc tin | Khong loi; 10 giay sau in `Qua thoi gian cho thong bao graded.` | Sai ten su kien chieu xuong khien client bo qua trong im lang |
| 5 | Cho 4 giay roi moi goi `WatchSubmission` | Lo mat thong bao SignalR nhung REST van dung | Khong nhan tin nao; `Trang thai doc lai bang REST: graded` | Vao nhom muon thi lo tin; REST luon la nguon su that cuoi cung |
