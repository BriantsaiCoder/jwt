# Azure Key Vault 遷移指引

本文件說明如何將 JWT 密鑰從 User Secrets 或環境變數遷移到 Azure Key Vault，以提供企業級的密鑰管理。

## 為什麼使用 Azure Key Vault？

- **集中式密鑰管理**：所有密鑰在雲端統一管理
- **存取控制**：使用 Azure AD 與 RBAC 控制誰可以存取密鑰
- **審計日誌**：記錄所有密鑰存取活動
- **自動輪換**：支援定期自動更新密鑰
- **合規性**：符合 FIPS 140-2、HIPAA、PCI DSS 等標準

## 步驟 1：建立 Azure Key Vault

### 使用 Azure Portal

1. 登入 [Azure Portal](https://portal.azure.com)
2. 點選 **建立資源** → 搜尋 **Key Vault**
3. 點選 **建立**
4. 填寫資訊：
   - **訂用帳戶**：選擇您的訂用帳戶
   - **資源群組**：建立新的或選擇現有的
   - **金鑰保存庫名稱**：`jwtauth-keyvault`（必須全域唯一）
   - **區域**：選擇靠近您應用程式的區域
   - **定價層**：標準（Standard）
5. 在 **存取原則** 標籤：
   - 啟用 **Azure 虛擬機器部署**
   - 啟用 **Azure Resource Manager 範本部署**
6. 點選 **檢閱 + 建立** → **建立**

### 使用 Azure CLI

```bash
# 登入 Azure
az login

# 建立資源群組（如果不存在）
az group create --name JwtAuthRG --location eastus

# 建立 Key Vault
az keyvault create \
  --name jwtauth-keyvault \
  --resource-group JwtAuthRG \
  --location eastus \
  --enable-rbac-authorization false
```

## 步驟 2：新增密鑰到 Key Vault

### 使用 Azure Portal

1. 進入您的 Key Vault
2. 在左側選單點選 **密碼** (Secrets)
3. 點選 **+ 產生/匯入**
4. 填寫資訊：
   - **上傳選項**：手動
   - **名稱**：`Jwt--SecretKey`（注意：使用雙破折號 `--` 取代組態中的 `:`）
   - **值**：貼上您的 Base64 編碼密鑰
   - **內容類型**：`text/plain`（選填）
   - **啟用**：是
5. 點選 **建立**

### 使用 Azure CLI

```bash
# 產生新密鑰（64 bytes Base64）
SECRET_KEY=$(openssl rand -base64 64 | tr -d '\n')

# 新增密鑰到 Key Vault
az keyvault secret set \
  --vault-name jwtauth-keyvault \
  --name "Jwt--SecretKey" \
  --value "$SECRET_KEY"

# 驗證密鑰已儲存
az keyvault secret show \
  --vault-name jwtauth-keyvault \
  --name "Jwt--SecretKey" \
  --query "value" \
  --output tsv
```

### 使用 PowerShell

```powershell
# 產生新密鑰
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$secretKey = [Convert]::ToBase64String($bytes)

# 新增密鑰到 Key Vault
$secretValue = ConvertTo-SecureString $secretKey -AsPlainText -Force
Set-AzKeyVaultSecret `
  -VaultName "jwtauth-keyvault" `
  -Name "Jwt--SecretKey" `
  -SecretValue $secretValue
```

## 步驟 3：設定應用程式存取權限

### 選項 A：使用 Managed Identity（推薦，適用於 Azure 託管）

#### 在 Azure App Service 上啟用 Managed Identity

1. 在 Azure Portal 進入您的 App Service
2. 左側選單 → **身分識別** (Identity)
3. **系統指派** 標籤 → **狀態** → **開啟**
4. 點選 **儲存**
5. 記下 **物件識別碼** (Object ID)

#### 授予 Managed Identity 存取 Key Vault 的權限

```bash
# 取得 App Service 的 Managed Identity Object ID
MANAGED_IDENTITY_OBJECT_ID=$(az webapp identity show \
  --name your-app-service-name \
  --resource-group JwtAuthRG \
  --query principalId \
  --output tsv)

# 授予密碼讀取權限
az keyvault set-policy \
  --name jwtauth-keyvault \
  --object-id $MANAGED_IDENTITY_OBJECT_ID \
  --secret-permissions get list
```

或使用 Azure Portal：

1. 進入 Key Vault → **存取原則** (Access policies)
2. 點選 **+ 新增存取原則**
3. **密碼權限**：選擇 **取得** 和 **列出**
4. **選取主體**：搜尋並選擇您的 App Service
5. 點選 **新增** → **儲存**

### 選項 B：使用服務主體（適用於本機或非 Azure 環境）

```bash
# 建立服務主體
SP_INFO=$(az ad sp create-for-rbac --name "JwtAuthApiSP" --sdk-auth)

# 取得服務主體的 Object ID
SP_OBJECT_ID=$(echo $SP_INFO | jq -r '.clientId' | xargs az ad sp show --id | jq -r '.objectId')

# 授予權限
az keyvault set-policy \
  --name jwtauth-keyvault \
  --object-id $SP_OBJECT_ID \
  --secret-permissions get list

# 儲存憑證資訊（用於本機開發）
echo $SP_INFO > azure-credentials.json
```

## 步驟 4：安裝 NuGet 套件

在 `JwtAuthApi` 專案中安裝必要的套件：

```bash
cd JwtAuthApi
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

## 步驟 5：更新 Program.cs

在 `Program.cs` 中加入 Azure Key Vault 組態：

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

// 在生產環境中載入 Azure Key Vault
if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVault:Name"] ?? "jwtauth-keyvault";
    var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");

    // 使用 DefaultAzureCredential 自動處理不同環境的驗證
    // - Azure 上使用 Managed Identity
    // - 本機開發使用 Azure CLI 或 Visual Studio 憑證
    builder.Configuration.AddAzureKeyVault(
        keyVaultUri,
        new DefaultAzureCredential());
}

