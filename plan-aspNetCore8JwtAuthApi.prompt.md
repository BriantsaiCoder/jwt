# 計劃：ASP.NET Core 8 JWT 驗證 Web API（完整企業級方案）

建立符合業界標準的 JWT 驗證系統，實作 **Refresh Token Rotation**（一次性使用）、Token Family 追蹤、重用偵測（30 秒寬限期）、自動撤銷機制。包含結構化日誌記錄（Serilog）、Swagger 文件、全域錯誤處理、整合式密鑰生成工具、完整 xUnit 整合測試（90%+ 覆蓋率）、coverlet 覆蓋率報告、BenchmarkDotNet 效能測試、GitHub Actions CI/CD、IIS 部署指引，以及 API 版本控制。

## 步驟

### 1. 建立方案與專案結構

執行 `dotnet new sln -n JwtAuth`、`dotnet new webapi -n JwtAuthApi`、`dotnet new xunit -n JwtAuthApi.Tests`、`dotnet new console -n JwtAuthApi.Benchmarks`，使用 `dotnet sln add` 將三個專案加入方案，設定測試與基準專案參考 API 專案

### 2. 安裝所有 NuGet 相依套件

API 專案安裝：`Microsoft.AspNetCore.Authentication.JwtBearer`、`Swashbuckle.AspNetCore`、`Serilog.AspNetCore`、`Serilog.Sinks.Console`、`Serilog.Sinks.File`、`Asp.Versioning.Mvc`、`Asp.Versioning.Mvc.ApiExplorer`；測試專案：`Microsoft.AspNetCore.Mvc.Testing`、`FluentAssertions`、`coverlet.collector`、`coverlet.msbuild`；基準專案：`BenchmarkDotNet`

### 3. 建立密鑰生成工具

在 `JwtAuthApi/Tools/SecretKeyGenerator.cs` 建立靜態類別，實作 `GenerateSecureKey()` 使用 `RandomNumberGenerator.GetBytes(64)` 產生並轉換為 Base64、`SetUserSecret(key)` 使用 `Process.Start` 執行 `dotnet user-secrets set "Jwt:SecretKey" "{key}"`、`GenerateAndSetKey()` 組合兩者，在 `Program.cs` 開發環境啟動時檢查並自動產生

### 4. 定義領域模型

建立 `Models/User.cs`（Id、Username、PasswordHash、Roles[]、CreatedAt）、`Models/RefreshTokenFamily.cs`（FamilyId、CurrentToken、ParentToken、UserId、IssuedAt、ExpiresAt、IsRevoked、RevokedAt、RevokedReason）、`Models/JwtSettings.cs`（Issuer、Audience、SecretKey、AccessTokenExpiryMinutes、RefreshTokenExpiryDays、GracePeriodSeconds）

### 5. 建立 DTOs 與請求驗證

建立 `DTOs/LoginRequest.cs`（Username、Password 加入 `[Required]`、`[StringLength]` 屬性）、`DTOs/TokenResponse.cs`（AccessToken、RefreshToken、ExpiresIn、TokenType = "Bearer"）、`DTOs/RefreshRequest.cs`（RefreshToken）、`DTOs/ErrorResponse.cs`（StatusCode、Message、TraceId、Timestamp、Errors）

### 6. 設定應用程式組態檔

在 `appsettings.json` 建立 `JwtSettings` 區段（Issuer: "JwtAuthApi"、Audience: "JwtAuthApi"、AccessTokenExpiryMinutes: 15、RefreshTokenExpiryDays: 14、GracePeriodSeconds: 30）、Serilog 組態（WriteTo Console + File）、在 `appsettings.Development.json` 設定 MinimumLevel.Debug

### 7. 實作 JWT Token 服務

建立 `Services/IJwtTokenService.cs` 介面與 `Services/JwtTokenService.cs` 實作，注入 `IOptions<JwtSettings>`、`IMemoryCache`、`ILogger`，實作方法：`GenerateAccessToken(User user)`、`GenerateRefreshToken()`、`CreateTokenFamily(string userId, string refreshToken)`、`RotateRefreshToken(string refreshToken, string userId)`、`DetectReuseAndRevoke(string familyId, string reason)`、`RevokeTokenFamily(string familyId, string reason)`、`IsTokenBlacklisted(string jti)`、`AddToBlacklist(string jti, DateTime expiresAt)`

### 8. 實作 Token 輪換核心邏輯

