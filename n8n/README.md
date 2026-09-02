# n8n 訂房確認信工作流程

本目錄提供「飯店訂房成功通知」功能所需的 n8n 工作流程、Docker 環境範例及測試工具。

目的為讓其他組員在功能分支合併後，可以：

1. 在自己的電腦建立本機 n8n。
2. 匯入訂房確認信工作流程。
3. 完成 ASP.NET Core 與 n8n 的整合驗收。
4. 後續使用相同工作流程建立遠端 n8n。

---

## 目錄結構

```text
n8n/
├─ workflows/
│  └─ hotel-booking-confirmed.json
├─ scripts/
│  └─ test-webhook.ps1
├─ docker-compose.yml
├─ .env.example
└─ README.md
```

各檔案用途：

- `workflows/hotel-booking-confirmed.json`：已測試成功的 n8n 工作流程。
- `scripts/test-webhook.ps1`：用來直接測試 n8n Webhook。
- `docker-compose.yml`：建立本機 n8n 容器及保存 SQLite 資料的 Docker Volume。
- `.env.example`：本機環境變數範例，不包含真實密碼。
- `README.md`：本機驗收及遠端部署說明。

---

## 目前功能狀態

目前已完成：

- 訂房付款成功並建立訂單後，呼叫 n8n Webhook。
- 使用 Gmail SMTP 寄送訂房確認信。
- n8n 回傳寄送結果 JSON。
- 訂房成功頁顯示確認信寄送結果。
- 訂房成功頁顯示遮罩後的收件 Email。
- 後台訂單查詢顯示顧客 Email。
- 後台可修改訂單 Email。
- 後台可補寄確認信。
- 短時間內重複補寄會被阻擋。
- n8n 無法連線或寄信失敗時，不會取消已成立的訂單。
- n8n 呼叫失敗時，ASP.NET Core 會寫入 Log。

尚待飯店資料庫重建後完成：

- 在新版飯店資料庫加入 `LastConfirmationEmailSentAt`。
- 重新產生 Database First Entity。
- 寄送成功後保存最近一次寄送時間。
- 後台訂單明細顯示資料庫中的實際寄送時間。
- 移除目前暫時使用的寄送時間顯示資料。
- 重新執行完整整合測試。

> n8n 目前使用內建 SQLite 儲存 Owner 帳號、工作流程、Credential 及 Executions。   
> n8n SQLite 與飯店系統資料庫互相獨立，重建飯店系統資料庫不會直接影響 n8n。

---

## 機密資料規則

以下資料禁止提交至 GitHub：

- Gmail 主帳號密碼。
- Gmail 16 位應用程式密碼。
- Webhook Header Auth 密鑰。
- n8n Encryption Key。
- n8n Owner 密碼。
- ASP.NET Core User Secrets。
- 真實 `.env` 檔案。
- 真實顧客姓名、電話、Email 或訂單資料。
- n8n SQLite 的本機資料或 Docker Volume。

`.env.example` 只能提供欄位名稱與設定範例，不得包含真實密碼。

即使是專案專用帳號，密碼也只能透過團隊認可的安全方式私下提供，不得寫入 Repository。

---

# 一、本機環境建立

## 1. 前置需求

開始前請先準備：

- Docker Desktop。
- 已拉取最新主分支的 ASP.NET Core 專案。
- 專案測試寄件 Gmail。
- 專案測試 Gmail 的應用程式密碼。
- 可接收測試信的 Email。

本功能目前使用 Gmail SMTP，不使用 Gmail OAuth Credential。

---

## 2. 建立本機環境變數

在 `n8n` 資料夾內，將：

```text
.env.example
```

複製一份並命名為：

```text
.env
```

開啟 `.env`，填入自行產生的本機設定：

```env
N8N_ENCRYPTION_KEY=填入自行產生並妥善保存的長隨機加密金鑰
GENERIC_TIMEZONE=Asia/Taipei
TZ=Asia/Taipei
```

注意：

- `N8N_ENCRYPTION_KEY` 用來加密 n8n 儲存的 Credential。
- `N8N_ENCRYPTION_KEY` 建立後必須保留，不可任意更換。
- `.env` 已被 `.gitignore` 排除，禁止提交。

---

## 3. 啟動 Docker 容器

確認 Docker Desktop 已啟動。

在 PowerShell 進入 `n8n` 資料夾：

```powershell
cd "專案實際路徑\n8n"
```

執行：

```powershell
docker compose up -d
```

確認容器狀態：

```powershell
docker compose ps
```

n8n 容器應顯示為執行中。

---

## 4. 建立本機 n8n Owner

開啟瀏覽器：

```text
http://localhost:5678
```

