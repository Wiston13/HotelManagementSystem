Database Deploy SQL

此資料夾保存已部署資料庫後續需要執行的增量 SQL。

## 使用方式

### 全新資料庫

直接使用最新版：

```text
01_create_hotel_management_schema.sql
02_required_seed.sql
```

如果是本機開發或展示環境，再依需要執行：

```text
03_demo_data.sql
04_development_scenarios.sql
```

全新資料庫已經是最新 Schema，不需要再執行 `deploy/` 內的歷史增量 SQL。

### 已部署資料庫

如果資料庫已經存在並需要保留原有資料，就使用本資料夾中的 SQL 依編號順序更新：

```text
001_add_customer_feedbacks.sql
002_add_announcements.sql
...
```

只執行該環境尚未套用的 SQL，不要重新執行已經套用過的檔案。

目前專案沒有自動 migration tracking 或 `SchemaVersions` table。每次正式資料庫更新完成後，必須在部署紀錄、Release 或對應 PR 中記載該環境最後成功套用的 deploy SQL，避免之後無法判斷 Production 已執行到哪一支增量 SQL。

執行前請先確認目前連線的是正確資料庫。

## 目前 Deploy SQL

### `001_add_customer_feedbacks.sql`

新增：

- `CustomerFeedbacks`
- 相關 FK、Constraint、Index

### `002_add_announcements.sql`

新增／調整：

- `Announcements`
- `OperationLogs.TargetBranchId` 改為 nullable
- 公告相關 `OperationTypes`

如果某個既有資料庫兩支都尚未套用，執行順序為：

```text
001_add_customer_feedbacks.sql
→
002_add_announcements.sql
```

## 注意事項

- 已執行過的 deploy SQL 不要回頭修改。
- 後續 Schema 若還需要調整，新增下一個流水號 SQL。
- 正式資料庫不要使用 `01_create_hotel_management_schema.sql` 重新建立。
- `03_demo_data.sql` 與 `04_development_scenarios.sql` 不應執行在正式資料庫。
