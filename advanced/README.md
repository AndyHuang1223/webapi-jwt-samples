# ASP.NET Core .NET 10 JWT Refresh Token 進階課範例

這是一套可獨立啟動的進階範例，包含：

- Access Token 與 Refresh Token。
- EF Core SQLite 持久化與 Migration。
- Refresh Token SHA-256 hash 儲存。
- Token Rotation、Replay Detection 與 Token Family Revoke。
- Body 與 HttpOnly Cookie 兩種傳輸模式。
- Cookie 模式的 antiforgery header。
- Logout 與整合測試。

> [!WARNING]
> 這仍是單一程序的教學範例，不是 Identity Provider。正式系統應使用 OIDC/OAuth、集中式資料庫／快取、金鑰管理、Rate limiting、稽核與完整裝置 Session 管理。

## 啟動

從 repository 根目錄設定 User Secrets：

```bash
dotnet user-secrets set \
  "Jwt:SigningKey" \
  "dev-only-course-key-please-replace-32-chars-min" \
  --project advanced/JwtCourseApi.Advanced.csproj
```

執行：

```bash
dotnet restore advanced/JwtCourseApi.Advanced.csproj
dotnet tool restore --tool-manifest advanced/.config/dotnet-tools.json
dotnet run \
  --project advanced/JwtCourseApi.Advanced.csproj \
  --launch-profile advanced-https
```

Swagger UI：<https://localhost:7223/swagger>

Development 啟動時會自動套用已提交的 Migration，並建立 `jwt-course-advanced.db`。資料庫檔案已列入 `.gitignore`。

示範帳號：

| 帳號 | 密碼 | 角色 | 部門 |
|---|---|---|---|
| `student` | `Student123!` | `Student` | `IT` |
| `admin` | `Admin123!` | `Admin` | `Management` |

## API

Body 模式：

- `POST /api/auth/body/login`
- `POST /api/auth/body/refresh`
- `POST /api/auth/body/logout`

Cookie 模式：

- `POST /api/auth/cookie/login`
- `GET /api/auth/cookie/csrf`
- `POST /api/auth/cookie/refresh`
- `POST /api/auth/cookie/logout`

Cookie 模式的 Login 與 Refresh response 不包含原始 Refresh Token。Refresh／Logout 必須同時具備 Refresh Token Cookie、antiforgery cookie 與 `X-CSRF-TOKEN` header。

完整操作順序見 [`JwtCourseApi.Advanced.http`](JwtCourseApi.Advanced.http)，完整講義見 [`docs/aspnet-core-jwt-refresh-token.md`](docs/aspnet-core-jwt-refresh-token.md)。

## 測試

```bash
dotnet test advanced/tests/JwtCourseApi.Advanced.Tests/JwtCourseApi.Advanced.Tests.csproj
```

整合測試使用獨立 SQLite 檔案及可控制的 `TimeProvider`，驗證 Rotation、Replay、不同 family 隔離、Logout、到期、Cookie 屬性、CSRF 與並行 Refresh。
