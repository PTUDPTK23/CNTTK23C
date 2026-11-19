# EduSubmit RESTful API

Solution nay gom hai project .NET 10:

- `Submission.Api`: ASP.NET Core Minimal API, lang nghe tai `http://localhost:5000`.
- `Submission.Client`: ung dung console dung `HttpClient` de liet ke bai tap, nop bai, doc lai luot nop va xu ly loi 400.

## Chay ung dung

Mo terminal tai thu muc chua `RestfulClientServer.slnx`, sau do chay API trong terminal thu nhat:

```powershell
dotnet run --project Submission.Api/Submission.Api.csproj
```

Chay client trong terminal thu hai:

```powershell
dotnet run --project Submission.Client/Submission.Client.csproj
```

Mo `requests.http` trong VS Code de gui lan luot 7 request kiem thu. Bai tap so 3 duoc seed voi `IsOpen = false` de thu loi bai tap da dong. Du lieu assignments va submissions nam trong bo nho, nen submissions se mat khi API dung.

## Cau hoi RESTful API

### 1. Vi sao URI dung danh tu, con hanh dong do phuong thuc HTTP dam nhan?

URI nen dinh danh tai nguyen, vi du `/api/assignments` la tap bai tap va `/api/submissions/{id}` la mot luot nop. Phuong thuc HTTP the hien thao tac tren tai nguyen: `GET` de doc, `POST` de tao. Tach tai nguyen khoi thao tac giup URI nhat quan, de doc va de mo rong ma khong can tao cac duong dan nhu `/getAssignments` hay `/submitAssignment`.

### 2. Stateless the hien o dau trong code?

Moi request tu client gui den API tu mang thong tin can thiet nhu URI, phuong thuc HTTP va body; API khong giu session rieng de nho buoc truoc cua client. Vi du, client doc mot submission bang ID trong URI. Danh sach assignments va submissions van duoc luu trong bo nho server, nhung day la du lieu tai nguyen, khong phai session cua client. Vi du nay khong co database, nen du lieu se reset khi API khoi dong lai.

### 3. Vi sao client va server moi ben khai bao DTO rieng ma van khop nhau?

Hai ben khong can dung chung class .NET. API va client giao tiep qua JSON theo hop dong du lieu: ten truong, kieu du lieu va y nghia cua cac truong phai tuong ung. Vi du, `StudentCode` duoc serialize thanh `studentCode` theo quy tac JSON mac dinh, va client co the deserialize JSON do vao DTO rieng cua minh. Neu mot ben doi hop dong (ten truong hoac kieu du lieu) thi ben con lai can duoc cap nhat de tuong thich.