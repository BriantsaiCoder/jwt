# 生產環境檢查清單

在將 JwtAuthApi 部署到生產環境之前，請確認以下所有項目都已完成。

## 🔐 安全性

### 密鑰管理

- [ ] JWT 密鑰已從 User Secrets 或 appsettings.json 移除
- [ ] 密鑰儲存在安全位置（Azure Key Vault、環境變數、AWS Secrets Manager）
- [ ] 密鑰長度 >= 64 bytes（512 bits）
- [ ] 密鑰使用密碼學安全的隨機數產生器生成
- [ ] 密鑰未提交到版本控制系統（檢查 `.gitignore`）
- [ ] 設定密鑰定期輪換策略（建議每 90-180 天）
- [ ] 備份密鑰並安全儲存

### HTTPS 與傳輸安全

- [ ] 強制使用 HTTPS（`app.UseHttpsRedirection()` 已啟用）
- [ ] 設定有效的 SSL/TLS 憑證
- [ ] SSL 憑證未過期（設定到期提醒）
- [ ] 啟用 HSTS（HTTP Strict Transport Security）
- [ ] HSTS max-age >= 31536000（1 年）
- [ ] 考慮啟用 HSTS preload
- [ ] 停用不安全的 TLS 版本（TLS 1.0、1.1）
- [ ] 使用強加密套件（AES-GCM、ChaCha20）

### CORS 設定

- [ ] CORS 已正確設定（不使用 `AllowAnyOrigin`）
- [ ] 明確列出允許的來源（Origins）
- [ ] 限制允許的 HTTP 方法
- [ ] 限制允許的標頭
- [ ] 設定適當的 `Access-Control-Max-Age`
- [ ] 生產環境不允許 `http://localhost`

### 驗證與授權

- [ ] JWT Token 設定符合需求：
  - [ ] Access Token 過期時間：15-30 分鐘
  - [ ] Refresh Token 過期時間：7-30 天
  - [ ] 寬限期設定：30 秒
- [ ] 密碼雜湊迭代次數 >= 100,000
- [ ] 實作 Token 重用偵測與撤銷機制
- [ ] Token Family 追蹤正常運作
- [ ] 黑名單機制已測試
- [ ] 所有受保護端點都正確標註 `[Authorize]`
- [ ] 角色驗證正確實作（`[Authorize(Roles = "Admin")]`）
- [ ] Claims 驗證正確實作

### 輸入驗證

- [ ] 所有 API 端點都有輸入驗證
- [ ] 使用 Data Annotations（`[Required]`、`[StringLength]` 等）
- [ ] 驗證失敗回傳適當的錯誤訊息
- [ ] 不洩漏系統內部資訊
- [ ] 防範 SQL Injection（若使用資料庫）
- [ ] 防範 XSS（跨站腳本攻擊）

## 📊 日誌與監控

### 日誌設定

- [ ] 日誌等級設定為 `Information` 或 `Warning`（不使用 `Debug`）
- [ ] 日誌不包含敏感資訊：
  - [ ] 不記錄密碼
  - [ ] 不記錄完整的 Token
  - [ ] 不記錄 JWT 密鑰
  - [ ] 不記錄個人資料（PII）
- [ ] 使用結構化日誌（Serilog JSON 格式）
- [ ] 設定日誌輪換策略（每日、每週）
- [ ] 設定日誌保留期限（例如 30 天）
- [ ] 日誌目錄有足夠的磁碟空間
- [ ] 應用程式有寫入日誌目錄的權限

### 安全事件記錄

- [ ] 記錄所有登入嘗試（成功與失敗）
- [ ] 記錄 Token 輪換事件
- [ ] 記錄 Token 重用偵測事件
- [ ] 記錄登出事件
- [ ] 記錄授權失敗事件
- [ ] 記錄包含以下資訊：
  - [ ] 使用者 ID / Username
  - [ ] IP 位址
  - [ ] 時間戳記
  - [ ] 事件類型
  - [ ] 結果（成功/失敗）

### 監控與警報

- [ ] 設定應用程式效能監控（APM）
  - [ ] Application Insights（Azure）
  - [ ] New Relic
  - [ ] Datadog
  - [ ] Elastic APM
- [ ] 監控關鍵指標：
  - [ ] API 回應時間
  - [ ] 錯誤率
  - [ ] 記憶體使用量
  - [ ] CPU 使用率
  - [ ] 請求數量
- [ ] 設定警報：
  - [ ] 錯誤率異常升高
  - [ ] 回應時間過慢
  - [ ] Token 重用偵測頻繁
  - [ ] 磁碟空間不足
  - [ ] 記憶體洩漏

## ⚙️ 組態設定

### 應用程式組態

- [ ] `appsettings.Production.json` 已正確設定
- [ ] 環境變數 `ASPNETCORE_ENVIRONMENT` 設定為 `Production`
- [ ] 不在 appsettings.json 中儲存敏感資訊
- [ ] 資料庫連線字串已加密或儲存在 Key Vault
- [ ] 第三方 API 金鑰已安全儲存
- [ ] AllowedHosts 已設定正確的網域

### Token 設定驗證

- [ ] `JwtSettings:Issuer` 設定為生產環境 URL
- [ ] `JwtSettings:Audience` 設定為生產環境 URL
- [ ] `JwtSettings:AccessTokenExpiryMinutes` <= 30
- [ ] `JwtSettings:RefreshTokenExpiryDays` <= 30
- [ ] `JwtSettings:GracePeriodSeconds` = 30

