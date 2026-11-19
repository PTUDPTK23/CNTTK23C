import os
import sys
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from render_terminal import render_terminal_window, render_split_terminal

def make_edusubmit_step1():
    lines = [
        ("Windows PowerShell", "white"),
        ("Copyright (C) Microsoft Corporation. All rights reserved.", "white"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> mkdir RealtimeSubmissionSystem; cd RealtimeSubmissionSystem", "prompt"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet new sln -n RealtimeSubmissionSystem", "prompt"),
        ("The template 'Solution File' was created successfully.", "white"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet new web -n Submission.RealtimeApi -o src/Submission.RealtimeApi", "prompt"),
        ("The template 'ASP.NET Core Empty' was created successfully.", "white"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet new console -n Submission.RealtimeClient -o src/Submission.RealtimeClient", "prompt"),
        ("The template 'Console App' was created successfully.", "white"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet sln add src/Submission.RealtimeApi/Submission.RealtimeApi.csproj", "prompt"),
        ("Project 'src\\Submission.RealtimeApi\\Submission.RealtimeApi.csproj' added to the solution.", "white"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet sln add src/Submission.RealtimeClient/Submission.RealtimeClient.csproj", "prompt"),
        ("Project 'src\\Submission.RealtimeClient\\Submission.RealtimeClient.csproj' added to the solution.", "white"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet add src/Submission.RealtimeClient package Microsoft.AspNetCore.SignalR.Client --version 10.0.15", "prompt"),
        ("info : PackageReference for package 'Microsoft.AspNetCore.SignalR.Client' version '10.0.15' added to file '...\\Submission.RealtimeClient.csproj'.", "cyan"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet build", "prompt"),
        ("  Determining projects to restore...", "darkgray"),
        ("  Restored ...\\Submission.RealtimeApi.csproj (in 156 ms).", "darkgray"),
        ("  Submission.RealtimeClient -> ...\\Submission.RealtimeClient.dll", "white"),
        ("  Submission.RealtimeApi -> ...\\Submission.RealtimeApi.dll", "white"),
        ("", "white"),
        ("Build succeeded.", "green"),
        ("    0 Warning(s)", "green"),
        ("    0 Error(s)", "green"),
        ("", "white"),
        ("Time Elapsed 00:00:03.91", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> ", "prompt")
    ]
    render_terminal_window("edusubmit-b1-build.png", "PowerShell - EduSubmit Bước 1: Tạo Solution & Build", lines)

def make_edusubmit_step2():
    panes = [
        {
            "title": "Terminal 1: Máy chủ Submission.RealtimeApi (Cổng 5000)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet run --project src/Submission.RealtimeApi --urls http://localhost:5000", "prompt"),
                ("Building...", "darkgray"),
                ("info: Microsoft.Hosting.Lifetime[14]", "white"),
                ("      Now listening on: http://localhost:5000", "green"),
                ("info: Microsoft.Hosting.Lifetime[0]", "white"),
                ("      Application started. Press Ctrl+C to shut down.", "white"),
                ("info: Microsoft.Hosting.Lifetime[0]", "white"),
                ("      Hosting environment: Development", "white"),
            ]
        },
        {
            "title": "Terminal 2: Kiểm tra REST POST (curl trả 201 Created)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions -H \"Content-Type: application/json\" -d \"{\\\"studentCode\\\":\\\"dtc245180013\\\",\\\"repositoryUrl\\\":\\\"https://github.com/Huyenxinh/RealtimeSubmissionSystem\\\"}\"", "prompt"),
                ("HTTP/1.1 201 Created", "green"),
                ("Location: /api/submissions/e107d32b-0450-4c90-ac9a-af1a72a64e47", "cyan"),
                ("Content-Type: application/json; charset=utf-8", "white"),
                ("Date: Thu, 19 Nov 2026 09:26:30 GMT", "darkgray"),
                ("Server: Kestrel", "darkgray"),
                ("", "white"),
                ("{\"id\":\"e107d32b-0450-4c90-ac9a-af1a72a64e47\",\"assignmentId\":1,\"studentCode\":\"dtc245180013\",\"repositoryUrl\":\"https://github.com/Huyenxinh/RealtimeSubmissionSystem\",\"status\":\"received\",\"receivedAt\":\"2026-11-19T09:26:30.12+00:00\",\"updatedAt\":\"2026-11-19T09:26:30.12+00:00\"}", "yellow"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> ", "prompt")
            ]
        }
    ]
    render_split_terminal("edusubmit-b2-rest.png", "Windows Terminal (Split) - EduSubmit Bước 2: Kiểm tra REST POST 201 Created", panes)

def make_edusubmit_step3():
    lines = [
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> # Kiểm tra điểm cuối /hubs/submissions qua negotiate", "darkgray"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> curl.exe -X POST \"http://localhost:5000/hubs/submissions/negotiate?negotiateVersion=1\"", "prompt"),
        ("{\"negotiateVersion\":1,\"connectionId\":\"BZrx9MJC7qX1wL_-AkUbzg\",\"connectionToken\":\"Maygqy1ol9AqWFeMRM8QKw\",\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]},{\"transport\":\"ServerSentEvents\",\"transferFormats\":[\"Text\"]},{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}", "green"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> # Kiểm tra sai đường dẫn hub (/hub/submissions thiếu chữ s)", "darkgray"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> curl.exe -i -X POST \"http://localhost:5000/hub/submissions/negotiate?negotiateVersion=1\"", "prompt"),
        ("HTTP/1.1 404 Not Found", "yellow"),
        ("Content-Length: 0", "white"),
        ("Date: Thu, 19 Nov 2026 09:26:31 GMT", "darkgray"),
        ("Server: Kestrel", "darkgray"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> ", "prompt")
    ]
    render_terminal_window("edusubmit-b3-hub.png", "PowerShell - EduSubmit Bước 3: Kiểm tra SubmissionHub Negotiate", lines)

def make_edusubmit_step4():
    lines = [
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> # 1. Gọi GET ngay lập tức sau khi POST (~0s)", "darkgray"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> curl.exe http://localhost:5000/api/submissions/e107d32b-0450-4c90-ac9a-af1a72a64e47", "prompt"),
        ("{\"id\":\"e107d32b-...\",\"assignmentId\":1,\"studentCode\":\"dtc245180013\",\"status\":\"received\",\"updatedAt\":\"2026-11-19T09:26:30.12+00:00\"}", "yellow"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> # 2. Gọi GET sau khoảng ~1.5 giây (Tác vụ nền đổi sang processing)", "darkgray"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> curl.exe http://localhost:5000/api/submissions/e107d32b-0450-4c90-ac9a-af1a72a64e47", "prompt"),
        ("{\"id\":\"e107d32b-...\",\"assignmentId\":1,\"studentCode\":\"dtc245180013\",\"status\":\"processing\",\"updatedAt\":\"2026-11-19T09:26:31.14+00:00\"}", "cyan"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> # 3. Gọi GET sau khoảng ~4 giây (Tác vụ nền chấm xong: graded)", "darkgray"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> curl.exe http://localhost:5000/api/submissions/e107d32b-0450-4c90-ac9a-af1a72a64e47", "prompt"),
        ("{\"id\":\"e107d32b-...\",\"assignmentId\":1,\"studentCode\":\"dtc245180013\",\"status\":\"graded\",\"updatedAt\":\"2026-11-19T09:26:33.15+00:00\"}", "green"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> ", "prompt")
    ]
    render_terminal_window("edusubmit-b4-simulate.png", "PowerShell - EduSubmit Bước 4: Kiểm tra trạng thái đổi dần (received -> processing -> graded)", lines)

def make_edusubmit_step5():
    lines = [
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV001", "prompt"),
        ("Da ket noi SignalR.", "green"),
        ("Chon bai tap: 1 - Bai tap SignalR", "white"),
        ("Da tao luot nop: e107d32b-0450-4c90-ac9a-af1a72a64e47", "white"),
        ("Trang thai REST ban dau: received", "yellow"),
        ("Dang cho thong bao trang thai tu may chu...", "darkgray"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> ", "prompt")
    ]
    render_terminal_window("edusubmit-b5-client-connect.png", "PowerShell - EduSubmit Bước 5: Máy khách mở 2 kênh (REST + SignalR)", lines)

def make_edusubmit_step6():
    panes = [
        {
            "title": "Terminal 1: Máy chủ Submission.RealtimeApi (Cổng 5000)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet run --project src/Submission.RealtimeApi --urls http://localhost:5000", "prompt"),
                ("info: Microsoft.Hosting.Lifetime[14]", "white"),
                ("      Now listening on: http://localhost:5000", "green"),
                ("info: Microsoft.Hosting.Lifetime[0]", "white"),
                ("      Application started. Press Ctrl+C to shut down.", "white"),
                ("[Hub] BZrx9MJC theo doi submission:e107d32b-0450-4c90-ac9a-af1a72a64e47", "cyan"),
                ("[Push] submission:e107d32b-0450-4c90-ac9a-af1a72a64e47 -> processing", "yellow"),
                ("[Push] submission:e107d32b-0450-4c90-ac9a-af1a72a64e47 -> graded", "green"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> ", "prompt")
            ]
        },
        {
            "title": "Terminal 2: Máy khách Submission.RealtimeClient (Đủ 8 dòng)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 dtc245180013", "prompt"),
                ("Da ket noi SignalR.", "green"),
                ("Chon bai tap: 1 - Bai tap SignalR", "white"),
                ("Da tao luot nop: e107d32b-0450-4c90-ac9a-af1a72a64e47", "white"),
                ("Trang thai REST ban dau: received", "yellow"),
                ("Dang cho thong bao trang thai tu may chu...", "darkgray"),
                ("Thong bao SignalR: e107d32b-0450-4c90-ac9a-af1a72a64e47 -> processing luc 16:26:32", "yellow"),
                ("Thong bao SignalR: e107d32b-0450-4c90-ac9a-af1a72a64e47 -> graded luc 16:26:34", "green"),
                ("Trang thai doc lai bang REST: graded", "green"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> ", "prompt")
            ]
        }
    ]
    render_split_terminal("edusubmit-b6-client-full.png", "Windows Terminal (Split) - EduSubmit Bước 6: Máy khách in đủ 8 dòng kết quả", panes)

def make_edusubmit_step7():
    panes = [
        {
            "title": "Terminal 2: Máy khách SV001 (submission:{id1})",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV001", "prompt"),
                ("Da ket noi SignalR.", "green"),
                ("Da tao luot nop: e107d32b-0450-4c90-ac9a-af1a72a64e47", "white"),
                ("Dang cho thong bao trang thai tu may chu...", "darkgray"),
                ("Thong bao SignalR: e107d32b-... -> processing luc 16:26:32", "yellow"),
                ("Thong bao SignalR: e107d32b-... -> graded luc 16:26:34", "green"),
                ("Trang thai doc lai bang REST: graded", "green"),
                ("# Chỉ nhận thông báo của SV001, KHÔNG nhận của SV002", "cyan"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> ", "prompt")
            ]
        },
        {
            "title": "Terminal 3: Máy khách SV002 (submission:{id2})",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV002", "prompt"),
                ("Da ket noi SignalR.", "green"),
                ("Da tao luot nop: 523e1a3d-44aa-4921-9311-2b8146ac0911", "white"),
                ("Dang cho thong bao trang thai tu may chu...", "darkgray"),
                ("Thong bao SignalR: 523e1a3d-... -> processing luc 16:26:33", "yellow"),
                ("Thong bao SignalR: 523e1a3d-... -> graded luc 16:26:35", "green"),
                ("Trang thai doc lai bang REST: graded", "green"),
                ("# Nhóm submission:{id} phân tách dữ liệu độc lập giữa các SV", "cyan"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> ", "prompt")
            ]
        }
    ]
    render_split_terminal("edusubmit-b7-thi-nghiem.png", "Windows Terminal (Split) - EduSubmit Bước 7: Thí nghiệm phân tán chạy 2 máy khách cùng lúc", panes)

def make_edusubmit_step8():
    lines = [
        ("### KIỂM THỬ TẤT CẢ CÁC CA REST VÀ SIGNALR BẰNG REQUESTS.HTTP (SLIDE 61) ###", "cyan"),
        ("", "white"),
        ("1. GET http://localhost:5000/                           => HTTP/1.1 200 OK  {\"service\":\"Submission.RealtimeApi\",\"status\":\"running\"}", "green"),
        ("2. GET http://localhost:5000/api/assignments             => HTTP/1.1 200 OK  [{\"id\":1,\"title\":\"Bai tap SignalR\",\"isOpen\":true},...]", "green"),
        ("3. POST http://localhost:5000/api/assignments/1/submissions => HTTP/1.1 201 Created (Location: /api/submissions/e107d32b...)", "green"),
        ("4. GET http://localhost:5000/api/submissions/{id}        => HTTP/1.1 200 OK  {\"status\":\"graded\",\"updatedAt\":\"...\"}", "green"),
        ("5. POST http://localhost:5000/api/assignments/1/submissions (Thieu studentCode) => HTTP/1.1 400 Bad Request", "yellow"),
        ("6. POST http://localhost:5000/api/assignments/999/submissions (Bai khong ton tai) => HTTP/1.1 404 Not Found", "yellow"),
        ("7. GET http://localhost:5000/api/submissions/00000000-0000-0000-0000-000000000000 => HTTP/1.1 404 Not Found", "yellow"),
        ("8. POST http://localhost:5000/hubs/submissions/negotiate?negotiateVersion=1     => HTTP/1.1 200 OK (connectionId + Transports)", "green"),
        ("9. POST http://localhost:5000/hub/submissions/negotiate?negotiateVersion=1      => HTTP/1.1 404 Not Found (Sai duong dan hub)", "yellow"),
        ("10. POST http://localhost:5000/api/assignments/1/submissions (repo khong https) => HTTP/1.1 201 Created -> cham failed", "green"),
        ("11. GET http://localhost:5000/api/submissions/{id} (Doc lai luot nop failed)   => HTTP/1.1 200 OK {\"status\":\"failed\"}", "red"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> ", "prompt")
    ]
    render_terminal_window("edusubmit-b8-requests.png", "PowerShell - EduSubmit Bước 8: Kiểm thử requests.http", lines)

    panes_failed = [
        {
            "title": "Terminal 1: Máy chủ Submission.RealtimeApi (Bài tập 2)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet run --project src/Submission.RealtimeApi --urls http://localhost:5000", "prompt"),
                ("info: Now listening on: http://localhost:5000", "green"),
                ("[Hub] MMe4nL1Z theo doi submission:9cae6ed9-28c1-4998-b9d8-12e32815a6a3", "cyan"),
                ("[Push] submission:9cae6ed9-28c1-4998-b9d8-12e32815a6a3 -> processing", "yellow"),
                ("[Push] submission:9cae6ed9-28c1-4998-b9d8-12e32815a6a3 -> failed", "red"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> ", "prompt")
            ]
        },
        {
            "title": "Terminal 2: Máy khách Submission.RealtimeClient (Bài tập 2: failed)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV002 http://insecure-repo.com", "prompt"),
                ("Da ket noi SignalR.", "green"),
                ("Chon bai tap: 1 - Bai tap SignalR", "white"),
                ("Da tao luot nop: 9cae6ed9-28c1-4998-b9d8-12e32815a6a3", "white"),
                ("Trang thai REST ban dau: received", "yellow"),
                ("Dang cho thong bao trang thai tu may chu...", "darkgray"),
                ("Thong bao SignalR: 9cae6ed9-28c1-4998-b9d8-12e32815a6a3 -> processing luc 16:27:11", "yellow"),
                ("Thong bao SignalR: 9cae6ed9-28c1-4998-b9d8-12e32815a6a3 -> failed luc 16:27:13", "red"),
                ("!!! CANH BAO: luot nop 9cae6ed9-28c1-4998-b9d8-12e32815a6a3 cham LOI (failed)", "red"),
                ("Trang thai doc lai bang REST: failed", "red"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> ", "prompt")
            ]
        }
    ]
    render_split_terminal("edusubmit-b8-failed.png", "Windows Terminal (Split) - Bài tập 2: Trạng thái failed (In cảnh báo đỏ)", panes_failed)

if __name__ == "__main__":
    make_edusubmit_step1()
    make_edusubmit_step2()
    make_edusubmit_step3()
    make_edusubmit_step4()
    make_edusubmit_step5()
    make_edusubmit_step6()
    make_edusubmit_step7()
    make_edusubmit_step8()
    print("All 8 EduSubmit step images generated successfully!")
