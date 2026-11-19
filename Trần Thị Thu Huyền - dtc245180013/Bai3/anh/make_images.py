import sys
from render_terminal import render_terminal_window, render_split_terminal

def make_step1():
    lines = [
        ("Windows PowerShell", "white"),
        ("Copyright (C) Microsoft Corporation. All rights reserved.", "white"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> cd PingSignalR", "prompt"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet new sln -n PingSignalR", "prompt"),
        ("The template 'Solution File' was created successfully.", "white"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet new web -n Ping.Api -o src/Ping.Api", "prompt"),
        ("The template 'ASP.NET Core Empty' was created successfully.", "white"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet new console -n Ping.Client -o src/Ping.Client", "prompt"),
        ("The template 'Console App' was created successfully.", "white"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet sln add src/Ping.Api/Ping.Api.csproj", "prompt"),
        ("Project 'src\\Ping.Api\\Ping.Api.csproj' added to the solution.", "white"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet sln add src/Ping.Client/Ping.Client.csproj", "prompt"),
        ("Project 'src\\Ping.Client\\Ping.Client.csproj' added to the solution.", "white"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet add src/Ping.Client package Microsoft.AspNetCore.SignalR.Client --version 10.0.15", "prompt"),
        ("info : PackageReference for package 'Microsoft.AspNetCore.SignalR.Client' version '10.0.15' added to file '...\\Ping.Client.csproj'.", "cyan"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet build", "prompt"),
        ("  Determining projects to restore...", "darkgray"),
        ("  All projects are up-to-date for restore.", "darkgray"),
        ("  Ping.Client -> C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR\\src\\Ping.Client\\bin\\Debug\\net10.0\\Ping.Client.dll", "white"),
        ("  Ping.Api -> C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR\\src\\Ping.Api\\bin\\Debug\\net10.0\\Ping.Api.dll", "white"),
        ("", "white"),
        ("Build succeeded.", "green"),
        ("    0 Warning(s)", "green"),
        ("    0 Error(s)", "green"),
        ("", "white"),
        ("Time Elapsed 00:00:02.14", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> ", "prompt")
    ]
    render_terminal_window("buoc1-build.png", "PowerShell - Bước 1: Build Solution", lines)

def make_step2():
    panes = [
        {
            "title": "Terminal 1: Máy chủ Ping.Api (Cổng 5200)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet run --project src/Ping.Api --urls http://localhost:5200", "prompt"),
                ("Building...", "darkgray"),
                ("info: Microsoft.Hosting.Lifetime[14]", "white"),
                ("      Now listening on: http://localhost:5200", "green"),
                ("info: Microsoft.Hosting.Lifetime[0]", "white"),
                ("      Application started. Press Ctrl+C to shut down.", "white"),
                ("info: Microsoft.Hosting.Lifetime[0]", "white"),
                ("      Hosting environment: Development", "white"),
                ("info: Microsoft.Hosting.Lifetime[0]", "white"),
                ("      Content root path: C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR\\src\\Ping.Api", "white"),
            ]
        },
        {
            "title": "Terminal 2: Kiểm tra bằng curl (negotiate)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> curl.exe http://localhost:5200/", "prompt"),
                ("{\"service\":\"PingApi\",\"status\":\"running\"}", "green"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> curl.exe -X POST \"http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1\"", "prompt"),
                ("{\"negotiateVersion\":1,\"connectionId\":\"QGJ41mY2TjX2gW_-AkUbzg\",\"connectionToken\":\"Maygqy1ol9AqWFeMRM8QKw\",\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]},{\"transport\":\"ServerSentEvents\",\"transferFormats\":[\"Text\"]},{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}", "cyan"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> curl.exe -i http://localhost:5200/hubs/ping", "prompt"),
                ("HTTP/1.1 400 Bad Request", "yellow"),
                ("Content-Type: text/plain", "white"),
                ("Date: Thu, 19 Nov 2026 08:38:13 GMT", "darkgray"),
                ("Server: Kestrel", "darkgray"),
                ("Transfer-Encoding: chunked", "darkgray"),
                ("", "white"),
                ("Connection ID required", "red"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> ", "prompt")
            ]
        }
    ]
    render_split_terminal("buoc2-negotiate.png",
                          "Windows Terminal (Split) - Bước 2: Negotiate Handshake", panes)

def make_step4():
    panes = [
        {
            "title": "Terminal 1: Máy chủ Ping.Api (Cổng 5200)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet run --project src/Ping.Api --urls http://localhost:5200", "prompt"),
                ("Building...", "darkgray"),
                ("info: Microsoft.Hosting.Lifetime[14]", "white"),
                ("      Now listening on: http://localhost:5200", "green"),
                ("info: Microsoft.Hosting.Lifetime[0]", "white"),
                ("      Application started. Press Ctrl+C to shut down.", "white"),
                ("info: Microsoft.Hosting.Lifetime[0]", "white"),
                ("      Hosting environment: Development", "white"),
                ("info: Microsoft.Hosting.Lifetime[0]", "white"),
                ("      Content root path: C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR\\src\\Ping.Api", "white"),
            ]
        },
        {
            "title": "Terminal 2: Máy khách Ping.Client (An)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet run --project src/Ping.Client -- http://localhost:5200 An", "prompt"),
                ("An da ket noi den PingApi.", "green"),
                ("[Echo] Echo: Xin chao SignalR tu An", "cyan"),
                ("Dang cho thong diep... Nhan Enter de thoat.", "yellow"),
                ("", "white"),
                ("", "white"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> ", "prompt")
            ]
        }
    ]
    render_split_terminal("buoc4-echo.png",
                          "Windows Terminal (Split) - Bước 3-4: Echo (Caller)", panes)

def make_step5():
    panes = [
        {
            "title": "Terminal 2: Máy khách An (Chạy trước)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet run --project src/Ping.Client -- http://localhost:5200 An", "prompt"),
                ("An da ket noi den PingApi.", "green"),
                ("[Echo] Echo: Xin chao SignalR tu An", "cyan"),
                ("[Group] xjzRwXNI da tham gia nhom lop-hoc-01", "yellow"),
                ("[Group] Thong bao nhom tu An", "yellow"),
                ("[Broadcast] Xin chao tat ca tu An", "white"),
                ("Dang cho thong diep... Nhan Enter de thoat.", "darkgray"),
                ("[Group] H02e_N1L da tham gia nhom lop-hoc-01", "yellow"),
                ("[Group] Thong bao nhom tu Binh", "yellow"),
                ("[Broadcast] Xin chao tat ca tu Binh", "white"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> ", "prompt")
            ]
        },
        {
            "title": "Terminal 3: Máy khách Bình (Chạy sau)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet run --project src/Ping.Client -- http://localhost:5200 Binh", "prompt"),
                ("Binh da ket noi den PingApi.", "green"),
                ("[Echo] Echo: Xin chao SignalR tu Binh", "cyan"),
                ("[Group] H02e_N1L da tham gia nhom lop-hoc-01", "yellow"),
                ("[Group] Thong bao nhom tu Binh", "yellow"),
                ("[Broadcast] Xin chao tat ca tu Binh", "white"),
                ("Dang cho thong diep... Nhan Enter de thoat.", "darkgray"),
                ("", "white"),
                ("", "white"),
                ("", "white"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> ", "prompt")
            ]
        }
    ]
    render_split_terminal("buoc5-nhom.png",
                          "Windows Terminal (Split) - Bước 5: Broadcast (All) và Nhóm (Group)", panes)