第一次啟動時，依畫面建立本機 n8n Owner 帳號。

這個帳號只存在目前這套本機 n8n，不會與其他組員或遠端 n8n 自動同步。

建議：

- Owner Email 可以使用專案管理信箱。
- 本機 Owner 密碼由本機使用者自行設定。
- 不要沿用其他環境的 Owner 密碼。
- 不要將 Owner 密碼提交 GitHub。

---

# 二、匯入工作流程

## 1. 匯入 JSON

登入 n8n 後，匯入：

```text
workflows/hotel-booking-confirmed.json
```

工作流程名稱應為：

```text
飯店訂房成功通知－Webhook
```

預期節點順序：

```text
Webhook
→ Send an Email
→ Respond to Webhook
```

匯入後，原本的 Credential 不會直接在新環境使用，必須重新建立並重新選取。

---

# 三、建立 SMTP Credential

## 1. 建立 SMTP account

在 n8n 的 Credential 選單建立 SMTP Credential。

建議 Credential 顯示名稱：

```text
SMTP account
```

設定內容：

```text
Host：smtp.gmail.com
Port：465
SSL/TLS：開啟
User：填入專案寄件 Gmail
Password：私下取得專案 Gmail 的應用程式密碼
Client Host Name：留空
```

注意：

- Password 使用 Gmail 16 位應用程式密碼。
- 不使用 Gmail 主密碼。
- 不將應用程式密碼寫入本文件。
- 本功能目前使用 SMTP，不使用既有的 Gmail OAuth Credential。

儲存 Credential 後，開啟工作流程的 `Send an Email` 節點，重新選取：

```text
SMTP account
```

並確認：

```text
From Email：專案寄件 Gmail
```

---

# 四、建立 Webhook Header Auth Credential

## 1. 產生本機 Webhook Secret

每位組員的本機環境應自行產生一組 Webhook Secret。

本機測試密鑰不得與遠端正式密鑰共用。

## 2. 建立 Header Auth Credential

在 Webhook 節點中，確認：

```text
Authentication：Header Auth
```

建立 Header Auth Credential：

```text
Credential 顯示名稱：Hotel Booking Webhook Header Auth
Name：X-Hotel-Webhook-Key
Value：填入自行產生的本機 Webhook Secret
```

注意：

- Credential 顯示名稱可以自行命名。
- Header 的 `Name` 必須是：

```text
X-Hotel-Webhook-Key
```

- `Value` 必須與 ASP.NET Core 設定的 `N8n:WebhookSecret` 完全相同。
- Webhook Secret 不得提交 GitHub。

---

# 五、檢查 Webhook 設定

開啟 Webhook 節點，確認：

```text
HTTP Method：POST
Path：hotel-booking-confirmed
Authentication：Header Auth
Respond：Using 'Respond to Webhook' Node
```

Production URL 應為：

```text
http://localhost:5678/webhook/hotel-booking-confirmed
```

測試 URL 則為：

```text
http://localhost:5678/webhook-test/hotel-booking-confirmed
```

兩者用途不同：

- `/webhook-test/`：按下 `Listen for test event` 時使用。
- `/webhook/`：工作流程 Publish 後，由 ASP.NET Core 正式呼叫。

---

# 六、ASP.NET Core 本機設定

## 1. 非機密設定

本機 `appsettings.Development.json` 應包含：

```json
{
  "N8n": {
    "WebhookUrl": "http://localhost:5678/webhook/hotel-booking-confirmed",
    "HeaderName": "X-Hotel-Webhook-Key"
  }
}
```

若檔案中已存在其他設定，請將 `N8n` 區段合併進原本 JSON，不要刪除其他設定。

## 2. Webhook Secret

在 ASP.NET Core 專案根目錄開啟 PowerShell，執行：

```powershell
dotnet user-secrets set "N8n:WebhookSecret" "填入與Header Auth相同的本機密鑰"
```

確認設定：

```powershell
dotnet user-secrets list
```

禁止將 Webhook Secret 寫入：

- `appsettings.json`
- `appsettings.Development.json`
- README
- GitHub

---

# 七、Webhook Request 格式

ASP.NET Core 傳送給 n8n 的 JSON 格式如下：

```json
{
  "bookingNumber": "TEST-001",
  "bookerName": "王小明",
  "email": "test@example.com",
  "roomTypeName": "豪華雙人房",
  "checkInDate": "2026-09-10",
  "checkOutDate": "2026-09-12",
  "totalAmount": 6000,
  "branchName": "台北分館",
  "branchAddress": "台北市中正區測試路100號",
  "branchPhone": "0212345678"
}
```

欄位說明：

