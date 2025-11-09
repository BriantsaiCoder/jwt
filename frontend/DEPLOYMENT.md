# 前端部署說明

## ❓ 常見問題：.env 檔案會被放入 dist 嗎？

### 答案：**不會！**

---

## 🔍 Vite 環境變數的工作原理

### 1️⃣ **建構時（Build Time）處理**

Vite 在執行 `npm run build` 時：

```bash
# 步驟 1: 讀取環境變數
讀取 .env.production → VITE_API_URL=https://api.yourdomain.com/api/v1

# 步驟 2: 替換程式碼中的變數
將所有 import.meta.env.VITE_API_URL
替換為 "https://api.yourdomain.com/api/v1"

# 步驟 3: 打包成 JavaScript
產生 dist/assets/index-abc123.js
```

### 📝 **原始程式碼（src/services/api.ts）：**

```typescript
const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:7198/api/v1';
```

### 📦 **建構後的程式碼（dist/assets/index-abc123.js）：**

```javascript
// 環境變數已被直接寫入程式碼
const API_BASE_URL = 'https://api.yourdomain.com/api/v1';
```

### ✅ **結果：**

- `.env` 檔案**不會**被複製到 `dist/` 目錄
- 環境變數的值已經**硬編碼**到 JavaScript 檔案中
- 部署後**無法**透過修改 `.env` 來改變 API 網址

---

## 🚀 正確的部署流程

### **步驟 1：建構前設定環境變數**

編輯 `.env.production`：

```env
VITE_API_URL=https://yourserver.com/api/v1
```

### **步驟 2：執行建構**

```bash
npm run build
# 或
npm run build:production
```

### **步驟 3：檢查建構輸出**

```
dist/
├── index.html
├── assets/
│   ├── index-abc123.js    ← API URL 已寫入此檔案
│   └── index-xyz789.css
└── favicon.ico
```

### **步驟 4：部署 dist 目錄**

將整個 `dist/` 目錄部署到 Web 伺服器。

---

## 📋 不同部署環境的處理方式

### 場景 1：單一生產環境

```bash
# 1. 設定 API 網址
編輯 .env.production:
VITE_API_URL=https://api.yourdomain.com/api/v1

# 2. 建構
npm run build

# 3. 部署 dist/ 到生產伺服器
```

### 場景 2：多個環境（開發、測試、生產）

```bash
# 測試環境
npm run build:staging    # 使用 .env.staging
部署到測試伺服器

# 生產環境
npm run build:production # 使用 .env.production
部署到生產伺服器
```

### 場景 3：需要不同後端的多個部署

**問題：** 同一份程式碼要部署到不同伺服器，但後端 API 網址不同

**解決方案 A：建構多個版本**

```bash
# 客戶 A 的部署
echo "VITE_API_URL=https://api.client-a.com/api/v1" > .env.production
npm run build
mv dist dist-client-a

# 客戶 B 的部署
echo "VITE_API_URL=https://api.client-b.com/api/v1" > .env.production
npm run build
mv dist dist-client-b
```

**解決方案 B：使用建構參數**

修改 `package.json`：

```json
{
  "scripts": {
    "build:client-a": "VITE_API_URL=https://api.client-a.com/api/v1 vite build",
    "build:client-b": "VITE_API_URL=https://api.client-b.com/api/v1 vite build"
  }
}
```

---

## ⚠️ 重要注意事項

### ❌ **無法在部署後修改 API 網址**

```bash
# ❌ 這樣做無效！
部署後修改 .env.production → 不會生效
因為環境變數已在建構時寫入程式碼
```

### ✅ **如需修改，必須重新建構**

```bash
# ✅ 正確做法
1. 修改 .env.production
2. npm run build
3. 重新部署 dist/
```

### 💡 **驗證建構結果**

```bash
# 建構後檢查 API URL
npm run preview

# 或在瀏覽器開發者工具中：
console.log('API URL:', import.meta.env.VITE_API_URL)
```

---

## 🏢 IIS 部署範例

### **步驟 1：準備後端**

後端部署在 `https://yourserver.com/api/`

### **步驟 2：設定前端環境變數**

```env
# .env.production
VITE_API_URL=https://yourserver.com/api/v1
```

### **步驟 3：建構前端**

```bash
npm run build
```

### **步驟 4：部署到 IIS**