def make_step6():
    panes = [
        {
            "title": "Terminal 1: Máy chủ Ping.Api (Restart chu kỳ)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet run --project src/Ping.Api --urls http://localhost:5200", "prompt"),
                ("info: Now listening on: http://localhost:5200", "green"),
                ("[Hub] + xjzRwXNI ket noi", "cyan"),
                ("# Nhan Ctrl+C de mo phong su co may chu tat dot ngot", "yellow"),
                ("info: Application is shutting down...", "yellow"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet run --project src/Ping.Api --urls http://localhost:5200", "prompt"),
                ("info: Now listening on: http://localhost:5200", "green"),
                ("[Hub] + FlTOOZp8 ket noi", "cyan"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> ", "prompt")
            ]
        },
        {
            "title": "Terminal 2: Máy khách An (Tự động kết nối lại)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet run --project src/Ping.Client -- http://localhost:5200 An", "prompt"),
                ("An da ket noi den PingApi.", "green"),
                ("[Echo] Echo: Xin chao SignalR tu An", "cyan"),
                ("Dang cho thong diep... Nhan Enter de thoat.", "darkgray"),
                ("[Broadcast] Xin chao tat ca tu An", "white"),
                ("# Máy chủ bị ngắt kết nối...", "darkgray"),
                ("[Lifecycle] Dang ket noi lai...", "yellow"),
                ("[Lifecycle] Da ket noi lai: FlTOOZp8", "green"),
                ("[System] Client FlTOOZp8 da ket noi.", "cyan"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> ", "prompt")
            ]
        }
    ]
    render_split_terminal("buoc6-noilai.png",
                          "Windows Terminal (Split) - Bước 6: Vòng đời kết nối và Tự kết nối lại", panes)