// ... 其餘的程式碼
```

或使用更詳細的設定：

```csharp
if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVault:Name"];

    if (!string.IsNullOrEmpty(keyVaultName))
    {
        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            // 排除不需要的驗證方法以加快速度
            ExcludeEnvironmentCredential = false,
            ExcludeManagedIdentityCredential = false,
            ExcludeSharedTokenCacheCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeAzureCliCredential = false,
            ExcludeInteractiveBrowserCredential = true
        });

        builder.Configuration.AddAzureKeyVault(keyVaultUri, credential);

        builder.Services.AddSingleton<SecretClient>(sp =>
            new SecretClient(keyVaultUri, credential));
    }
}
```

## 步驟 6：更新 appsettings.Production.json

新增 Key Vault 組態：

```json
{
  "KeyVault": {
    "Name": "jwtauth-keyvault"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  }
}
```

## 步驟 7：本機開發設定

### 使用 Azure CLI 驗證

```bash
# 登入 Azure CLI
az login

# 驗證登入狀態
az account show

# 測試存取 Key Vault
az keyvault secret show \
  --vault-name jwtauth-keyvault \
  --name "Jwt--SecretKey" \
  --query "value"
```

### 在本機開發時使用 Key Vault

選項 1：在 `appsettings.Development.json` 中啟用 Key Vault

```json
{
  "KeyVault": {
    "Name": "jwtauth-keyvault"
  }
}
```

選項 2：繼續使用 User Secrets（推薦用於本機開發）

在 `Program.cs` 中只在生產環境啟用 Key Vault：

```csharp
if (builder.Environment.IsProduction())
{
    // 只在生產環境使用 Key Vault
    builder.Configuration.AddAzureKeyVault(...);
}
```

本機開發繼續使用：

```bash
dotnet user-secrets set "Jwt:SecretKey" "your-dev-secret-key"
```

## 步驟 8：驗證設定

### 本機驗證

1. 確保已執行 `az login`
2. 執行應用程式：
   ```bash
   dotnet run --environment Production
   ```
3. 檢查啟動日誌，確認沒有 Key Vault 相關錯誤

### Azure 驗證

1. 部署應用程式到 Azure App Service
2. 在 App Service 組態中設定：
   - **名稱**：`KeyVault__Name`
   - **值**：`jwtauth-keyvault`
3. 重新啟動 App Service
4. 檢查應用程式日誌（Application Insights 或 Log Stream）

## 疑難排解

### 錯誤：Azure.Identity.CredentialUnavailableException

**原因**：DefaultAzureCredential 找不到有效的驗證方法

**解決方案**：

1. 確認已執行 `az login`（本機）
2. 確認 Managed Identity 已啟用（Azure）
3. 檢查 Key Vault 存取原則

### 錯誤：403 Forbidden

**原因**：應用程式沒有存取 Key Vault 的權限

**解決方案**：

```bash
# 檢查存取原則
az keyvault show --name jwtauth-keyvault --query "properties.accessPolicies"