在 `JwtTokenService.RotateRefreshToken` 方法中：從快取取得 Token Family、檢查 CurrentToken 是否匹配、若不匹配檢查是否為 ParentToken 且在寬限期內（30 秒）、若為舊 Token 重用則呼叫 `DetectReuseAndRevoke` 撤銷整個 Family、產生新 RefreshToken、更新 Family（CurrentToken、ParentToken、IssuedAt）、將舊 Token 的 Jti 加入黑名單、回傳新 Tokens

### 9. 實作使用者服務

建立 `Services/IUserService.cs` 與 `Services/InMemoryUserService.cs`，注入 `IPasswordHasher<User>`，在建構函式建立 3 個測試使用者（使用 PasswordHasher.HashPassword）：`admin/Admin@123` (["Admin", "User"])、`user/User@123` (["User"])、`guest/Guest@123` (["Guest"])，實作 `ValidateCredentialsAsync(username, password)`、`GetUserByIdAsync(id)`、`GetUserByUsernameAsync(username)`

### 10. 設定密碼雜湊強度

在 `Program.cs` 註冊服務時設定 `services.Configure<PasswordHasherOptions>(options => { options.IterationCount = 100000; })`、`services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>()`，確保符合 2024-2025 安全標準

### 11. 建立全域錯誤處理

實作 `Middleware/GlobalExceptionHandler.cs` 實作 `IExceptionHandler` 介面，在 `TryHandleAsync` 方法中根據例外類型設定 StatusCode（UnauthorizedAccessException: 401、ArgumentException: 400、KeyNotFoundException: 404、其他: 500），建立 `ErrorResponse` 並寫入 Response，記錄到 `ILogger` 包含 StackTrace 與 TraceId

### 12. 設定 API 版本控制

在 `Program.cs` 加入 `services.AddApiVersioning(options => { options.DefaultApiVersion = new ApiVersion(1, 0); options.AssumeDefaultVersionWhenUnspecified = true; options.ReportApiVersions = true; options.ApiVersionReader = new UrlSegmentApiVersionReader(); }).AddApiExplorer(options => { options.GroupNameFormat = "'v'VVV"; options.SubstituteApiVersionInUrl = true; })`

### 13. 註冊服務與中介軟體管線

在 `Program.cs` 設定：`UseSerilog`、綁定 `JwtSettings` 到 `IOptions`、`AddMemoryCache`、註冊 Singleton 服務（IJwtTokenService、IUserService、PasswordHasher）、`AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`、`AddJwtBearer` 設定 TokenValidationParameters、註冊 OnTokenValidated 事件檢查黑名單、`AddExceptionHandler<GlobalExceptionHandler>`、`AddSwaggerGen`

### 14. 設定 JWT 驗證參數與事件

在 `AddJwtBearer` 設定：TokenValidationParameters（ValidateIssuer/Audience/Lifetime/IssuerSigningKey = true、IssuerSigningKey 從 JwtSettings.SecretKey 建立 SymmetricSecurityKey）、Events.OnTokenValidated 取得 Jti Claim 並呼叫 `IsTokenBlacklisted` 檢查、OnAuthenticationFailed 記錄錯誤到 Serilog

### 15. 設定 Swagger 多版本與安全

在 `AddSwaggerGen` 設定：為 v1 版本加入 SwaggerDoc、AddSecurityDefinition("Bearer" 使用 BearerFormat "JWT")、AddSecurityRequirement（全域套用 Bearer scheme）、IncludeXmlComments（啟用 XML 文件）、在專案檔加入 `<GenerateDocumentationFile>true</GenerateDocumentationFile>`

### 16. 實作驗證控制器

建立 `Controllers/V1/AuthController.cs`，標註 `[ApiVersion("1.0")]`、`[Route("api/v{version:apiVersion}/auth")]`、`[ApiController]`，實作端點：`POST login`（呼叫 UserService.ValidateCredentials、產生 Tokens、記錄 UserLogin 事件）、`POST refresh`（呼叫 JwtTokenService.RotateRefreshToken、捕捉重用例外並記錄 TokenReuseDetected、回傳新 Tokens）、`POST logout`（撤銷 Token Family、記錄 UserLogout）

### 17. 加入結構化安全事件記錄

