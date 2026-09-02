# n8n 訂房確認信工作流程

本目錄提供「飯店訂房成功通知」功能所需的 n8n 工作流程、Docker 環境範例及測試工具。

目前開發階段由一台開發主機執行 Docker、n8n 與 ngrok，
其他組員可透過固定的 ngrok Webhook URL 呼叫同一套 n8n 工作流程，
不需要在各自電腦另外安裝或設定 n8n。

本目錄同時保留本機 n8n 建置與 Workflow 匯入說明，
供環境重建、獨立驗收及後續遠端部署使用。

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

# 目前開發測試架構

目前開發階段使用一台電腦作為 n8n 開發主機：

```text
其他組員 ASP.NET Core MVC
        ↓
ngrok HTTPS Webhook URL
        ↓
開發主機
        ↓
Docker / n8n
        ↓
Send an Email（SMTP）
        ↓
Gmail
```

測試寄信功能前，需確認提供 n8n 的開發主機：

- 已啟動 Docker Desktop。
- n8n Container 正常執行。
- ngrok Agent 與 Endpoint 為 Online。
- 「飯店訂房成功通知－Webhook」工作流程已 Published。

其他組員不需要另外安裝 n8n、匯入 Workflow 或建立 SMTP Credential；
ASP.NET Core 會透過設定檔中的 ngrok Webhook URL 呼叫開發主機上的 n8n。

> 此 ngrok Endpoint 僅供開發與整合測試使用，不作為正式部署環境。  
> 若開發主機關機、Docker 或 n8n 停止、ngrok 離線，其他裝置將暫時無法使用寄信功能。

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

# 一、本機 n8n 環境建立

> 第一至五章適用於需要自行建立、重建或獨立驗收 n8n 環境的情況。  
> 一般組員若使用目前共用的 ngrok 開發測試環境，不需要執行第一至五章的環境建立與 Credential 設定。

## 1. 前置需求

自行建立 n8n 環境前請先準備：

- Docker Desktop。
- 已拉取專案 Repository。
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

確認 n8n 容器顯示為執行中後，再進行後續設定。

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

## 1. 準備 Webhook Secret

若自行建立獨立的本機 n8n，應自行產生一組 Webhook Secret。

若使用目前共用的 ngrok 開發測試環境，ASP.NET Core 必須使用與共用 n8n Header Auth Credential 相同的測試用 Webhook Secret，並透過團隊認可的安全方式私下取得。

開發測試用 Webhook Secret 不得提交至 GitHub，也不得與遠端正式環境的密鑰共用。

## 2. 建立 Header Auth Credential

在 Webhook 節點中，確認：

```text
Authentication：Header Auth
```

建立 Header Auth Credential：

