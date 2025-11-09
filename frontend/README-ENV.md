# 前端環境變數管理指南

## 📁 環境變數檔案說明

### 1. `.env` - 預設環境變數

- 所有環境都會載入
- 可以設定預設值
- **會被提交到版本控制**

### 2. `.env.local` - 本機覆寫

- 用於本機特殊設定
- 優先級高於 `.env`
- **不會被提交到版本控制**（已加入 .gitignore）

### 3. `.env.development` - 開發環境

- `npm run dev` 時自動載入
- 本機開發專用設定

### 4. `.env.production` - 生產環境

- `npm run build` 時自動載入
- 生產部署專用設定

### 5. `.env.staging` - 測試環境

- `npm run build:staging` 時載入
- 測試伺服器專用設定

### 6. `.env.local.example` - 範例檔案

- 提供給團隊成員參考的範例
- 複製為 `.env.local` 並修改

---

## 🔄 載入優先順序（從高到低）

```
.env.production.local
.env.production
.env.local
.env
```

---

## 🚀 使用方式

### 開發環境（本機開發）

```bash
# 使用 .env.development
npm run dev
```

後端 API 網址：`https://localhost:7198/api/v1`

### 生產環境建構

```bash
# 使用 .env.production
npm run build

# 或明確指定
npm run build:production
```

建構後的 `dist/` 目錄可部署到 IIS、Nginx 等 Web 伺服器。

### 測試環境建構

```bash
# 使用 .env.staging
npm run build:staging
```

### 預覽建構結果

```bash
# 預覽生產環境建構
npm run preview:production

# 預覽測試環境建構
npm run preview:staging
```

---

## 📝 部署前設定步驟

### 步驟 1：修改生產環境 API 網址

編輯 `.env.production`：

```env
# 範例 1: 使用網域名稱
VITE_API_URL=https://api.yourdomain.com/api/v1

# 範例 2: 使用伺服器 IP
VITE_API_URL=https://192.168.1.100/api/v1

# 範例 3: 後端與前端在同一伺服器
VITE_API_URL=https://yourserver.com/api/v1
```

### 步驟 2：建構生產版本

```bash
npm run build:production
```

### 步驟 3：部署 `dist/` 目錄

將 `dist/` 目錄中的所有檔案部署到：

- **IIS**：網站根目錄（例如：`C:\inetpub\wwwroot\jwt-frontend`）
- **Nginx**：`/var/www/html/`
- **Apache**：`/var/www/html/`

---

## 🔍 環境變數驗證

在程式碼中檢查 API URL：

```typescript
// 在 src/services/api.ts
console.log('API Base URL:', import.meta.env.VITE_API_URL);
console.log('Mode:', import.meta.env.MODE);
```

瀏覽器開發者工具中會顯示：

```
API Base URL: https://api.yourdomain.com/api/v1
Mode: production
```

---

## 🏢 部署場景範例

### 場景 1：前後端分離部署

```
前端：https://app.yourdomain.com
後端：https://api.yourdomain.com

.env.production:
VITE_API_URL=https://api.yourdomain.com/api/v1
```

後端需設定 CORS：

```csharp
policy.WithOrigins("https://app.yourdomain.com")
```

### 場景 2：前後端同一伺服器

```
網站：https://yourserver.com
前端：https://yourserver.com/
後端：https://yourserver.com/api/v1

.env.production:
VITE_API_URL=https://yourserver.com/api/v1
```

IIS 設定：

- 前端：網站根目錄
- 後端：應用程式（別名：api）

### 場景 3：使用伺服器 IP

```
伺服器 IP：192.168.1.100

.env.production:
VITE_API_URL=https://192.168.1.100/api/v1
```

需要有效的 SSL 憑證或客戶端接受自簽憑證。

---

## ⚠️ 重要注意事項

### 1. 環境變數必須以 `VITE_` 開頭

❌ 錯誤：

```env
API_URL=https://api.com
```

✅ 正確：

```env
VITE_API_URL=https://api.com
```

### 2. 建構時嵌入，無法動態變更

環境變數在**建構時**就被打包到程式碼中，部署後**無法修改**。

如需在部署後修改，必須：

1. 修改 `.env.production`
2. 重新執行 `npm run build`
3. 重新部署 `dist/` 目錄

### 3. 不要在環境變數中儲存敏感資訊

❌ 不要儲存：

- API 密鑰
- 資料庫密碼
- JWT Secret

這些資訊會暴露在前端程式碼中！

### 4. 確保 CORS 設定一致

後端的 CORS `AllowOrigins` 必須包含前端的網址。

---

## 🔧 故障排除

### 問題 1：API 請求失敗（CORS 錯誤）

**檢查：**

1. `.env.production` 中的 API URL 是否正確
2. 後端 CORS 設定是否包含前端網址
3. 瀏覽器開發者工具 → Network → 檢查實際請求的 URL

### 問題 2：環境變數未生效

**解決：**

1. 確認變數名稱以 `VITE_` 開頭
2. 修改後重新執行 `npm run build`
3. 清除瀏覽器快取

### 問題 3：建構後無法連接後端

**檢查：**

1. 後端服務是否正在運行
2. 防火牆是否開放對應端口
3. SSL 憑證是否有效
4. 後端 URL 是否可從客戶端訪問

---

## 📚 參考資料

- [Vite 環境變數文件](https://vitejs.dev/guide/env-and-mode.html)
- `.env` 檔案優先順序說明
- 建構與部署最佳實踐