在 `AuthController` 所有端點使用 `_logger.LogInformation` 記錄結構化資訊：Login 成功/失敗（UserId、Username、IP from HttpContext.Connection.RemoteIpAddress）、Token Rotated（UserId、FamilyId、IssuedAt）、Token Reuse Detected（UserId、FamilyId、AttemptedToken、IP、Timestamp）、Logout（UserId、FamilyId）

### 18. 建立受保護資源控制器

建立 `Controllers/V1/WeatherForecastController.cs`（`[Authorize]`、GET 端點回傳範例天氣資料）、`Controllers/V1/AdminController.cs`（`[Authorize(Roles = "Admin")]`、GET/POST 端點）、`Controllers/V1/UserController.cs`（`[Authorize]`、GET `/profile` 從 `User.Claims` 讀取 UserId/Username/Roles 回傳使用者資訊）

### 19. 為控制器加入 XML 註解

在所有控制器端點加入 `<summary>`、`<remarks>`、`<param>`、`<response code="200/400/401/403/500">` 標籤，說明請求格式、成功/失敗回應、驗證流程（Login → 取得 Tokens → Refresh 輪換 → Logout 撤銷）、Token 重用偵測機制

### 20. 建立測試基礎設施

在 `JwtAuthApi.Tests/Infrastructure/` 建立：`WebApplicationFactoryFixture.cs`（繼承 `WebApplicationFactory<Program>`、`ConfigureWebHost` 覆寫 IMemoryCache 為測試專用實例、設定測試 Configuration）、`TestDataBuilder.cs`（提供 CreateUser、CreateLoginRequest、CreateTokenResponse 等 Builder 方法）、`HttpClientExtensions.cs`（`AddBearerToken`、`SetAuthorizationHeader` 擴充方法）

### 21. 實作驗證流程整合測試

建立 `Tests/Integration/AuthControllerTests.cs` 使用 `WebApplicationFactoryFixture`，測試案例：`Login_WithValidCredentials_ReturnsTokens`、`Login_WithInvalidPassword_Returns401`、`Refresh_WithValidToken_ReturnsNewTokens`、`Refresh_WithReusedToken_RevokesFamily_Returns401`、`Refresh_WithParentTokenInGracePeriod_AllowsOnce`、`Refresh_WithParentTokenOutsideGracePeriod_ReturnsUnauthorized`、`Logout_RevokesTokenFamily`、`RevokedFamily_AllTokensInvalid`，使用 FluentAssertions

### 22. 實作核心服務單元測試

建立 `Tests/Unit/JwtTokenServiceTests.cs`，測試：`GenerateAccessToken_ContainsCorrectClaims`、`GenerateRefreshToken_ReturnsBase64String`、`CreateTokenFamily_StoresInCache`、`RotateRefreshToken_UpdatesFamily`、`RotateRefreshToken_DetectsReuse_RevokesFamily`、`IsTokenBlacklisted_ReturnsTrue_WhenBlacklisted`、`GracePeriod_AllowsParentTokenOnce`；建立 `Tests/Unit/UserServiceTests.cs` 測試密碼驗證與雜湊

### 23. 實作授權測試

建立 `Tests/Integration/AuthorizationTests.cs`，測試：`AccessProtectedEndpoint_WithoutToken_Returns401`、`AccessProtectedEndpoint_WithValidToken_Returns200`、`AccessProtectedEndpoint_WithExpiredToken_Returns401`、`AccessProtectedEndpoint_WithBlacklistedToken_Returns401`、`AccessAdminEndpoint_WithUserRole_Returns403`、`AccessAdminEndpoint_WithAdminRole_Returns200`

### 24. 設定測試覆蓋率門檻

在 `JwtAuthApi.Tests.csproj` 加入 PropertyGroup：`<CollectCoverage>true</CollectCoverage>`、`<CoverletOutputFormat>opencover,json,lcov,cobertura</CoverletOutputFormat>`、`<Exclude>[JwtAuthApi]JwtAuthApi.Program,[JwtAuthApi]*.DTOs.*</Exclude>`、`<Threshold>90</Threshold>`、`<ThresholdType>line,branch</ThresholdType>`、`<ThresholdStat>total</ThresholdStat>`

### 25. 建立效能基準測試

