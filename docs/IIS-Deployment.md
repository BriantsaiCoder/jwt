# IIS 部署指引

本文件說明如何將 JwtAuthApi 部署到 Windows IIS 伺服器。

## 先決條件

1. **Windows Server** (2016 或更新版本)
2. **IIS 10.0** 或更新版本
3. **.NET 9.0 Hosting Bundle**

## 步驟 1：安裝 .NET Hosting Bundle

1. 下載 [.NET 9.0 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/9.0)
2. 執行安裝程式
3. 重新啟動 IIS：
   ```cmd
   net stop was /y
   net start w3svc
   ```

## 步驟 2：建立 IIS 應用程式集區

1. 開啟 **IIS 管理員**
2. 展開伺服器節點，右鍵點選 **應用程式集區** → **新增應用程式集區**
3. 設定如下：
   - **名稱**：`JwtAuthApi_Pool`
   - **.NET CLR 版本**：**無受控程式碼**（重要！）
   - **受控管線模式**：整合式
   - **啟動應用程式集區**：勾選
4. 點選新建立的應用程式集區 → **進階設定**：
   - **處理序模型** → **識別**：ApplicationPoolIdentity
   - **處理序模型** → **載入使用者設定檔**：**True**（重要！讓應用程式可讀取環境變數）
   - **一般** → **啟用 32 位元應用程式**：False
   - **資源回收** → **固定時間間隔（分鐘）**：1740（29 小時，避免在高峰時段重啟）

## 步驟 3：發布應用程式

在開發機器上執行：

```bash
cd /path/to/JwtAuthApi
dotnet publish -c Release -o ./publish
```

或使用 Visual Studio：

1. 右鍵點選專案 → **發佈**
2. 選擇 **資料夾** 目標
3. 設定輸出路徑
4. 點選 **發佈**

## 步驟 4：設定密鑰環境變數

### 方法 A：透過 IIS 管理員（推薦）

1. 開啟 **IIS 管理員**
2. 選擇應用程式集區 `JwtAuthApi_Pool`
3. 右鍵 → **進階設定**
4. 在 **處理序模型** 區段，找到 **環境變數**
5. 點選 **...** 按鈕
6. 新增環境變數：
   - **名稱**：`Jwt__SecretKey`
   - **值**：`你的64-byte Base64編碼密鑰`

### 方法 B：透過 web.config（不建議用於生產環境）

在 `web.config` 中加入：

```xml
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <aspNetCore processPath="dotnet" arguments=".\JwtAuthApi.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="Jwt__SecretKey" value="你的密鑰" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

⚠️ **注意**：此方法會將密鑰明文儲存在檔案中，僅適用於開發環境。

### 產生新密鑰

使用專案內建的密鑰產生器：

```bash
dotnet run --project JwtAuthApi -- generate-key
```

或使用 PowerShell：

```powershell
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)
Write-Host "產生的密鑰: $key"
```

## 步驟 5：建立 IIS 網站或應用程式

### 選項 A：建立新網站

1. 在 IIS 管理員中，右鍵點選 **網站** → **新增網站**
2. 設定：
   - **網站名稱**：JwtAuthApi
   - **應用程式集區**：選擇 `JwtAuthApi_Pool`
   - **實體路徑**：選擇發佈目錄（例如 `C:\inetpub\wwwroot\JwtAuthApi`）
   - **繫結**：
     - 類型：https
     - IP 位址：全部未指派
     - 連接埠：443
     - SSL 憑證：選擇您的 SSL 憑證
3. 點選 **確定**

### 選項 B：建立應用程式（在現有網站下）

1. 展開現有網站，右鍵點選 → **新增應用程式**
2. 設定：
   - **別名**：api（URL 會是 `https://yourdomain.com/api`）
   - **應用程式集區**：選擇 `JwtAuthApi_Pool`
   - **實體路徑**：選擇發佈目錄
3. 點選 **確定**

## 步驟 6：設定 HTTPS 與 SSL 憑證

1. 在 IIS 管理員中選擇網站
2. 雙擊 **繫結**
3. 新增或編輯 HTTPS 繫結：
   - 類型：https
   - 連接埠：443
   - SSL 憑證：選擇有效的 SSL 憑證
4. 如果需要自動 HTTP 重導向到 HTTPS，應用程式已內建此功能（`app.UseHttpsRedirection()`）

### 取得免費 SSL 憑證（Let's Encrypt）

