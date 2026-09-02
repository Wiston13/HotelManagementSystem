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

## 2. 在 Visual Studio 加入 API Key

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

## 3. 設定 n8n／ngrok 網址

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

## 4. 換電腦開發

每台電腦都需要：

1. 啟動或匯入相同的 n8n workflow。
2. 在 n8n 設定相同 Header Name 與 API Key。
3. 透過 Visual Studio「管理使用者秘密」設定相同的 `N8n:ApiKey`。
4. 將 `FaqWebhookUrl` 改成目前可連線的 ngrok Webhook 網址。

## 5. 確認連線

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