在 `JwtAuthApi.Benchmarks/` 建立：`TokenGenerationBenchmarks.cs`（`[Benchmark]` 方法測試單次與批次 Token 產生）、`TokenValidationBenchmarks.cs`（測試 Token 解析與驗證）、`PasswordHashingBenchmarks.cs`（使用 `[Params(10000, 100000, 500000)]` 比較不同 IterationCount）、`RefreshTokenRotationBenchmarks.cs`（測試完整輪換流程含快取操作），加入 `[MemoryDiagnoser]`、`[SimpleJob(RuntimeMoniker.Net80)]` 屬性

### 26. 設定基準測試執行

在 `JwtAuthApi.Benchmarks/Program.cs` 設定 `var summary = BenchmarkRunner.Run<TokenGenerationBenchmarks>()` 或使用 `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args)` 支援參數選擇，設定輸出格式（HtmlExporter、MarkdownExporter、CsvExporter）到 `BenchmarkDotNet.Artifacts/results/`

### 27. 建立 GitHub Actions CI/CD

建立 `.github/workflows/ci.yml`，設定 jobs：build（dotnet restore、build）、test（dotnet test、上傳覆蓋率到 Codecov 使用 codecov/codecov-action）、benchmark（僅在 push to main 時執行、dotnet run -c Release --project Benchmarks、上傳結果為 artifact）、觸發條件（push、pull_request to main）

### 28. 建立 IIS 部署組態

建立 `web.config`（設定 aspNetCore 模組：processPath="dotnet"、arguments=".\JwtAuthApi.dll"、stdoutLogEnabled="true"、stdoutLogFile=".\logs\stdout"、hostingModel="inprocess"）、`appsettings.Production.json`（覆寫 Serilog file path 為絕對路徑、設定 AllowedHosts）、發布設定檔 `Properties/PublishProfiles/IIS.pubxml`（PublishProtocol: FileSystem、TargetFramework: net8.0、SelfContained: false）

### 29. 撰寫 IIS 部署文件

建立 `docs/IIS-Deployment.md`，包含步驟：安裝 .NET 8 Hosting Bundle、建立 IIS 應用程式集區（.NET CLR Version: No Managed Code、Enable 32-bit: False）、建立 IIS 網站或應用程式、**設定密鑰環境變數**（在應用程式集區進階設定 → 處理序模型 → 載入使用者設定檔 = True、環境變數加入 `Jwt__SecretKey=<your-key>`）、發布專案（`dotnet publish -c Release -o <path>`）、設定 HTTPS 繫結與 SSL 憑證、重啟應用程式集區、疑難排解（啟用 stdout log、檢查 Windows Event Viewer）

### 30. 撰寫 Azure Key Vault 遷移文件

建立 `docs/Azure-KeyVault-Migration.md`，說明：建立 Azure Key Vault、新增密鑰（名稱使用 `Jwt--SecretKey` 格式）、安裝 `Azure.Extensions.AspNetCore.Configuration.Secrets` 與 `Azure.Identity` 套件、在 `Program.cs` 加入 `builder.Configuration.AddAzureKeyVault(new Uri("https://<vault-name>.vault.azure.net/"), new DefaultAzureCredential())`、設定 Managed Identity（Azure App Service）、本機開發使用 Azure CLI (`az login`) 或 Visual Studio 認證

### 31. 建立完整 README

建立 `README.md` 包含：專案簡介與核心特性（Refresh Token Rotation、一次性使用、重用偵測、30 秒寬限期、Token Family 追蹤、自動撤銷）、技術棧（ASP.NET Core 8、JWT、Serilog、xUnit、BenchmarkDotNet）、快速開始（先決條件 .NET 8 SDK、git clone、執行密鑰生成、dotnet run）、API 文件（端點列表與範例請求）、Swagger UI 使用流程、測試執行指令、基準測試執行、專案結構說明、部署指引（IIS、Azure）、貢獻指南

### 32. 建立生產環境檢查清單

建立 `docs/Production-Checklist.md`，包含檢查項目：密鑰管理（✅ 使用 Key Vault 或環境變數、❌ 不在 appsettings.json）、HTTPS（✅ UseHttpsRedirection、✅ HSTS）、CORS（✅ 限制 AllowedOrigins）、日誌（✅ 移除敏感資訊、✅ 結構化日誌）、Token 設定（✅ 15 分鐘 Access Token、✅ 14 天 Refresh Token）、密碼雜湊（✅ IterationCount 100000）、錯誤處理（✅ 不洩漏內部資訊）、監控（✅ Application Insights 或 ELK）

## 進一步考量

無，計劃已完整涵蓋所有需求，準備開始實作。