```text
Credential 顯示名稱：Hotel Booking Webhook Header Auth
Name：X-Hotel-Webhook-Key
Value：填入此 n8n 環境使用的 Webhook Secret
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

若使用自行建立的本機 n8n，Production URL 為：

```text
http://localhost:5678/webhook/hotel-booking-confirmed
```

若使用目前共用的 ngrok 開發測試環境，ASP.NET Core 應呼叫：

```text
https://<ngrok-domain>/webhook/hotel-booking-confirmed
```

實際 ngrok Domain 以目前開發環境設定為準。

本機 n8n 的 Test URL 則為：

```text
http://localhost:5678/webhook-test/hotel-booking-confirmed
```

Production URL 與 Test URL 用途不同：

- `/webhook-test/`：在 n8n 中按下 `Listen for test event` 時使用。
- `/webhook/`：工作流程 Publish 後，由 ASP.NET Core 呼叫。

---

# 六、ASP.NET Core 開發環境設定

ASP.NET Core 透過 `N8n` 設定取得 Webhook URL、Header 名稱與 Webhook Secret。

## 1. 設定 Webhook URL

若使用目前共用的 ngrok 開發測試環境，在 `appsettings.Development.json` 設定：

```json
"N8n": {
  "WebhookUrl": "https://<ngrok-domain>/webhook/hotel-booking-confirmed",
  "HeaderName": "X-Hotel-Webhook-Key"
}
```

`<ngrok-domain>` 請替換為目前共用開發環境實際使用的 ngrok Domain。

若自行建立並使用本機 n8n，則改為：

```json
"N8n": {
  "WebhookUrl": "http://localhost:5678/webhook/hotel-booking-confirmed",
  "HeaderName": "X-Hotel-Webhook-Key"
}
```

## 2. 設定 Webhook Secret

`WebhookSecret` 不可寫入 `appsettings.Development.json`，請使用 ASP.NET Core User Secrets：

```powershell
dotnet user-secrets init
dotnet user-secrets set "N8n:WebhookSecret" "你的 Webhook Secret"
```

若使用共用的 ngrok 開發測試環境，這裡設定的 Webhook Secret 必須與共用 n8n 的 Header Auth Credential 相同。

若自行建立本機 n8n，則必須與自己建立的 Header Auth Credential 相同。

設定完成後，可使用以下指令確認：

```powershell
dotnet user-secrets list
```

應可看到：

```text
N8n:WebhookSecret = ...
```

> Webhook Secret 僅存放於各自電腦的 User Secrets，不得寫入程式碼、設定檔或提交至 GitHub。

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

# 九、Publish Workflow

完成 Workflow 設定與測試後，必須 Publish Workflow。

Publish 後，ASP.NET Core 才能透過 Production Webhook URL（`/webhook/`）呼叫。

若使用共用的 ngrok 開發測試環境，需確認開發主機上的「飯店訂房成功通知－Webhook」工作流程已 Published。

---

# 十、直接測試 Webhook

可先不經過 ASP.NET Core，直接使用 PowerShell 測試 n8n Webhook 是否能正常接收請求並寄送 Email。

## 1. 設定測試參數

若使用目前共用的 ngrok 開發測試環境：

```powershell
$webhookUrl = "https://<ngrok-domain>/webhook/hotel-booking-confirmed"
```

若使用自行建立的本機 n8n：

```powershell
$webhookUrl = "http://localhost:5678/webhook/hotel-booking-confirmed"
```

設定 Header：

```powershell
$headers = @{
    "X-Hotel-Webhook-Key" = "你的 Webhook Secret"
}
```

設定測試資料：

```powershell
$body = @{
    bookingNumber = "TEST-001"
    bookerName = "王小明"
    email = "test@example.com"
    roomTypeName = "豪華雙人房"
    checkInDate = "2026-09-10"
    checkOutDate = "2026-09-12"
    totalAmount = 6000
    branchName = "台北分館"
    branchAddress = "台北市中正區測試路100號"
    branchPhone = "0212345678"
} | ConvertTo-Json
```

送出 POST：

```powershell
Invoke-RestMethod `
    -Uri $webhookUrl `
    -Method Post `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $body
```

若執行成功，應：

- n8n 出現新的 Workflow Execution。
- 指定的收件 Email 收到訂房成功通知信。
- PowerShell 收到成功 Response。

> 若使用共用 ngrok 開發測試環境，Webhook Secret 必須與共用 n8n 的 Header Auth Credential 相同。

---

# 十一、完整驗收流程

依使用情境，可分為「共用 ngrok 開發環境驗收」與「獨立本機 n8n 驗收」。

## 1. 共用 ngrok 開發環境驗收

一般組員進行功能整合測試時，建議使用此方式。

### Step 1：確認共用 n8n 環境

確認提供 n8n 的開發主機：

- Docker Desktop 已啟動。
- n8n Container 正常執行。
- ngrok Agent 與 Endpoint 為 Online。
- 「飯店訂房成功通知－Webhook」工作流程已 Published。

### Step 2：確認 ASP.NET Core 設定

確認 `appsettings.Development.json` 的：

```text
N8n:WebhookUrl
```

使用目前共用的 ngrok Production Webhook URL：

```text
https://<ngrok-domain>/webhook/hotel-booking-confirmed
```

並確認：

```text
N8n:HeaderName = X-Hotel-Webhook-Key
```

### Step 3：確認 User Secrets

確認本機已設定：

```text
N8n:WebhookSecret
```

此 Secret 必須與共用 n8n 的 Header Auth Credential 相同。

### Step 4：執行 ASP.NET Core 專案

啟動 ASP.NET Core MVC 專案，完成一次完整訂房流程。

### Step 5：確認結果

驗收以下項目：

- 訂房流程正常完成。
- n8n 出現新的 Workflow Execution。
- Workflow Execution 執行成功。
- 指定的 Email 收到訂房成功通知信。
- Email 內容中的訂房資料正確。

---

## 2. 獨立本機 n8n 驗收

若需要自行建立、重建或獨立驗收 n8n 環境，先依第一至五章完成：

1. 建立 `.env`。
2. 啟動 Docker n8n。
3. 建立 n8n Owner。
4. 匯入 Workflow。
5. 建立 SMTP Credential。
6. 建立 Header Auth Credential。
7. Publish Workflow。

接著將 ASP.NET Core 的 Webhook URL 設為：

```text
http://localhost:5678/webhook/hotel-booking-confirmed
```

並依照前述流程完成一次訂房測試。

驗收結果同樣應確認：

- ASP.NET Core 訂房流程正常完成。
- n8n Workflow Execution 成功。
- 指定的 Email 收到訂房成功通知信。
- Email 內容中的訂房資料正確。

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