| JSON 欄位 | 用途 |
|---|---|
| `bookingNumber` | 訂單編號 |
| `bookerName` | 訂房人姓名 |
| `email` | 確認信收件地址 |
| `roomTypeName` | 訂購房型名稱 |
| `checkInDate` | 入住日期 |
| `checkOutDate` | 退房日期 |
| `totalAmount` | 訂單總金額 |
| `branchName` | 入住分館名稱 |
| `branchAddress` | 入住分館地址 |
| `branchPhone` | 入住分館電話 |

Request 的欄位名稱必須與 ASP.NET Core 的 `N8nBookingEmailRequest` JSON 設定一致。

---

# 八、Webhook Response 格式

n8n 成功完成工作流程後，應回傳：

```json
{
  "success": true,
  "bookingNumber": "TEST-001",
  "emailAccepted": true,
  "n8nCompletedAtUtc": "2026-09-02T06:30:00.000Z"
}
```

欄位說明：

| JSON 欄位 | 用途 |
|---|---|
| `success` | n8n 工作流程是否成功完成 |
| `bookingNumber` | 對應的訂單編號 |
| `emailAccepted` | 寄信節點是否接受寄送 |
| `n8nCompletedAtUtc` | n8n 完成時間，使用 UTC |

Response 必須與 ASP.NET Core 的 `N8nEmailResponse` 對應。

---

# 九、Publish 工作流程

完成 Credential 與節點設定後：

1. 儲存工作流程。
2. 按下 `Publish`。
3. 確認使用 Production URL。
4. 不需要勾選 Production Checklist 的選項即可進行目前測試。

未 Publish 時，ASP.NET Core 呼叫 Production URL 可能無法觸發工作流程。

---

# 十、直接測試 n8n Webhook

可使用：

```text
scripts/test-webhook.ps1
```

執行時傳入：

- Webhook URL。
- Webhook Secret。
- 測試收件 Email。

預期執行方式：

```powershell
.\scripts\test-webhook.ps1 `
    -WebhookUrl "http://localhost:5678/webhook/hotel-booking-confirmed" `
    -WebhookSecret "填入本機Webhook密鑰" `
    -RecipientEmail "填入測試收件信箱"
