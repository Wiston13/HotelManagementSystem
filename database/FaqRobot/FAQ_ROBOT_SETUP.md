# FAQ Robot 設定說明

本功能由 ASP.NET Core 呼叫 n8n Webhook，再由 n8n 將回答回傳至網站。API Key 不可提交至 GitHub，每台開發電腦都需要個別設定。

## 1. 設定 n8n Webhook 驗證

1. 啟動 n8n、匯入 FAQ workflow，並確認流程已 Publish。
2. 在 n8n 建立 `Header Auth` Credential。
3. Header Name 設為：

   ```text
   X-FAQ-API-Key
   ```

4. Header Value 填入自行保管的 API Key。
5. 將這個 Credential 套用到 `Webhook` 節點。

## 2. 設定 Gemini API Key（n8n）

Gemini API Key 只供 n8n 呼叫 Gemini 模型使用，不要填入 ASP.NET 的 `appsettings`、User Secrets 或 Repository。

1. 前往 [Google AI Studio API Keys](https://aistudio.google.com/app/apikey)，建立或取得自己的 Gemini API Key。
2. 在 n8n 開啟 FAQ workflow。
3. 點擊 `Google Gemini Chat Model` 節點。
4. 在 Credential 選擇現有 Gemini Credential；若沒有，選擇建立新的 Credential。
5. 將 Gemini API Key 貼入 Credential 的 API Key 欄位並儲存。
6. 選擇目前 n8n 介面可用的 Gemini Flash 模型。
7. 執行一次 FAQ 測試，確認 AI Agent 可以正常回覆。

注意：

- n8n 匯出的 workflow JSON 不會包含 Gemini API Key；換電腦或重新建立 n8n 時，必須另外重新設定 Gemini Credential。
- Gemini API Key 不可提交到 GitHub、`appsettings.json`、`appsettings.Development.json` 或 Visual Studio User Secrets。
- ASP.NET 的 `N8n:ApiKey` 與 Gemini API Key 是不同用途的兩把 Key：
  - `N8n:ApiKey`：ASP.NET 呼叫 n8n Webhook 時的 Header 驗證。
  - Gemini API Key：n8n 呼叫 Gemini 模型時使用。

## 3. 在 Visual Studio 加入 API Key

1. 在方案總管對 `HotelManagementSystem` 專案按右鍵。
2. 選擇「管理使用者秘密（Manage User Secrets）」。
3. 在開啟的 `secrets.json` 輸入：

   ```json
   {
     "N8n": {
       "ApiKey": "請填入與 n8n Header Auth 相同的 API Key"
     }
   }
   ```

4. 儲存後重新啟動 ASP.NET 專案。

`secrets.json` 不會被提交至 GitHub；`.csproj` 中的 `UserSecretsId` 可以正常提交。

## 4. 設定 n8n／ngrok 網址

開啟 `appsettings.Development.json`，確認：

```json
"N8n": {
  "FaqWebhookUrl": "https://你的-ngrok-網址.ngrok-free.dev/webhook/hotel-faq"
}
```

注意：

- 網址結尾必須是 `/webhook/hotel-faq`。
- `/webhook-test/hotel-faq` 只供 n8n 測試監聽使用，不可作為網站正式呼叫網址。
- ngrok 網址變更時，必須同步修改 `FaqWebhookUrl`。
- 執行網站前，Docker 中的 n8n 與 ngrok Tunnel 都必須正常運作。

## 5. 換電腦開發

每台電腦都需要：

1. 啟動或匯入相同的 n8n workflow。
2. 在 n8n 設定相同 Header Name 與 API Key。
3. 在 n8n 重新建立或選擇可用的 Gemini Credential，填入自己的 Gemini API Key。
4. 透過 Visual Studio「管理使用者秘密」設定相同的 `N8n:ApiKey`。
5. 將 `FaqWebhookUrl` 改成目前可連線的 ngrok Webhook 網址。

## 6. 確認連線

開啟網站首頁並詢問一個 FAQ。能正常收到回答，即代表下列流程已接通：

```text
網站前端 → ASP.NET /Faq/Ask → n8n Webhook → AI Agent → 網站前端
```

如果只收到「客服目前暫時無法回覆」，請依序檢查 n8n 是否啟動、workflow 是否 Publish、ngrok 網址是否正確，以及兩邊的 API Key 是否完全相同。

## 正式部署

正式環境不要把 API Key 寫入 Git。請改用部署平台的環境變數：

```text
N8n__FaqWebhookUrl
N8n__ApiKey
```