使用 [win-acme](https://www.win-acme.com/)：

```cmd
wacs.exe --target manual --host yourdomain.com --installation iis --store centralssl
```

## 步驟 7：設定目錄權限

1. 右鍵點選發佈目錄 → **內容** → **安全性**
2. 確保以下使用者有讀取權限：
   - `IIS AppPool\JwtAuthApi_Pool`（應用程式集區識別）
   - `IIS_IUSRS`
3. 對 `logs` 目錄賦予寫入權限（Serilog 需要）

## 步驟 8：驗證部署

1. 重新啟動應用程式集區：

   ```cmd
   %windir%\system32\inetsrv\appcmd recycle apppool "JwtAuthApi_Pool"
   ```

2. 瀏覽器訪問：

   - Swagger UI：`https://yourdomain.com/swagger`
   - 健康檢查：`https://yourdomain.com/api/v1/weatherforecast`（需要先登入）

3. 測試登入：
   ```bash
   curl -X POST https://yourdomain.com/api/v1/auth/login \
     -H "Content-Type: application/json" \
     -d '{"username":"admin","password":"Admin@123"}'
   ```

## 步驟 9：設定生產環境組態

建立或編輯 `appsettings.Production.json`：

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "C:\\Logs\\JwtAuthApi\\log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  },
  "AllowedHosts": "yourdomain.com"
}
```

確保設定環境變數 `ASPNETCORE_ENVIRONMENT=Production`：

在應用程式集區環境變數中加入：

- **名稱**：`ASPNETCORE_ENVIRONMENT`
- **值**：`Production`

## 疑難排解

### 應用程式無法啟動

1. **啟用 stdout 日誌**：編輯 `web.config`

   ```xml
   <aspNetCore ... stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" ...>
   ```

2. **檢查 Windows 事件檢視器**：

   - 開啟 **事件檢視器**
   - **Windows 記錄檔** → **應用程式**
   - 尋找來源為 **IIS AspNetCore Module** 的錯誤

3. **確認 .NET Hosting Bundle 已安裝**：

   ```cmd
   dotnet --list-runtimes
   ```

4. **檢查應用程式集區設定**：
   - .NET CLR 版本必須是「無受控程式碼」
   - 載入使用者設定檔必須為 True

### 500.30 錯誤 - ASP.NET Core 應用程式無法啟動

通常是因為：

- 缺少 .NET Hosting Bundle
- 環境變數設定錯誤
- 密鑰未正確設定

檢查 stdout 日誌檔案以取得詳細錯誤訊息。

### 401 未授權錯誤

- 確認密鑰已正確設定在環境變數中
- 確認環境變數名稱使用雙底線：`Jwt__SecretKey`（不是單一底線）

### 應用程式執行緩慢

1. **啟用 Response Compression**（已在程式碼中啟用）
2. **調整應用程式集區設定**：
   - 增加佇列長度：1000 → 5000
   - 調整工作者處理序數量
3. **使用 Application Initialization**：
   在 `web.config` 中加入：
   ```xml
   <applicationInitialization>
     <add initializationPage="/api/v1/weatherforecast" />
   </applicationInitialization>
   ```

## 監控與維護

### 設定應用程式監控

建議整合：

- **Application Insights**（Azure）
- **New Relic**
- **Datadog**

### 日誌管理

- 定期清理舊日誌（Serilog 已設定保留 30 天）
- 設定日誌等級為 `Information` 或 `Warning`（避免 Debug 日誌在生產環境）
- 監控日誌檔案大小

### 備份策略

定期備份：

1. 應用程式目錄
2. `appsettings.Production.json`
3. IIS 設定（使用 `appcmd` 匯出）
   ```cmd
   %windir%\system32\inetsrv\appcmd list site "JwtAuthApi" /config /xml > JwtAuthApi-backup.xml
   ```

## 更新部署

1. 停止應用程式集區
2. 備份現有檔案
3. 複製新的發佈檔案
4. 重新啟動應用程式集區
5. 驗證應用程式運作正常

建議使用自動化部署工具（如 Azure DevOps、GitHub Actions）進行零停機部署。

## 安全性檢查清單

- ✅ 使用 HTTPS（強制 HTTP 重導向）
- ✅ SSL 憑證有效且未過期
- ✅ 密鑰儲存在環境變數（不在 appsettings.json）
- ✅ 設定 HSTS（已在程式碼中啟用）
- ✅ 限制 CORS（在生產環境設定允許的來源）
- ✅ 定期更新 .NET Runtime 與套件
- ✅ 監控安全漏洞
- ✅ 實作速率限制（若需要）
- ✅ 定期審查日誌

## 相關資源

- [ASP.NET Core 部署到 IIS](https://docs.microsoft.com/aspnet/core/host-and-deploy/iis/)
- [.NET Hosting Bundle](https://dotnet.microsoft.com/download/dotnet)
- [IIS 管理文件](https://docs.microsoft.com/iis/)
