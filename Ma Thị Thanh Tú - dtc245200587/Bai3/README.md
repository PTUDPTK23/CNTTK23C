# Bài 3: SignalR – Ma Thị Thanh Tú

- **Mã sinh viên:** `dtc245200587`
- **Lớp:** CNTT K23C

*Minh chứng Bài 3: PingSignalR và RealtimeSubmissionSystem.*

---

## Phần 1: PingSignalR

Gồm 2 project:
- `Ping.Api`: server ASP.NET Core có hub `/hubs/ping`, chạy ở `http://localhost:5200`
- `Ping.Client`: console client kết nối tới hub bằng `HubConnectionBuilder`

```powershell
dotnet run --project src/Ping.Api --urls http://localhost:5200
dotnet run --project src/Ping.Client -- http://localhost:5200 An
```

![Minh chứng 1](images/img01.png)
![Minh chứng 2](images/img02.png)
![Minh chứng 3](images/img03.png)
![Minh chứng 4](images/img04.png)
![Minh chứng 5](images/img05.png)
![Minh chứng 6](images/img06.png)
![Minh chứng 7](images/img07.png)
![Minh chứng 8](images/img08.png)
![Minh chứng 9](images/img09.png)
![Minh chứng 10](images/img10.png)
![Minh chứng 11](images/img11.png)
![Minh chứng 12](images/img12.png)

---

## Phần 2: RealtimeSubmissionSystem

Hệ thống nộp bài realtime, có hub `/hubs/submissions` chạy ở `http://localhost:5000`.

```powershell
curl.exe -X POST "http://localhost:5000/hubs/submissions/negotiate?negotiateVersion=1"
```

![Minh chứng 13](images/img13.png)
![Minh chứng 14](images/img14.png)
![Minh chứng 15](images/img15.png)
![Minh chứng 16](images/img16.png)
![Minh chứng 17](images/img17.png)
![Minh chứng 18](images/img18.png)
![Minh chứng 19](images/img19.png)
![Minh chứng 20](images/img20.png)
![Minh chứng 21](images/img21.png)
![Minh chứng 22](images/img22.png)
![Minh chứng 23](images/img23.png)
![Minh chứng 24](images/img24.png)
![Minh chứng 25](images/img25.png)
![Minh chứng 26](images/img26.png)
![Minh chứng 27](images/img27.png)
![Minh chứng 28](images/img28.png)
![Minh chứng 29](images/img29.png)
![Minh chứng 30](images/img30.png)
![Minh chứng 31](images/img31.png)
![Minh chứng 32](images/img32.png)
![Minh chứng 33](images/img33.png)
