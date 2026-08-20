# 開發資料庫重建順序

下列腳本只適用於本機開發／測試資料庫；`01` 會重建 Schema，`02` 與 `03` 會清除既有資料。

1. `01_create_hotel_management_schema.sql`：重建目前正式使用的八張資料表、約束與索引。
2. `02_sample_data.sql`：建立穩定基準資料（分館、房型、房間、員工、操作類型）。
3. `03_development_scenarios.sql`：依執行當天台灣日期建立訂單、住房、取消、No-show 與操作紀錄情境。
4. `05_development_volume_data.sql`：可選；增加 2,000 筆訂單、1,148 筆住房與 3,648 筆操作紀錄，供查詢、分頁、匯出及統計測試。
5. `04_validate_sample_data.sql`：唯讀摘要與資料一致性檢查。

PowerShell 範例（Windows 驗證登入，SQL Server 預設執行個體）：

```powershell
sqlcmd -S . -E -C -b -f 65001 -i .\database\01_create_hotel_management_schema.sql
sqlcmd -S . -E -C -b -f 65001 -i .\database\02_sample_data.sql
sqlcmd -S . -E -C -b -f 65001 -i .\database\03_development_scenarios.sql
sqlcmd -S . -E -C -b -f 65001 -i .\database\05_development_volume_data.sql
sqlcmd -S . -E -C -b -f 65001 -i .\database\04_validate_sample_data.sql
```

`02`、`03` 與 `05` 都可依上述順序重跑。若只需重建動態情境，基準資料未被修改時可重跑 `03`，需要量體時再跑 `05`，最後執行 `04` 驗證。

`03` 保留 42 筆容易辨識的核心驗收情境；`05` 使用獨立固定編號範圍建立大量營運資料，不會覆蓋核心訂單。若只需要開發單一流程，可略過 `05`，避免大量資料干擾人工查找。

所有測試帳號的開發密碼皆為 `Hotel@123`。固定密碼雜湊只供本機開發與展示，不得用於正式環境。