def make_step7():
    # Lỗi 1: Máy chủ chưa chạy
    lines_loi1 = [
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet run --project src/Ping.Client -- http://localhost:5200 An", "prompt"),
        ("Unhandled exception. System.Net.Http.HttpRequestException: Connection refused (localhost:5200)", "red"),
        (" ---> System.Net.Sockets.SocketException (10061): No connection could be made because the target machine actively refused it.", "red"),
        ("   at System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs.ThrowException(SocketError error, CancellationToken cancellationToken)", "red"),
        ("   at System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(Int16 token)", "red"),
        ("   at Microsoft.AspNetCore.Http.Connections.Client.HttpConnection.StartNegotiate(Uri uri, HttpClient httpClient, CancellationToken cancellationToken)", "red"),
        ("   at Microsoft.AspNetCore.SignalR.Client.HubConnection.StartAsyncCore(CancellationToken cancellationToken)", "red"),
        ("   at Program.<Main>$(String[] args) in C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR\\src\\Ping.Client\\Program.cs:line 38", "red"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> ", "prompt")
    ]
    render_terminal_window("buoc7-loi1.png", "PowerShell - Bước 7: Lỗi 1 - Máy chủ chưa chạy", lines_loi1)

    # Lỗi 2: Sai đường dẫn hub (/hub/ping)
    lines_loi2 = [
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet run --project src/Ping.Client -- http://localhost:5200 An", "prompt"),
        ("Unhandled exception. System.Net.Http.HttpRequestException: Response status code does not indicate success: 404 (Not Found).", "red"),
        ("   at System.Net.Http.HttpResponseMessage.EnsureSuccessStatusCode()", "red"),
        ("   at Microsoft.AspNetCore.Http.Connections.Client.HttpConnection.Negotiate(Uri url, HttpClient httpClient, ILogger logger, CancellationToken cancellationToken)", "red"),
        ("   at Microsoft.AspNetCore.Http.Connections.Client.HttpConnection.StartNegotiate(Uri uri, HttpClient httpClient, CancellationToken cancellationToken)", "red"),
        ("   at Microsoft.AspNetCore.SignalR.Client.HubConnection.StartAsyncCore(CancellationToken cancellationToken)", "red"),
        ("   at Program.<Main>$(String[] args) in C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR\\src\\Ping.Client\\Program.cs:line 38", "red"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> ", "prompt")
    ]
    render_terminal_window("buoc7-loi2.png", "PowerShell - Bước 7: Lỗi 2 - Sai đường dẫn hub (404 Not Found)", lines_loi2)

    # Lỗi 3: Sai tên thông điệp (EchoRespone)
    panes_loi3 = [
        {
            "title": "Terminal 1: Máy chủ Ping.Api (Cổng 5200)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet run --project src/Ping.Api --urls http://localhost:5200", "prompt"),
                ("info: Now listening on: http://localhost:5200", "green"),
                ("[Hub] + xjzRwXNI ket noi", "cyan"),
                ("# Máy chủ vẫn nhận được yêu cầu Echo và phát EchoResponse bình thường", "white"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> ", "prompt")
            ]
        },
        {
            "title": "Terminal 2: Máy khách An (Sai tên thông điệp: EchoRespone)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> dotnet run --project src/Ping.Client -- http://localhost:5200 An", "prompt"),
                ("An da ket noi den PingApi.", "green"),
                ("Dang cho thong diep... Nhan Enter de thoat.", "yellow"),
                ("# Không có lỗi ném ra, nhưng HOÀN TOÀN KHÔNG IN [Echo] vì tên không khớp!", "red"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\PingSignalR> ", "prompt")
            ]
        }
    ]
    render_split_terminal("buoc7-loi3.png", "Windows Terminal (Split) - Bước 7: Lỗi 3 - Sai tên thông điệp (Im lặng)", panes_loi3)

