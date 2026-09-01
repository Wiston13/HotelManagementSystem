# Database Deployment Baseline 與增量部署 SQL

本目錄定義第一次部署完成後，已部署資料庫如何進行 Schema 與 Required Data 演進。它不是企業級資料庫維運手冊；目前由部署流程依序執行 SQL。

## 1. 第一次部署與 Database Baseline

第一次部署以前沒有 migration history。第一次部署使用：

- 當下最新版 `01_create_hotel_management_schema.sql`。
- 正式部署版本的 Required Seed／Bootstrap。

第一次部署完成後，該部署資料庫即成為 Database Deployment Baseline。因此不要建立 `001_initial_schema.sql` 或 `001_baseline.sql`，去表示一段實際不存在的 migration history。

第一支真正的 `deploy/001`，必須是第一次部署後首次需要修改既有資料庫的功能。例如未來若新增公告功能並需要資料庫變更，檔名可能是 `001_add_announcements.sql`；這只是命名範例，本輪沒有建立該 SQL，也不代表公告 Schema 已定案。

## 2. Production 與 Demo／Scenario 界線

Production／已部署資料庫允許：

- 第一次 Database Baseline 初始化。
- `deploy/NNN_description.sql` 增量部署 SQL。

第一次部署完成後，禁止：

- 使用 `01_create_hotel_management_schema.sql` DROP／重建已部署資料庫。
- 執行 `03_demo_data.sql`。
- 執行 `04_development_scenarios.sql`。

Local／Test／Demo 環境可依序使用 `01 → 02 → 03 → 04`。

## 3. Schema 修改必須維護兩條路徑

### Fresh Database

每次 Schema 變更都要更新 `01_create_hotel_management_schema.sql`，讓新的開發環境從零建立時直接取得目前最新版完整 Schema。

只有新增系統必要固定資料時，才同步更新 `02_required_seed.sql`。

### Existing Deployed Database

同一次變更要新增 `database/deploy/NNN_description.sql`，讓已有資料的部署 DB 從前一狀態安全更新。

兩條路徑責任不同：

- `01`：最新版完整 Schema。
- `deploy/NNN`：既有部署版本之間的增量更新。

不得把 migration SQL 依序堆回 `01`；`01` 應直接描述更新後的完整結果。

## 4. Deploy SQL 命名與執行順序

檔名固定使用三位數流水號：

```text
NNN_description.sql
```

例如：

```text
001_add_announcements.sql
002_add_customer_feedback.sql
```

一支 script 對應一個明確的 Schema／Required Data 變更。不得使用日期命名、不得重複流水號。

部署既有資料庫時，依檔名編號順序執行尚未套用的 SQL：

```text
001 → 002 → 003
```

不得跳號或改變順序。專案目前沒有自動 migration tracking，因此 deployment 紀錄或 PR 必須明確記載各部署環境最後套用的 deploy SQL；本規則不要求新增 Schema version table。

## 5. 已執行的 Deploy SQL 不可修改

一旦 `NNN_description.sql` 已套用到任何部署 DB，就不得回頭修改內容。若已執行的 `001` 仍需修正，應新增下一支，例如 `002_fix_xxx.sql`。

原因是其他部署環境可能已執行原始版本；修改歷史檔會使不同環境無法確認實際套用狀態。

## 6. Deploy SQL 基本要求

每支增量部署 SQL：

- 只處理該次必要變更。
- 必須保留既有正式資料，不得 DROP 全部資料表後重建。
- FK、Constraint 與 Index 的最終狀態須和最新版 `01` 一致。
- 涉及資料轉換時，必須先考慮既有 rows。
- 執行失敗時應提供清楚錯誤。
- 適合時使用 transaction 與 `XACT_ABORT`。
- 原則上只執行一次，不要求每支都能無限重跑。

目前不要求導入 migration framework、自製 migration runner 或 SchemaVersions table。

## 7. Required Seed 的後續演進

如果未來新增 `OperationTypes` 這類系統必要固定資料：

- Fresh DB：更新 `02_required_seed.sql`。
- Existing DB：在對應的 `deploy/NNN_description.sql` 插入新增的必要資料。

正式環境不得為此重新執行整支 `02_required_seed.sql`。

## 8. SystemAdmin 正式部署規則

Repository 內的 `Hotel@123` 只是 Demo credential，正式第一次部署不得直接使用。

部署時必須：

1. 在第一次正式部署前，直接配置正式 `SystemAdmin` 密碼的 PasswordHash。
2. 可在部署環境準備不提交版本控制的 Required Seed／Bootstrap 內容，或只替換 Repository Seed 中 `SystemAdmin` 的 `PasswordHash`。
3. PasswordHash 必須與 ASP.NET Identity `PasswordHasher<Employee>` 相容。
4. 部署用明碼與 hash 不得寫入 `appsettings.json`、README、Repository SQL 或 Git commit。

## 9. 部署前後基本驗證

### 第一次部署

- Schema 建立成功。
- Required Seed 執行成功。
- `OperationTypes` 存在。
- 正式 `SystemAdmin` PasswordHash 已於部署前配置完成。
- 正式 `SystemAdmin` 可登入。
- 應用程式可啟動。
- 核心頁面可讀取資料。

### 後續 migration

- deploy SQL 執行成功。
- 應用程式 build／test 通過。
- 新功能可用。
- 原核心流程 Smoke Test 通過。
