# ASP.NET Core .NET 10 JWT 基礎課範例

這是一個給 3–4 小時實作課使用的 Controller 型 Web API。範例涵蓋登入、JWT Bearer Authentication、Claims、Role、Policy、Swagger UI、`.http` 與 curl。

> [!WARNING]
> 本專案使用記憶體假帳號與自行簽發的 JWT，**僅供封閉式教學與測試**。正式系統請使用 OIDC/OAuth、身分提供者、密碼雜湊與安全的金鑰管理；不要直接複製本範例的登入流程上線。

## 環境準備

- .NET SDK 10
- 可使用 HTTPS 的開發憑證（必要時執行 `dotnet dev-certs https --trust`）

第一次執行前，請在專案目錄設定 User Secrets：

```bash
dotnet user-secrets set "Jwt:SigningKey" "dev-only-course-key-please-replace-32-chars-min" --project JwtCourseApi.Basic.csproj
```

## 啟動

```bash
dotnet restore
dotnet run --launch-profile basic-https
```

- Swagger UI：<https://localhost:7123/swagger>
- API：<https://localhost:7123>
- HTTP（會重新導向至 HTTPS）：<http://localhost:5123>

示範帳號：

| 帳號 | 密碼 | 角色 | 部門 |
|---|---|---|---|
| `student` | `Student123!` | `Student` | `IT` |
| `admin` | `Admin123!` | `Admin` | `Management` |

## 講義

完整的繁體中文課程講義在 [`docs/aspnet-core-jwt-basic.md`](docs/aspnet-core-jwt-basic.md)。

## 驗證

```bash
dotnet build
```

也可以直接使用 [`JwtCourseApi.Basic.http`](JwtCourseApi.Basic.http) 逐個呼叫 API。先登入、複製 `accessToken`，再貼到檔案上方的 `@token` 變數。
