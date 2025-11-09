# JWT Authentication API for ASP.NET Core 9

[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build Status](https://github.com/yourusername/jwt-auth-api/workflows/CI%2FCD%20Pipeline/badge.svg)](https://github.com/yourusername/jwt-auth-api/actions)

符合 2024-2025 業界安全標準的企業級 JWT 驗證 API，實作 **Refresh Token Rotation**、重用偵測、自動撤銷機制與完整的測試覆蓋率。

## 🚀 核心特性

### 安全性

- ✅ **Refresh Token Rotation**：一次性使用，每次重新整理自動輪換
- ✅ **Token Family 追蹤**：追蹤令牌血緣關係，偵測異常使用
- ✅ **重用偵測**：自動偵測令牌重用攻擊並撤銷整個 Family
- ✅ **30 秒寬限期**：處理網路延遲，避免誤判合法請求
- ✅ **黑名單機制**：撤銷的令牌無法再次使用
- ✅ **高強度密碼雜湊**：PBKDF2-HMAC-SHA256，100,000 次迭代
- ✅ **結構化日誌**：Serilog 記錄所有安全事件（登入、令牌輪換、重用偵測）
- ✅ **密鑰管理**：User Secrets（開發）、環境變數（IIS）、Azure Key Vault（生產）

### 技術棧

- **ASP.NET Core 9.0**：最新的 .NET 平台
- **JWT Bearer Authentication**：業界標準的令牌驗證
- **Serilog**：結構化日誌記錄
- **Swagger/OpenAPI**：自動生成 API 文件
- **xUnit + FluentAssertions**：完整的單元與整合測試
- **BenchmarkDotNet**：效能基準測試
- **API Versioning**：支援多版本 API（v1）

### 架構特色

- 📦 **Clean Architecture**：清晰的層次分離（Controllers、Services、Models、DTOs）
- 🔧 **依賴注入**：充分利用 ASP.NET Core DI 容器
- 🧪 **90%+ 測試覆蓋率**：核心安全邏輯完整測試
- 📊 **效能監控**：內建基準測試追蹤效能
- 🔄 **CI/CD**：GitHub Actions 自動化測試與部署

## 📋 先決條件

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- IDE：[Visual Studio 2022](https://visualstudio.microsoft.com/) 或 [VS Code](https://code.visualstudio.com/)
- （選用）[Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)（用於 Azure Key Vault）

## ⚡ 快速開始

### 1. Clone 專案

```bash
git clone https://github.com/yourusername/jwt-auth-api.git
cd jwt-auth-api
```

### 2. 產生並設定 JWT 密鑰

```bash
cd JwtAuthApi

# 自動產生並設定密鑰到 User Secrets
dotnet run -- generate-key
```

或手動產生：

```bash
# 產生 64-byte Base64 密鑰
dotnet run --project Tools/KeyGenerator

# 設定到 User Secrets
dotnet user-secrets set "Jwt:SecretKey" "your-generated-key-here"
```

### 3. 執行應用程式

```bash
dotnet run --project JwtAuthApi
```

應用程式將在 `https://localhost:5001` 啟動。

### 4. 訪問 Swagger UI

開啟瀏覽器訪問：

```
https://localhost:5001/swagger
```

## 🔐 使用範例

### 1. 登入取得 Tokens

```bash
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Admin@123"
  }'
```

回應：

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "Xy9mK3pR2Vb...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

### 2. 使用 Access Token 訪問受保護資源

```bash
curl -X GET https://localhost:5001/api/v1/weatherforecast \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

### 3. 重新整理 Token（Rotation）

```bash
curl -X POST https://localhost:5001/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "Xy9mK3pR2Vb..."
  }'
```

每次呼叫都會回傳新的 Access Token 和 Refresh Token，舊的 Refresh Token 立即失效。

### 4. 登出（撤銷 Token Family）

```bash
curl -X POST https://localhost:5001/api/v1/auth/logout \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "Xy9mK3pR2Vb..."
  }'
