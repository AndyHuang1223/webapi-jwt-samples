# ASP.NET Core .NET 10 JWT 課程範例

這是一套以繁體中文撰寫的 ASP.NET Core Controller Web API 課程範例，從 JWT、Authentication、Authorization 與 RBAC 的前導觀念開始，逐步實作基礎 JWT API，再延伸到 Refresh Token 與 Cookie 安全流程。

## 課程導覽

建議依照以下順序學習：

| 階段 | 教材／專案 | 內容 | 建議時間 |
|---|---|---|---:|
| 1 | [JWT 前導互動教材](preview-intro/00-jwt-introduction.html) | JWT 結構、Claims、簽章、Authentication 與 Authorization | 30–45 分鐘 |
| 2 | [RBAC 前導互動教材](preview-intro/01-rbac-jwt-introduction.html) | 角色、權限、Role Claim 與角色式存取控制 | 30–45 分鐘 |
| 3 | [基礎版 API](basic/README.md) | Login、JWT Bearer、Claims、Role、Policy、401／403 | 3–4 小時 |
| 4 | [進階版 API](advanced/README.md) | EF Core SQLite、Refresh Token、Rotation、Replay、Logout、Cookie 與 CSRF | 2.5–3 小時 |

兩份 `preview-intro` 是可直接在瀏覽器開啟的單檔互動教材。GitHub 會以原始 HTML 顯示檔案內容；要查看互動效果，請先 clone 或下載專案後，在本機瀏覽器開啟檔案，或從 repository 根目錄啟動簡易靜態伺服器：

```bash
python3 -m http.server 8080 --directory preview-intro
```

接著開啟：

- <http://localhost:8080/00-jwt-introduction.html>
- <http://localhost:8080/01-rbac-jwt-introduction.html>

## 環境準備

- .NET SDK 10
- 可使用 HTTPS 的開發憑證

確認 SDK 並信任 HTTPS 憑證：

```bash
dotnet --version
dotnet dev-certs https --trust
```

## 設定 Signing Key

兩個專案使用不同的 User Secrets ID，第一次執行前請各自設定開發用 Signing Key：

```bash
dotnet user-secrets set \
  "Jwt:SigningKey" \
  "dev-only-course-key-please-replace-32-chars-min" \
  --project basic/JwtCourseApi.Basic.csproj

dotnet user-secrets set \
  "Jwt:SigningKey" \
  "dev-only-course-key-please-replace-32-chars-min" \
  --project advanced/JwtCourseApi.Advanced.csproj
```

Signing Key 不應寫入 `appsettings.json`、原始碼或版本控制。課程範例使用固定的示範帳號，User Secrets 只適合本機開發。

## 建置與測試

```bash
dotnet restore JwtCourse.slnx
dotnet build JwtCourse.slnx
dotnet test advanced/tests/JwtCourseApi.Advanced.Tests/JwtCourseApi.Advanced.Tests.csproj
```

進階版測試涵蓋 Refresh Token Rotation、Replay Detection、Token Family Revoke、Logout、到期、Cookie 屬性、CSRF 與並行 Refresh。

## 啟動 API

在 repository 根目錄執行：

```bash
dotnet run --project basic/JwtCourseApi.Basic.csproj --launch-profile basic-https
dotnet run --project advanced/JwtCourseApi.Advanced.csproj --launch-profile advanced-https
```

兩個 API 可獨立執行：

| API | HTTPS | Swagger |
|---|---|---|
| 基礎版 | <https://localhost:7123> | <https://localhost:7123/swagger> |
| 進階版 | <https://localhost:7223> | <https://localhost:7223/swagger> |

操作範例與完整 API 呼叫順序請參考：

- [基礎版 HTTP 範例](basic/JwtCourseApi.Basic.http)
- [基礎版完整講義](basic/docs/aspnet-core-jwt-basic.md)
- [進階版 HTTP 範例](advanced/JwtCourseApi.Advanced.http)
- [進階版完整講義](advanced/docs/aspnet-core-jwt-refresh-token.md)

進階版在 Development 啟動時會自動套用已提交的 SQLite Migration，並建立本機資料庫。資料庫檔案、編譯產物與測試產物均不納入版本控制；正式部署不應在每個應用程式實例啟動時自動執行 Migration。

## 安全提醒

> [!WARNING]
> 這兩套 API 使用記憶體中的固定教學帳號，並由 API 自行簽發 JWT，僅供封閉式課堂與測試。它們不是 Identity Provider，也不是可直接上線的登入系統。

正式系統應優先採用 OIDC/OAuth 與可信任的 Identity Provider，並搭配密碼雜湊、集中式金鑰管理、Rate limiting、稽核記錄、裝置 Session 管理與適當的 Token 儲存策略。