### Serilog 設定

- [ ] 日誌路徑設定為絕對路徑（IIS）
- [ ] 設定適當的 `MinimumLevel`
- [ ] 停用不必要的 Enrichers
- [ ] 設定 `RollingInterval` 為 Day 或 Hour

## 🚀 效能與延展性

### 效能優化

- [ ] 啟用 Response Compression（已在程式碼中啟用）
- [ ] 考慮啟用 Output Caching
- [ ] 設定適當的快取策略
- [ ] 資料庫查詢已優化（若使用資料庫）
- [ ] 使用連線池
- [ ] 非同步方法已正確實作（`async/await`）

### 快取設定

- [ ] IMemoryCache 設定適當的大小限制
- [ ] 考慮使用分散式快取（Redis）以支援水平擴展
- [ ] Token Family 快取過期時間正確設定
- [ ] 黑名單快取過期時間正確設定

### 負載測試

- [ ] 執行負載測試驗證應用程式容量
- [ ] 測試同時登入使用者數量
- [ ] 測試 Token 重新整理高併發情況
- [ ] 測試記憶體使用量在高負載下的表現
- [ ] 識別效能瓶頸並優化

## 🏗️ 基礎設施

### IIS 設定（若使用 IIS）

- [ ] 安裝 .NET 9.0 Hosting Bundle
- [ ] 應用程式集區設定正確：
  - [ ] .NET CLR 版本：無受控程式碼
  - [ ] 受控管線模式：整合式
  - [ ] 啟用 32 位元應用程式：False
  - [ ] 載入使用者設定檔：True
- [ ] 應用程式集區有足夠的權限
- [ ] 設定應用程式集區回收策略
- [ ] 啟用 stdout 日誌（疑難排解用）

### Azure App Service（若使用 Azure）

- [ ] 選擇適當的 App Service Plan 等級
- [ ] 啟用 Managed Identity
- [ ] 設定 Always On（避免冷啟動）
- [ ] 設定自動擴展規則
- [ ] 設定健康檢查端點
- [ ] 設定部署槽位（Staging/Production）

### 資料庫（若使用）

- [ ] 資料庫連線字串已加密
- [ ] 使用連線池
- [ ] 設定適當的連線逾時
- [ ] 資料庫備份策略已設定
- [ ] 資料庫效能監控已啟用

## 🧪 測試

### 測試覆蓋率

- [ ] 單元測試覆蓋率 >= 90%
- [ ] 整合測試涵蓋所有關鍵流程
- [ ] 測試 Token 輪換流程
- [ ] 測試 Token 重用偵測
- [ ] 測試授權與角色驗證
- [ ] 測試錯誤處理

### 安全測試

- [ ] 執行漏洞掃描
- [ ] 測試 SQL Injection
- [ ] 測試 XSS 攻擊
- [ ] 測試 CSRF 攻擊
- [ ] 測試未授權存取
- [ ] 測試密碼暴力破解（考慮實作速率限制）

### 效能測試

- [ ] 執行基準測試
- [ ] Token 產生效能可接受
- [ ] Token 驗證效能可接受
- [ ] 密碼雜湊效能可接受
- [ ] API 回應時間 < 500ms（95 percentile）

## 📦 部署

### 部署前

- [ ] 所有測試通過
- [ ] 程式碼審查完成
- [ ] 更新 CHANGELOG
- [ ] 標記版本號（Git tag）
- [ ] 建立發布說明

### 部署程序

- [ ] 使用自動化部署（CI/CD）
- [ ] 有回滾計劃
- [ ] 生產環境備份已完成
- [ ] 部署時間選擇在低峰時段
- [ ] 有部署檢查清單

### 部署後驗證

- [ ] 應用程式正常啟動
- [ ] 健康檢查端點回傳正常
- [ ] Swagger UI 可存取（或已停用）
- [ ] 登入功能正常
- [ ] Token 重新整理功能正常
- [ ] 授權功能正常
- [ ] 日誌正常寫入
- [ ] 監控顯示正常指標
- [ ] 執行煙霧測試（Smoke Tests）

## 🔄 維護

### 定期維護

- [ ] 定期更新 .NET Runtime 與套件
- [ ] 定期審查安全漏洞（`dotnet list package --vulnerable`）
- [ ] 定期審查日誌
- [ ] 定期審查監控指標
- [ ] 定期測試備份還原
- [ ] 定期審查密鑰並考慮輪換

### 文件

- [ ] API 文件已更新
- [ ] 部署文件已更新
- [ ] 故障排除文件已準備
- [ ] 運維手冊已準備
- [ ] 聯絡資訊已更新

## 🆘 災難復原

### 備份

- [ ] 自動備份已設定
- [ ] 備份已測試還原
- [ ] 備份儲存在異地
- [ ] 備份保留策略已設定

### 高可用性

- [ ] 考慮多區域部署
- [ ] 考慮負載平衡
- [ ] 考慮自動故障轉移
- [ ] 設定災難復原計劃（DRP）

## 📋 最終檢查

- [ ] 所有上述項目都已完成
- [ ] 相關團隊已通知部署時間
- [ ] 部署後支援人員已待命
- [ ] 監控已啟用並正常運作
- [ ] 回滾計劃已準備就緒

---

**注意**：這是一個全面的檢查清單。根據您的具體需求，某些項目可能不適用。請根據您的環境調整清單。

**建議**：在每次部署前印出此清單並逐項檢查，確保不遺漏任何重要步驟。

**最後更新**：2025-01-09