```
IIS 網站結構：
C:\inetpub\wwwroot\
├── api/                          ← 後端 (ASP.NET Core)
│   ├── JwtAuthApi.dll
│   └── web.config
└── app/                          ← 前端 (dist/ 的內容)
    ├── index.html
    ├── assets/
    │   ├── index-abc123.js
    │   └── index-xyz789.css
    └── favicon.ico
```

### **步驟 5：IIS 網站設定**

```
網站繫結：
- https://yourserver.com

應用程式：
- / → 前端（app 目錄）
- /api → 後端（api 目錄）
```

### **步驟 6：驗證**

訪問 `https://yourserver.com`，前端會自動連接到 `https://yourserver.com/api/v1`

---

## 🔧 進階：執行時期動態設定（選用）

如果**真的需要**在部署後動態修改 API 網址，可以使用以下方法：

### 方法 1：使用 config.json

**步驟 1：建立公開的設定檔**

```json
// public/config.json
{
  "apiUrl": "https://api.yourdomain.com/api/v1"
}
```

**步驟 2：在應用程式啟動時載入**

```typescript
// src/services/api.ts
let API_BASE_URL = '';

// 啟動時載入設定
async function loadConfig() {
  const response = await fetch('/config.json');
  const config = await response.json();
  API_BASE_URL = config.apiUrl;
}

// 在 main.tsx 中呼叫
await loadConfig();
```

**優點：** 部署後可修改 `config.json`，不需重新建構

**缺點：**

- 增加初始載入時間
- 需要額外的 HTTP 請求
- 設定檔暴露在公開目錄

### 方法 2：使用伺服器端渲染（SSR）

使用 Next.js、Nuxt.js 等框架，可在伺服器端注入環境變數。

---

## 📊 建構與部署對照表

| 項目                   | 建構時                  | 部署後                      |
| ---------------------- | ----------------------- | --------------------------- |
| **讀取 .env**          | ✅ 是                   | ❌ 否                       |
| **環境變數寫入程式碼** | ✅ 是                   | -                           |
| **.env 檔案在 dist/**  | ❌ 否                   | ❌ 否                       |
| **可修改 API URL**     | ✅ 修改 .env 後重新建構 | ❌ 無法修改（已寫入程式碼） |
| **dist/ 內容**         | 產生靜態檔案            | 直接部署這些檔案            |

---

## ✅ 最佳實踐

1. **建構前確認環境變數正確**

   - 檢查 `.env.production` 中的 API URL
   - 確保後端 URL 可從客戶端訪問

2. **為不同環境建立不同的 .env 檔案**

   - `.env.development` - 本機開發
   - `.env.staging` - 測試環境
   - `.env.production` - 生產環境

3. **使用建構指令區分環境**

   ```bash
   npm run build:staging
   npm run build:production
   ```

4. **建構後進行測試**

   ```bash
   npm run preview:production
   ```

   在本機預覽生產版本，確認 API 連接正確

5. **記錄每次部署的設定**
   - 記錄建構時使用的環境變數
   - 記錄部署的時間和版本

---

## 🔍 故障排除

### 問題 1：部署後 API 請求 404

**原因：** API URL 設定錯誤

**檢查：**

```bash
# 開啟瀏覽器開發者工具 → Network
# 查看實際請求的 URL 是否正確
```

**解決：**

```bash
# 1. 修正 .env.production
# 2. 重新建構
npm run build
# 3. 重新部署
```

### 問題 2：CORS 錯誤

**原因：** 後端 CORS 設定未包含前端網址

**解決：**
修改後端 `Program.cs`：

```csharp
policy.WithOrigins("https://yourfrontend.com")
```

### 問題 3：建構後環境變數未生效

**檢查：**

1. 變數名稱是否以 `VITE_` 開頭
2. 是否在建構前修改了 `.env.production`
3. 是否重新執行了 `npm run build`

---

## 📚 總結

1. ✅ `.env` 檔案**不會**被複製到 `dist/`
2. ✅ 環境變數在**建構時**就寫入程式碼
3. ✅ 部署後**無法**透過修改 `.env` 改變設定
4. ✅ 如需修改 API URL，必須**重新建構**
5. ✅ 使用不同的 `.env` 檔案管理不同環境

**核心概念：** Vite 是靜態網站產生器，環境變數在建構時就被替換成實際的值，部署的是已編譯的靜態檔案。