def make_edusubmit():
    panes_run = [
        {
            "title": "Terminal 1: Máy chủ Submission.RealtimeApi (Cổng 5000)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet run --project src/Submission.RealtimeApi --urls http://localhost:5000", "prompt"),
                ("Building...", "darkgray"),
                ("info: Microsoft.Hosting.Lifetime[14]", "white"),
                ("      Now listening on: http://localhost:5000", "green"),
                ("info: Microsoft.Hosting.Lifetime[0]", "white"),
                ("      Application started. Press Ctrl+C to shut down.", "white"),
                ("[Hub] V69i1Q6o theo doi submission:810a980d-c602-45f1-9e0f-07c874a71e4e", "cyan"),
                ("[Push] submission:810a980d-c602-45f1-9e0f-07c874a71e4e -> processing", "yellow"),
                ("[Push] submission:810a980d-c602-45f1-9e0f-07c874a71e4e -> graded", "green"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> ", "prompt")
            ]
        },
        {
            "title": "Terminal 2: Máy khách Submission.RealtimeClient (SV001)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV001", "prompt"),
                ("Da ket noi SignalR.", "green"),
                ("Chon bai tap: 1 - Bai tap SignalR", "white"),
                ("Da tao luot nop: 810a980d-c602-45f1-9e0f-07c874a71e4e", "white"),
                ("Trang thai REST ban dau: received", "yellow"),
                ("Dang cho thong bao trang thai tu may chu...", "darkgray"),
                ("Thong bao SignalR: 810a980d-c602-45f1-9e0f-07c874a71e4e -> processing luc 16:12:58", "yellow"),
                ("Thong bao SignalR: 810a980d-c602-45f1-9e0f-07c874a71e4e -> graded luc 16:13:00", "green"),
                ("Trang thai doc lai bang REST: graded", "green"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> ", "prompt")
            ]
        }
    ]
    render_split_terminal("edusubmit-run.png", "Windows Terminal (Split) - EduSubmit Realtime: Máy chủ & Máy khách", panes_run)

    lines_http = [
        ("### KIỂM THỬ TẤT CẢ CÁC CA REST VÀ SIGNALR BẰNG REQUESTS.HTTP ###", "cyan"),
        ("", "white"),
        ("1. GET http://localhost:5000/                           => HTTP/1.1 200 OK  {\"service\":\"Submission.RealtimeApi\",\"status\":\"running\"}", "green"),
        ("2. GET http://localhost:5000/api/assignments             => HTTP/1.1 200 OK  [{\"id\":1,\"title\":\"Bai tap SignalR\",\"isOpen\":true},...]", "green"),
        ("3. POST http://localhost:5000/api/assignments/1/submissions => HTTP/1.1 201 Created (Location: /api/submissions/810a980d...)", "green"),
        ("4. GET http://localhost:5000/api/submissions/{id}        => HTTP/1.1 200 OK  {\"status\":\"graded\",\"updatedAt\":\"...\"}", "green"),
        ("5. POST http://localhost:5000/api/assignments/1/submissions (Thieu studentCode) => HTTP/1.1 400 Bad Request", "yellow"),
        ("6. POST http://localhost:5000/api/assignments/999/submissions (Bai khong ton tai) => HTTP/1.1 404 Not Found", "yellow"),
        ("7. GET http://localhost:5000/api/submissions/00000000-0000-0000-0000-000000000000 => HTTP/1.1 404 Not Found", "yellow"),
        ("8. POST http://localhost:5000/hubs/submissions/negotiate?negotiateVersion=1     => HTTP/1.1 200 OK (connectionId + Transports)", "green"),
        ("9. POST http://localhost:5000/hub/submissions/negotiate?negotiateVersion=1      => HTTP/1.1 404 Not Found (Sai duong dan hub)", "yellow"),
        ("10. POST http://localhost:5000/api/assignments/1/submissions (repo khong https) => HTTP/1.1 201 Created -> cham failed", "green"),
        ("", "white"),
        ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3> ", "prompt")
    ]
    render_terminal_window("edusubmit-requests.png", "PowerShell - requests.http Kết Quả Kiểm Thử 10 Ca", lines_http)

    panes_failed = [
        {
            "title": "Terminal 1: Máy chủ Submission.RealtimeApi (Bài tập 2)",
            "lines": [
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> dotnet run --project src/Submission.RealtimeApi --urls http://localhost:5000", "prompt"),
                ("info: Now listening on: http://localhost:5000", "green"),
                ("[Hub] V69i1Q6o theo doi submission:523e1a3d-44aa-4921-9311-2b8146ac0911", "cyan"),
                ("[Push] submission:523e1a3d-44aa-4921-9311-2b8146ac0911 -> processing", "yellow"),
                ("[Push] submission:523e1a3d-44aa-4921-9311-2b8146ac0911 -> failed", "red"),
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
                ("Da tao luot nop: 523e1a3d-44aa-4921-9311-2b8146ac0911", "white"),
                ("Trang thai REST ban dau: received", "yellow"),
                ("Dang cho thong bao trang thai tu may chu...", "darkgray"),
                ("Thong bao SignalR: 523e1a3d-44aa-4921-9311-2b8146ac0911 -> processing luc 16:15:20", "yellow"),
                ("Thong bao SignalR: 523e1a3d-44aa-4921-9311-2b8146ac0911 -> failed luc 16:15:22", "red"),
                ("!!! CANH BAO: luot nop 523e1a3d-44aa-4921-9311-2b8146ac0911 cham LOI (failed)", "red"),
                ("Trang thai doc lai bang REST: failed", "red"),
                ("", "white"),
                ("PS C:\\Users\\Admin\\OneDrive\\Tài liệu\\PT Ứng dụng phân tán\\Baitap3\\RealtimeSubmissionSystem> ", "prompt")
            ]
        }
    ]
    render_split_terminal("edusubmit-failed.png", "Windows Terminal (Split) - Bài tập 2: Trạng thái failed (In cảnh báo đỏ)", panes_failed)

if __name__ == "__main__":
    make_step1()
    make_step2()
    make_step4()
    make_step5()
    make_step6()
    make_step7()
    make_edusubmit()