### ASP.NET Core 出現網路錯誤

若使用共用 ngrok 開發測試環境，確認：

- 提供 n8n 的開發主機是否開機並保持網路連線。
- Docker Desktop 是否已啟動。
- n8n Container 是否正常執行。
- ngrok Agent 是否正常執行，且 Endpoint 為 Online。
- `N8n:WebhookUrl` 是否為目前正確的 ngrok Production Webhook URL。
- Webhook URL 是否使用 `/webhook/`，而不是 `/webhook-test/`。

若使用自行建立的本機 n8n，確認：

- Docker Desktop 是否已啟動。
- n8n Container 是否正常執行。
- `N8n:WebhookUrl` 是否為：

```text
http://localhost:5678/webhook/hotel-booking-confirmed
```

- Docker Port `5678` 是否正常對應至 n8n。


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

# 十三、停止本機 n8n 環境

若只是暫時停止本機 n8n，可在 `n8n` 目錄執行：

```powershell
docker compose down
```

此指令會停止並移除 Container，但會保留 `n8n_data` Docker Volume，因此 n8n 的設定與 SQLite 資料仍會保留。

若確定要連同本機 n8n 資料一起刪除，可執行：

```powershell
docker compose down -v
```

> `-v` 會刪除 `n8n_data` Docker Volume，其中包含本機 n8n 的 SQLite 資料、Workflow、Credential 等設定。  
> 除非確定要重建環境，否則不要使用 `docker compose down -v`。

若目前使用的是共用 ngrok 開發測試環境，一般組員不需要執行上述指令；只有執行 n8n 的開發主機需要管理 Docker Container。

---

# 十四、遠端部署原則

> 目前使用的 ngrok Endpoint 僅供開發與整合測試使用，不視為正式遠端部署環境。  
> 正式部署時，應將 n8n 部署於可長期穩定運行的遠端環境，並使用正式的 HTTPS Domain、Webhook Secret 與 Credential。
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

目前開發階段以「共用 n8n + ngrok」作為主要整合測試方式。

## 1. 一般組員進行開發測試

一般組員不需要另外安裝或建立 n8n，只需：

1. 拉取最新專案程式碼。
2. 在 `appsettings.Development.json` 設定目前共用的 ngrok Production Webhook URL。
3. 確認 `N8n:HeaderName` 為 `X-Hotel-Webhook-Key`。
4. 使用 ASP.NET Core User Secrets 設定共用開發環境的 `N8n:WebhookSecret`。
5. 確認提供 n8n 的開發主機、Docker、n8n 與 ngrok 皆正常執行。
6. 啟動 ASP.NET Core MVC 專案並完成訂房流程測試。
7. 確認 n8n Workflow Execution 成功，且收到訂房成功通知信。

Webhook Secret 應透過團隊認可的安全方式私下提供，不得寫入 README、程式碼或提交至 GitHub。

## 2. 需要自行建立或重建 n8n

Repository 的 `n8n/` 目錄已提供：

```text
workflows/hotel-booking-confirmed.json
scripts/test-webhook.ps1
docker-compose.yml
.env.example
README.md
```

需要自行建立、重建或獨立驗收 n8n 環境時，可依本 README 前述的本機環境建立、Credential 設定、Webhook 設定及驗收流程操作。

SMTP Credential、Webhook Secret、`N8N_ENCRYPTION_KEY` 等敏感資訊不包含於 Repository，需依文件說明自行設定或透過安全方式取得。

---

# 十六、交接完成標準

符合以下條件即視為交接完成：

## 1. 共用 ngrok 開發環境

一般組員應能：

1. 拉取最新專案程式碼。
2. 正確設定共用的 ngrok Production Webhook URL。
3. 使用 ASP.NET Core User Secrets 設定 `N8n:WebhookSecret`。
4. 啟動 ASP.NET Core MVC 專案並完成一次完整訂房流程。
5. 成功觸發共用 n8n Workflow。
6. 測試信箱可以收到內容正確的訂房成功通知信。

## 2. n8n 環境重建與後續交接

依本文件操作後，應能：

1. 使用 `docker-compose.yml` 建立本機 n8n。
2. 使用 `.env.example` 建立自己的 `.env`。
3. 匯入 `hotel-booking-confirmed.json`。
4. 重新建立 SMTP 與 Header Auth Credential。
5. 使用 Production Webhook 成功觸發 Workflow。
6. PowerShell 測試可以收到成功 Response。
7. 測試信箱可以收到訂房成功通知信。
8. 使用相同 Workflow JSON 建立後續遠端 n8n 環境。
9. 所有真實密碼與密鑰皆未提交至 GitHub。