```

## 📁 專案結構

```
jwt-auth-api/
├── JwtAuthApi/                      # 主要 API 專案
│   ├── Controllers/
│   │   └── V1/                      # API v1 控制器
│   │       ├── AuthController.cs    # 驗證端點（登入、重新整理、登出）
│   │       ├── UserController.cs    # 使用者資訊端點
│   │       ├── AdminController.cs   # Admin 專用端點
│   │       └── WeatherForecastController.cs
│   ├── Services/
│   │   ├── IJwtTokenService.cs      # JWT Token 服務介面
│   │   ├── JwtTokenService.cs       # Token 產生、輪換、撤銷實作
│   │   ├── IUserService.cs          # 使用者服務介面
│   │   └── InMemoryUserService.cs   # 記憶體內存使用者範例
│   ├── Models/
│   │   ├── User.cs                  # 使用者模型
│   │   ├── RefreshTokenFamily.cs   # Token Family 模型
│   │   └── JwtSettings.cs           # JWT 組態模型
│   ├── DTOs/
│   │   ├── LoginRequest.cs
│   │   ├── TokenResponse.cs
│   │   ├── RefreshRequest.cs
│   │   └── ErrorResponse.cs
│   ├── Middleware/
│   │   └── GlobalExceptionHandler.cs # 全域錯誤處理
│   ├── Tools/
│   │   └── SecretKeyGenerator.cs    # 密鑰產生工具
│   ├── Program.cs                   # 應用程式進入點
│   └── appsettings.json             # 組態檔
│
├── JwtAuthApi.Tests/                # 測試專案
│   ├── Integration/                 # 整合測試
│   │   ├── AuthControllerTests.cs   # 驗證流程測試
│   │   └── AuthorizationTests.cs    # 授權測試
│   ├── Unit/                        # 單元測試
│   │   ├── JwtTokenServiceTests.cs
│   │   └── UserServiceTests.cs
│   └── Infrastructure/              # 測試基礎設施
│       ├── WebApplicationFactoryFixture.cs
│       ├── TestDataBuilder.cs
│       └── HttpClientExtensions.cs
│
├── JwtAuthApi.Benchmarks/           # 效能基準測試
│   ├── TokenGenerationBenchmarks.cs
│   ├── PasswordHashingBenchmarks.cs
│   └── RefreshTokenRotationBenchmarks.cs
│
├── docs/                            # 文件
│   ├── IIS-Deployment.md            # IIS 部署指引
│   ├── Azure-KeyVault-Migration.md  # Azure Key Vault 遷移
│   └── Production-Checklist.md      # 生產環境檢查清單
│
└── .github/workflows/
    └── ci.yml                       # GitHub Actions CI/CD
```

## 🔑 預設使用者帳號

| Username | Password  | Roles       |
| -------- | --------- | ----------- |
| admin    | Admin@123 | Admin, User |
| user     | User@123  | User        |
| guest    | Guest@123 | Guest       |

## 🧪 執行測試

### 單元測試與整合測試

```bash
dotnet test
```

### 測試覆蓋率報告

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

覆蓋率門檻設定為 **90%**（line + branch）。

### 效能基準測試

```bash
dotnet run --project JwtAuthApi.Benchmarks --configuration Release
```

結果會產生在 `BenchmarkDotNet.Artifacts/results/` 目錄（HTML、Markdown、CSV 格式）。

## 📊 API 端點

### 驗證端點（v1）

| 方法 | 路徑                   | 說明                 | 需要驗證 |
| ---- | ---------------------- | -------------------- | -------- |
| POST | `/api/v1/auth/login`   | 使用者登入           | ❌       |
| POST | `/api/v1/auth/refresh` | 重新整理令牌（輪換） | ❌       |
| POST | `/api/v1/auth/logout`  | 登出（撤銷 Family）  | ❌       |

### 使用者端點（v1）

| 方法 | 路徑                   | 說明           | 需要驗證 |
| ---- | ---------------------- | -------------- | -------- |
| GET  | `/api/v1/user/profile` | 取得使用者資訊 | ✅       |

### Admin 端點（v1）

| 方法 | 路徑                     | 說明               | 需要驗證 | 需要角色 |
| ---- | ------------------------ | ------------------ | -------- | -------- |
| GET  | `/api/v1/admin/users`    | 取得所有使用者列表 | ✅       | Admin    |
| POST | `/api/v1/admin/settings` | 更新系統設定       | ✅       | Admin    |

### 範例端點（v1）

| 方法 | 路徑                      | 說明             | 需要驗證 |
| ---- | ------------------------- | ---------------- | -------- |
| GET  | `/api/v1/weatherforecast` | 取得天氣預報範例 | ✅       |

## 🔒 安全機制詳解

### Refresh Token Rotation 流程

```
1. 使用者登入
   └─> 產生 Access Token (15 分鐘)
   └─> 產生 Refresh Token RT1 (14 天)
   └─> 建立 Token Family F1

2. Access Token 過期後重新整理
   └─> 客戶端發送 RT1
   └─> 伺服器驗證 RT1
   └─> 產生新 Access Token (15 分鐘)
   └─> 產生新 Refresh Token RT2 (14 天)
   └─> 更新 Family：CurrentToken = RT2, ParentToken = RT1
   └─> RT1 加入黑名單（但保留 30 秒寬限期）