# 重新設定存取原則
az keyvault set-policy \
  --name jwtauth-keyvault \
  --object-id <your-object-id> \
  --secret-permissions get list
```

### 密鑰名稱中的冒號問題

Key Vault 不支援密鑰名稱中使用冒號 `:`。使用雙破折號 `--` 取代：

- ✅ 正確：`Jwt--SecretKey`
- ❌ 錯誤：`Jwt:SecretKey`

ASP.NET Core 組態系統會自動將 `--` 轉換為 `:`。

### 檢查 Managed Identity

```bash
# 檢查 App Service 的 Managed Identity
az webapp identity show \
  --name your-app-service-name \
  --resource-group JwtAuthRG

# 檢查 Managed Identity 的存取權限
az role assignment list \
  --assignee <principal-id> \
  --all
```

## 安全性最佳實踐

1. **最小權限原則**：只授予必要的權限（Get、List），不要給予 Set、Delete
2. **啟用軟刪除**：防止意外刪除密鑰
   ```bash
   az keyvault update \
     --name jwtauth-keyvault \
     --enable-soft-delete true \
     --enable-purge-protection true
   ```
3. **定期輪換密鑰**：設定密鑰過期時間並定期更新
4. **審計日誌**：啟用 Key Vault 診斷設定，記錄所有存取
5. **網路限制**：使用 Virtual Network 與 Private Endpoints 限制存取
6. **備份**：定期備份 Key Vault
   ```bash
   az keyvault backup start \
     --vault-name jwtauth-keyvault \
     --storage-resource-id <storage-account-id> \
     --blob-container-name backups
   ```

## 密鑰輪換策略

### 手動輪換

1. 產生新密鑰並加入新版本：

   ```bash
   NEW_KEY=$(openssl rand -base64 64 | tr -d '\n')
   az keyvault secret set \
     --vault-name jwtauth-keyvault \
     --name "Jwt--SecretKey" \
     --value "$NEW_KEY"
   ```

2. 重新啟動應用程式載入新密鑰

### 自動輪換（進階）

使用 Azure Functions 或 Logic Apps 定期輪換密鑰，並通知應用程式重新載入組態。

## 成本考量

- **Key Vault 標準層**：每 10,000 次交易約 $0.03 USD
- **密鑰儲存**：前 10,000 個密鑰免費
- **Managed Identity**：免費

估計每月成本：< $5 USD（小型應用程式）

## 相關資源

- [Azure Key Vault 文件](https://docs.microsoft.com/azure/key-vault/)
- [DefaultAzureCredential](https://docs.microsoft.com/dotnet/api/azure.identity.defaultazurecredential)
- [ASP.NET Core 中的 Azure Key Vault 組態提供者](https://docs.microsoft.com/aspnet/core/security/key-vault-configuration)
- [Managed Identity 概觀](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview)