```

成功時應顯示：

```text
success              : True
bookingNumber        : TEST-HANDOFF-001
emailAccepted        : True
n8nCompletedAtUtc    : UTC時間
```

並確認測試信箱收到訂房確認信。

---

# 十一、ASP.NET Core 完整驗收

至少執行以下測試：

## 正常訂房

- 付款成功後建立訂單。
- n8n Executions 顯示成功。
- 顧客收到訂房確認信。
- 郵件中的訂單編號、姓名、分館、房型、日期及金額正確。
- 訂房成功頁顯示確認信寄送成功。
- 訂房成功頁顯示遮罩後的 Email。

## Email 格式錯誤

- 付款頁阻擋錯誤 Email 格式。
- 後台修改 Email 時阻擋錯誤格式。

錯誤範例：

```text
abc
abc@
abc@gmail
@gmail.com
abc gmail.com
```

## n8n 停止

- 停止 n8n 後建立測試訂單。
- 訂單仍然成功建立。
- 成功頁顯示確認信暫時無法寄送。
- Visual Studio Output 或 Log 出現 n8n 連線錯誤。

## 後台修改 Email

- 開啟已付款訂單。
- 修改 Email。
- Modal 不重新整理即可顯示新 Email。
- 關閉後重新查詢仍顯示新 Email。
- 資料庫中的 `Booking.Email` 已更新。

## 後台補寄

- 已付款訂單可以補寄。
- 修改 Email 後可以寄到新 Email。
- 收件信箱確實收到確認信。
- 短時間內重複點擊會被阻擋。
- 已取消訂單不可補寄。

## SMTP 錯誤

- SMTP Credential 錯誤時，訂單仍然成立。
- n8n Executions 顯示寄信失敗。
- ASP.NET Core 不把寄信失敗當成付款失敗。

---

# 十二、常見問題

## 403 Forbidden

可能原因：

- Request 沒有帶 `X-Hotel-Webhook-Key`。
- Header 名稱錯誤。
- ASP.NET Core 與 n8n 使用不同的 Webhook Secret。
- 本機 User Secrets 尚未設定。

## Webhook 一直 Listening

可能原因：

- 使用 Production URL 測試 `Listen for test event`。
- Test URL 與 Production URL 混用。
- PowerShell Request 尚未真正送出。
- `Respond to Webhook` 節點沒有完成。

## ASP.NET Core 顯示 n8n 網路錯誤

檢查：

- Docker Desktop 是否啟動。
- n8n 容器是否執行中。
- `WebhookUrl` 是否正確。
- 工作流程是否已 Publish。
- 是否誤用 `/webhook-test/`。

## 無法解析 n8n 回應

檢查：

- `Respond to Webhook` 是否回傳有效 JSON。
- n8n 表達式是否使用 Expression 模式。
- Response 欄位名稱是否與 `N8nEmailResponse` 一致。
- JSON 中是否殘留未解析的 `{{ }}` 文字。

## 匯入後 Send an Email 顯示 Credential 錯誤

這是正常情況。

匯出的 Workflow 不會讓另一台 n8n 直接使用原本 Credential。

請在新環境：

1. 建立 `SMTP account`。
2. 回到 `Send an Email` 節點。
3. 重新選取新的 Credential。
4. 儲存並測試。

## 信箱沒有收到信

檢查：

- SMTP Credential 是否連線成功。
- Gmail App Password 是否正確。
- From Email 是否與 SMTP 帳號一致。
- To Email 表達式是否取得 `body.email`。
- Gmail 垃圾郵件匣。
- n8n Executions 的錯誤內容。

---

# 十三、停止本機環境

停止容器：

```powershell
docker compose stop
```

重新啟動：

```powershell
docker compose start
```

停止並移除容器，但保留具名 Volume：

```powershell
docker compose down
```

除非確定要刪除全部本機 n8n 資料，否則不要執行：

```text
docker compose down -v
```

因為 `-v` 會移除 Volume，可能刪除：

- n8n Owner 帳號。
- 工作流程。
- Credential。
- Executions。
- n8n SQLite 資料。

---

# 十四、遠端部署原則

> 本目錄的 `docker-compose.yml` 僅供本機 HTTP 開發環境使用。  
> 遠端部署不可直接沿用 `N8N_SECURE_COOKIE=false`，必須配合 HTTPS 及正式環境安全設定重新調整。

完成本機驗收後，使用相同的 Workflow JSON 建立遠端 n8n。

遠端環境必須重新產生：

- `N8N_ENCRYPTION_KEY`
- n8n Owner 密碼
- Webhook Header Auth Secret
- 正式環境 Gmail App Password

禁止直接沿用本機測試密鑰。

## ASP.NET Core 與 n8n 位於同一個 Docker 網路

Webhook URL 可能設定為：

```text
http://n8n:5678/webhook/hotel-booking-confirmed
```

## ASP.NET Core 與 n8n 位於同一台主機，但 ASP.NET Core 不在 Docker

Webhook URL 可能設定為：

```text
http://127.0.0.1:5678/webhook/hotel-booking-confirmed
```

## ASP.NET Core 與 n8n 位於不同伺服器

必須使用 HTTPS，例如：

```text
https://n8n.example.com/webhook/hotel-booking-confirmed
```

正式環境不可直接把 `5678` 管理介面無保護地公開至網際網路。

## ASP.NET Core 正式環境設定

正式環境不要使用開發用 User Secrets，應使用部署平台的 Secret 或環境變數：

```text
N8n__WebhookUrl
N8n__HeaderName
N8n__WebhookSecret
```

其中：

```text
N8n__HeaderName=X-Hotel-Webhook-Key
```

`N8n__WebhookSecret` 必須與遠端 n8n Header Auth Credential 的 Value 完全相同。

---

# 十五、交接內容

Repository 會提供：

- n8n Workflow JSON。
- Docker Compose。
- `.env.example`。
- PowerShell 測試程式。
- 本機與遠端設定說明。

專案管理者會私下提供：

- 專案測試寄件 Gmail。
- 測試用 Gmail App Password。

本機驗收人員自行產生：

- 本機 `N8N_ENCRYPTION_KEY`。
- 本機 n8n Owner 密碼。
- 本機 Webhook Secret。

遠端部署人員自行產生並保存：

- 遠端 `N8N_ENCRYPTION_KEY`。
- 遠端 n8n Owner 密碼。
- 遠端 Webhook Secret。
- 正式 Gmail App Password。

---

# 十六、交接完成標準

符合以下條件即視為交接完成：

1. 組員拉取主分支。
2. 組員依本文件建立自己的本機 n8n。
3. 組員匯入 Workflow JSON。
4. 組員重新建立 SMTP 與 Header Auth Credential。
5. 組員設定 ASP.NET Core User Secrets。
6. PowerShell 測試可以收到成功 JSON。
7. 測試信箱可以收到確認信。
8. ASP.NET Core 完整訂房流程測試成功。
9. 組員可以使用相同 Workflow JSON 建立遠端 n8n。
10. 所有真實密碼與密鑰都未提交至 GitHub。