3. 偵測到 Token 重用（攻擊）
   └─> 客戶端嘗試使用已用過的 RT1
   └─> 伺服器偵測 RT1 不是 CurrentToken
   └─> 檢查是否在寬限期內（30 秒）
   └─> 超過寬限期 → 視為重用攻擊
   └─> 撤銷整個 Family F1（RT1、RT2 全部失效）
   └─> 記錄安全事件（UserId、IP、時間）
   └─> 回傳 401 Unauthorized
```

### 寬限期機制

為處理網路延遲或時鐘偏移，實作 30 秒寬限期：

- ✅ **允許**：父 Token 在 30 秒內使用一次（正常輪換延遲）
- ❌ **拒絕**：父 Token 在 30 秒後再次使用（疑似重用攻擊）
- ❌ **拒絕**：父 Token 使用超過一次（確定為重用攻擊）

## 🚀 部署

### IIS 部署

詳細步驟請參閱 [IIS 部署指引](docs/IIS-Deployment.md)

快速步驟：

1. 安裝 .NET 9.0 Hosting Bundle
2. 建立應用程式集區（無受控程式碼）
3. 設定環境變數 `Jwt__SecretKey`
4. 發布應用程式
5. 設定 HTTPS 繫結

### Azure App Service

```bash
# 建立 App Service
az webapp create \
  --name jwtauthapi \
  --resource-group JwtAuthRG \
  --plan JwtAuthPlan \
  --runtime "DOTNET|9.0"

# 設定密鑰（使用 Azure Key Vault 更佳）
az webapp config appsettings set \
  --name jwtauthapi \
  --resource-group JwtAuthRG \
  --settings Jwt__SecretKey="your-secret-key"

# 部署
az webapp deployment source config-zip \
  --name jwtauthapi \
  --resource-group JwtAuthRG \
  --src publish.zip
```

### Azure Key Vault 整合

詳細步驟請參閱 [Azure Key Vault 遷移指引](docs/Azure-KeyVault-Migration.md)

## 📝 組態設定

### appsettings.json

```json
{
  "JwtSettings": {
    "Issuer": "JwtAuthApi",
    "Audience": "JwtAuthApi",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 14,
    "GracePeriodSeconds": 30
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### 環境變數

| 變數名稱                 | 說明                 | 範例                       |
| ------------------------ | -------------------- | -------------------------- |
| `Jwt__SecretKey`         | JWT 簽章密鑰         | Base64 編碼的 64-byte 金鑰 |
| `ASPNETCORE_ENVIRONMENT` | 執行環境             | Development / Production   |
| `KeyVault__Name`         | Azure Key Vault 名稱 | jwtauth-keyvault           |

## 🛡️ 生產環境檢查清單

詳細清單請參閱 [生產環境檢查清單](docs/Production-Checklist.md)

- [ ] 密鑰儲存在安全位置（Key Vault / 環境變數）
- [ ] 啟用 HTTPS 與 HSTS
- [ ] 設定正確的 CORS 原則
- [ ] 日誌不包含敏感資訊
- [ ] Token 過期時間符合安全需求
- [ ] 密碼雜湊迭代次數 >= 100,000
- [ ] 實作速率限制（保護登入端點）
- [ ] 設定應用程式監控與警報
- [ ] 定期更新相依套件
- [ ] 審查安全漏洞

## 🤝 貢獻

歡迎提交 Issue 或 Pull Request！

1. Fork 專案
2. 建立功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交變更 (`git commit -m 'Add some AmazingFeature'`)
4. Push 到分支 (`git push origin feature/AmazingFeature`)
5. 開啟 Pull Request

請確保：

- 所有測試通過
- 測試覆蓋率 >= 90%
- 遵循現有的程式碼風格
- 更新相關文件

## 📄 授權

本專案採用 MIT 授權 - 詳見 [LICENSE](LICENSE) 檔案

## 🙏 致謝

本專案參考以下業界標準與最佳實踐：

- [OAuth 2.0 Security Best Current Practice](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics)
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [Auth0 Refresh Token Rotation](https://auth0.com/docs/secure/tokens/refresh-tokens/refresh-token-rotation)
- [Microsoft Identity Platform Best Practices](https://docs.microsoft.com/azure/active-directory/develop/identity-platform-integration-checklist)

## 📧 聯絡方式

專案連結：[https://github.com/yourusername/jwt-auth-api](https://github.com/yourusername/jwt-auth-api)

## 🔖 版本歷史

### v1.0.0 (2025-01-09)

- ✨ 初始版本發布
- 🔒 實作 Refresh Token Rotation 機制
- 🛡️ Token Family 追蹤與重用偵測
- 📊 完整測試覆蓋率（90%+）
- 📚 完整文件（IIS 部署、Azure Key Vault）
- ⚡ 效能基準測試
- 🚀 GitHub Actions CI/CD

---

**Made with ❤️ using ASP.NET Core 9**
