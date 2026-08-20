# 開發資料庫重建順序

下列腳本只適用於本機開發／測試資料庫；`01` 會重建 Schema，`02` 與 `03` 會清除既有資料。

1. `01_create_hotel_management_schema.sql`：重建目前正式使用的八張資料表、約束與索引。
2. `02_sample_data.sql`：建立穩定基準資料（分館、房型、房間、員工、操作類型）。
3. `03_development_scenarios.sql`：依執行當天台灣日期建立訂單、住房、取消、No-show 與操作紀錄情境。
4. `04_validate_sample_data.sql`：唯讀摘要與資料一致性檢查。

PowerShell 範例（Windows 驗證登入，SQL Server 預設執行個體）：

```powershell
sqlcmd -S . -E -C -b -f 65001 -i .\database\01_create_hotel_management_schema.sql
sqlcmd -S . -E -C -b -f 65001 -i .\database\02_sample_data.sql
sqlcmd -S . -E -C -b -f 65001 -i .\database\03_development_scenarios.sql
sqlcmd -S . -E -C -b -f 65001 -i .\database\04_validate_sample_data.sql
```

`02` 與 `03` 都可依上述順序重跑。若只需重建動態情境，基準資料未被修改時可單獨重跑 `03`，再執行 `04` 驗證。

所有測試帳號的開發密碼皆為 `Hotel@123`。固定密碼雜湊只供本機開發與展示，不得用於正式環境。
