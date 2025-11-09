# JWT 認證系統 - 前端

這是一個使用 Vite + React + TypeScript + Tailwind CSS 建立的 JWT 認證系統前端應用程式。

## 技術堆疊

- **框架**: React 19.1.1
- **建構工具**: Vite 7.1.7
- **語言**: TypeScript 5.9.3
- **路由**: React Router DOM v7
- **狀態管理**: Zustand
- **HTTP 客戶端**: Axios
- **樣式**: Tailwind CSS
- **工具**: clsx, tailwind-merge

## 功能特性

### 認證功能

- ✅ 使用者登入 (JWT Token)
- ✅ 自動 Token 重新整理 (Axios 攔截器)
- ✅ Token 儲存 (Access Token 在記憶體, Refresh Token 在 SessionStorage)
- ✅ 安全登出 (清除所有 Token)
- ✅ 受保護路由 (ProtectedRoute)
- ✅ 管理員路由保護 (AdminRoute)

### 使用者功能

- ✅ 儀表板 - 顯示使用者資訊和最近活動
- ✅ 個人資料 - 檢視和編輯個人資訊
- ✅ 活動記錄 - 檢視最近的系統活動

### 管理員功能 (需要 Admin 角色)

- ✅ 使用者管理 - 檢視所有使用者
- ✅ Token 撤銷 - 撤銷特定使用者的所有 Token
- ✅ 系統統計 - 檢視系統使用情況
- ✅ 快取管理 - 清除系統快取

## 專案結構

```
frontend/
├── src/
│   ├── components/          # React 元件
│   │   ├── auth/           # 認證相關元件
│   │   │   ├── LoginForm.tsx
│   │   │   ├── ProtectedRoute.tsx
│   │   │   └── AdminRoute.tsx
│   │   ├── layout/         # 佈局元件
│   │   │   ├── Header.tsx
│   │   │   └── Layout.tsx
│   │   └── ui/             # 可重用 UI 元件
│   │       ├── Button.tsx
│   │       ├── Input.tsx
│   │       ├── Card.tsx
│   │       └── Alert.tsx
│   ├── hooks/              # 自定義 React Hooks
│   │   ├── useAuth.ts      # 認證 Hook
│   │   └── useApi.ts       # API 呼叫 Hook
│   ├── pages/              # 頁面元件
│   │   ├── LoginPage.tsx
│   │   ├── DashboardPage.tsx
│   │   ├── ProfilePage.tsx
│   │   └── admin/
│   │       ├── AdminUsersPage.tsx
│   │       └── AdminStatsPage.tsx
│   ├── router/             # 路由設定
│   │   └── index.tsx
│   ├── services/           # API 服務層
│   │   ├── api.ts          # Axios 實例和攔截器
│   │   ├── auth.service.ts # 認證服務
│   │   ├── user.service.ts # 使用者服務
│   │   └── admin.service.ts # 管理員服務
│   ├── store/              # Zustand 狀態管理
│   │   └── authStore.ts    # 認證狀態
│   ├── types/              # TypeScript 型別定義
│   │   └── api.types.ts    # API 型別
│   ├── utils/              # 工具函式
│   │   └── index.ts        # cn(), formatDateTime(), etc.
│   ├── App.tsx             # 主應用程式元件
│   ├── main.tsx            # 應用程式進入點
│   ├── index.css           # 全域樣式
│   └── vite-env.d.ts       # Vite 環境變數型別
├── index.html              # HTML 範本
├── vite.config.ts          # Vite 設定
├── tailwind.config.js      # Tailwind CSS 設定
├── postcss.config.js       # PostCSS 設定
├── tsconfig.json           # TypeScript 設定
├── .env                    # 環境變數
└── package.json            # 專案依賴
```

## 安裝與執行

### 前置需求

- Node.js 18+ 或 20+
- npm 或 yarn

### 安裝步驟

1. 安裝依賴套件:

```bash
cd frontend
npm install
```

2. 設定環境變數:
   複製 `.env` 檔案並根據需要修改:

```bash
cp .env.local.example .env.local
```

預設 API URL: `https://localhost:7198/api/v1`

3. 啟動開發伺服器:

```bash
npm run dev
```

應用程式會在 `http://localhost:5173` 啟動

### 建構生產版本

```bash
npm run build
```

建構後的檔案會在 `dist/` 目錄

### 預覽生產版本

```bash
npm run preview
```

## 設定說明

### 環境變數

在 `.env` 或 `.env.local` 檔案中設定:

```env
# API Base URL
VITE_API_URL=https://localhost:7198/api/v1
```

### Vite 設定

`vite.config.ts` 已設定代理,在開發模式下自動處理 CORS:

```typescript
server: {
  port: 5173,
  proxy: {
    '/api': {
      target: 'https://localhost:7198',
      changeOrigin: true,
      secure: false,
    },
  },
}
```

## API 整合

### 自動 Token 重新整理

Axios 攔截器會自動處理 Token 重新整理:

1. 當收到 401 回應時
2. 自動使用 Refresh Token 取得新的 Access Token
3. 重試原始請求
4. 如果重新整理失敗,重導向到登入頁面

### Token 儲存策略

- **Access Token**: 儲存在記憶體變數中 (防止 XSS 攻擊)
- **Refresh Token**: 儲存在 SessionStorage 中 (關閉瀏覽器即清除)

### API 服務

所有 API 呼叫都透過服務層抽象:

```typescript
// 使用範例
import { authService } from './services/auth.service';

// 登入
await authService.login({ username: 'admin', password: 'password' });

// 登出
await authService.logout();

// 取得當前使用者
const user = await authService.getCurrentUser();
```

## 使用說明

### 登入

預設測試帳號 (由後端 API 提供):

**管理員帳號:**

- 使用者名稱: `admin`
- 密碼: `Admin@123`

**一般使用者:**

- 使用者名稱: `user`
- 密碼: `User@123`

### 路由

- `/login` - 登入頁面
- `/dashboard` - 儀表板 (需要認證)
- `/profile` - 個人資料頁面 (需要認證)
- `/admin/users` - 使用者管理 (需要 Admin 角色)
- `/admin/stats` - 系統統計 (需要 Admin 角色)

### 自定義 Hooks

#### useAuth

提供認證相關功能:

```typescript
const { user, isLoading, error, isAuthenticated, isAdmin, login, logout } = useAuth();
```

#### useApi

處理 API 呼叫的載入狀態和錯誤:

```typescript
const { data, isLoading, error, execute } = useApi(apiFunction);
```

#### useApiAuto

自動執行的 API 呼叫:

```typescript
const { data, isLoading, error } = useApiAuto(() => userService.getProfile(), []);
```

## 開發指南

### 建立新頁面

1. 在 `src/pages/` 建立新元件
2. 在 `src/router/index.tsx` 加入路由
3. 如需保護路由,使用 `ProtectedRoute` 或 `AdminRoute`

### 建立新服務

1. 在 `src/services/` 建立新服務檔案
2. 匯出函式,使用已設定的 `api` 實例
3. 在 `src/types/api.types.ts` 定義相關型別

### UI 元件

專案使用 Tailwind CSS + shadcn/ui 風格的元件。所有 UI 元件位於 `src/components/ui/`:

- `Button` - 按鈕元件 (多種變體和大小)
- `Input` - 輸入框元件 (支援標籤和錯誤訊息)
- `Card` - 卡片容器
- `Alert` - 警告/通知元件

## 安全考量

1. **XSS 防護**: Access Token 儲存在記憶體,不暴露在 localStorage
2. **CSRF 防護**: 使用 Bearer Token 而非 Cookie
3. **Token 輪替**: 使用 Refresh Token 輪替機制
4. **HTTPS**: 生產環境必須使用 HTTPS
5. **CORS**: 後端已設定 CORS 僅允許 `http://localhost:5173`

## 疑難排解

### CORS 錯誤

確保後端 API 的 CORS 設定包含前端 URL: `http://localhost:5173`

### Token 無效錯誤

檢查後端 API 是否正在執行,並確認 JWT 設定正確

### 開發伺服器連線錯誤

確認 `.env` 檔案中的 `VITE_API_URL` 設定正確

## 授權

MIT License
