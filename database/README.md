# 中小型連鎖商旅訂房與住宿管理系統｜Database Scripts

本目錄保存目前完整資料庫 Schema、必要初始化資料，以及只供本機開發、測試與展示使用的 Demo／Scenario 資料。請依環境選擇正確的初始化方式。

## 1. Database Scripts

### `01_create_hotel_management_schema.sql`

建立目前完整 Schema，包含資料表、PK、FK、UNIQUE、CHECK、DEFAULT 與 Index。

這是可重建 Schema：腳本會依相依順序刪除既有資料表後重新建立，只可用於：

- 本機開發或測試環境。
- 全新的空白資料庫。
- 第一次部署建立全新資料庫。

第一次正式部署完成後，不得再用本腳本重建已部署資料庫。本腳本不是 migration。

### `02_required_seed.sql`

只建立系統正常使用所需的必要初始化資料：

- `OperationTypes` 1～25。
- 初始 `SystemAdmin`。

本腳本不是 Demo Data，不包含分館、一般員工、房型、房間、訂單或住房資料。

Repository 內的固定密碼 `Hotel@123` 與 PasswordHash 只供本機開發／Demo。正式部署時：

1. 保留完整 `OperationTypes`。
2. 第一次正式部署前，直接配置正式 `SystemAdmin` 密碼的 PasswordHash。
3. PasswordHash 必須與 ASP.NET Identity `PasswordHasher<Employee>` 相容。
4. 部署用明碼與 hash 不得提交至版本控制。

詳細規則見 [deploy/README.md](deploy/README.md)。

### `03_demo_data.sql`

建立本機開發與展示所需的 Demo 基礎資料：

- 6 間分館。
- 24 種房型。
- 188 間房間。
- 18 位一般分館員工。

執行前必須先完成 `01`、`02`。本腳本會清除並重建 Demo 相關資料，不可對正式部署資料庫執行。所有測試帳號固定使用 `Hotel@123`，只供本機／Demo。

### `04_development_scenarios.sql`

建立可供人工操作、Smoke Test 與展示使用的動態情境：

- 46 筆 Booking。
- 9 筆 StayRecord。
- 53 筆 OperationLog。
- 5 筆 CustomerFeedbacks：分館 1（台北中山）、2（台北信義）、3（台中草悟），含有／無電話及同 Email 重複提交。
- 回饋日期為台灣時間今天、前 1、3、7 天，供本館／跨館查詢、日期篩選、排序與 CSV 匯出情境使用。
- Paid、CheckedIn、Completed、Cancelled、NoShow 狀態。
- Check-in、Check-out、No-show、取消、房況、清潔、房量重疊與容量臨界情境。
- `OperationTypeId` 1～25 coverage。

本腳本依執行當天的台灣日期產生可持續使用的情境，必須建立在 `01 → 02 → 03` 之後。它不取代 Unit Test 或 Integration Test，也不可對正式部署資料庫執行。

`03` 會先清除 CustomerFeedbacks 再重建分館；`04` 單獨重跑會清除並重建 5 筆回饋（含開發時自行新增的回饋），沿用交易、固定 Identity 與台灣時間慣例。

### 顧客意見回饋資料模型

SQL 表名固定為 `dbo.CustomerFeedbacks`；EF 使用 `Feedback` Entity 與 `Feedbacks` DbSet，透過 Fluent Mapping 明確指定表名。

| 欄位 | SQL 型別 | 限制 |
| --- | --- | --- |
| Id | int | PK、IDENTITY(1,1)、NOT NULL |
| BranchId | int | NOT NULL、FK → Branches.BranchId、ON DELETE NO ACTION |
| CustomerName | nvarchar(254) | NOT NULL、不可空字串／全由半形空格組成 |
| Email | nvarchar(254) | NOT NULL、不可空字串／全由半形空格組成；不設 UNIQUE |
| Phone | nvarchar(20) | NULL 或 1～20 碼 ASCII 0～9 |
| Content | nvarchar(500) | NOT NULL、不可空字串／全由半形空格組成 |
| CreatedAt | datetime2(0) | NOT NULL、DEFAULT 明確轉成 Taipei Standard Time |

上述必填 CHECK 排除空字串與全由半形空格組成的值；後端仍須驗證所有空白輸入、Email 格式、有效分館及各欄位長度。電話寫入前須移除空白與半形連字號，未填保存 NULL，再驗證僅含 0～9 且不超過 20 碼。

`CreatedAt` 設為新增時由資料庫產生；新 Entity 不指定時間時使用 SQL DEFAULT。索引為 `(BranchId, CreatedAt DESC)`（本館／指定分館）與 `(CreatedAt DESC)`（全部分館日期範圍）。

本表只保存七欄；不含處理狀態、備註／回覆、訂單／住房關聯或通知紀錄。顧客提交不要求登入或訂單驗證；內部依角色查詢／匯出皆為唯讀，相關授權、輸入驗證與畫面由功能分支實作。

型別與長度依 Notion「新增資料表」；七欄、單向唯讀及台灣時間依「顧客意見回饋功能規格補充」與本次確認。舊筆記中的 Status 與 SYSDATETIME() 不採用。本次只更新完整建庫與開發資料；既有部署資料庫若要加入此表，另行安排增量 SQL，不可用 `01` 重建升級。

## 2. 本機初始化流程

### 最小可運作資料庫

只建立 Schema 與必要初始化資料：

```text
01 → 02
```

### Demo／完整人工測試資料庫

```text
01 → 02 → 03 → 04
```

Windows 驗證登入與 SQL Server `sqlcmd` 範例：

```powershell
sqlcmd -S . -E -C -b -f 65001 -i .\database\01_create_hotel_management_schema.sql
sqlcmd -S . -E -C -b -f 65001 -i .\database\02_required_seed.sql
sqlcmd -S . -E -C -b -f 65001 -i .\database\03_demo_data.sql
sqlcmd -S . -E -C -b -f 65001 -i .\database\04_development_scenarios.sql
```

`-S .` 只是本機 SQL Server 預設執行個體範例；請依實際環境調整伺服器與驗證參數。

## 3. 第一次正式部署

第一次部署本身就是 Database Baseline，不建立虛構的初始 migration。流程摘要：

```text
全新空白部署資料庫
→ 執行目前最新版完整 Schema
→ 執行正式部署版本的 Required Seed／Bootstrap
→ 啟動應用程式
→ Smoke Test
```

正式部署不得執行 `03_demo_data.sql` 或 `04_development_scenarios.sql`。第一次部署完成後，既有資料庫只能使用 `database/deploy/NNN_description.sql` 逐步更新，詳細規則見 [deploy/README.md](deploy/README.md)。

## 4. 環境使用界線

| 環境 | 允許的資料庫腳本 |
| --- | --- |
| Fresh Database | 使用最新版 `01` 建立完整 Schema；依用途選擇後續資料。 |
| Local / Test / Demo | 可依序執行 `01 → 02 → 03 → 04`。 |
| First Deployment | 對全新空白 DB 執行最新版 Schema 與正式部署專用 Required Seed／Bootstrap。 |
| Existing Deployed Database | 只依序執行尚未套用的 `deploy/NNN_description.sql`；不得重跑 `01`、`03`、`04`。 |
