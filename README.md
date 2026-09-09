# HotelManagementSystem

**中小型連鎖商旅訂房與住宿管理系統**

以 ASP.NET Core MVC 建置的中小型連鎖商旅訂房與住宿管理系統，整合顧客訂房、房型可售量、分館住宿作業、房間狀態管理與跨分館後台管理。

專案由五人團隊共同開發，從需求規格、資料庫設計、功能實作、測試、CI 到 Azure 部署皆納入實際開發流程。

---

## 專案簡介

HotelManagementSystem 以「訂房需求」與「實際房間安排」分離為主要設計概念。

顧客訂房時選擇的是房型，而不是特定房號；系統依住宿日期與目前有效訂單計算房型可售數量。實際入住時，再由分館員工根據原訂房型、房間供應狀態與清潔狀態指派實際房間。

完整住宿流程為：

```text
查詢房型
→ 填寫訂房與付款資料
→ 模擬付款
→ 建立 Paid 訂單
→ 占用房型可售量
→ Check-in
→ 指派房號
→ 建立住房紀錄
→ Check-out
→ 房間待清潔
→ 清潔完成
```

除了核心住宿流程之外，系統亦提供：

- 顧客訂單查詢
- 分館員工營運首頁
- 訂單取消與 No-Show
- 房間保留／停用與供應風險檢查
- 系統公告
- 顧客意見回饋
- 跨分館基礎資料管理
- 員工帳號管理
- 訂單資料匯出
- 操作紀錄查詢

---

# 使用者角色

系統目前包含三種正式角色：

## 顧客

顧客不需登入即可使用公開功能。

主要功能：

- 瀏覽分館資訊
- 依分館、入住日期、退房日期及入住人數查詢可售房型
- 查看房型資訊與價格
- 填寫訂房資料
- 完成信用卡付款流程模擬
- 付款驗證成功後建立 `Paid` 訂單
- 使用訂單編號與聯絡電話查詢訂單
- 查看目前公開且有效的系統公告
- 提交顧客意見回饋

顧客訂房時只選擇房型，不直接指定實際入住房號。

---

## 分館員工

分館員工登入後，只能操作自己所屬分館的住宿營運資料。

主要功能：

- 分館員工首頁
- 查看今日待入住與待退房資訊
- 查詢本館訂單
- 處理訂單取消
- 辦理 Check-in
- 指派符合條件的房間
- 辦理 Check-out
- 處理 No-Show
- 管理房間供應狀態
- 管理房間清潔狀態
- 查看目前有效的系統公告
- 修改本人登入密碼

---

## 總系統管理員

總系統管理員負責跨分館的基礎資料與系統管理。

主要功能：

- 分館資料管理
- 房型與固定價格管理
- 房間資料管理
- 分館員工帳號管理
- 分館員工密碼重設
- 訂單資料預覽
- CSV 訂單資料匯出
- 系統重要操作紀錄查詢
- 系統公告新增、修改、啟停與刪除
- 設定公告是否公開給顧客
- 查看全部顧客意見回饋

---

# 核心業務流程

## 1. 顧客訂房

顧客選擇：

- 分館
- 入住日期
- 退房日期
- 入住人數

系統查詢符合條件且仍有可售數量的房型。

顧客選擇房型後填寫訂房資料並進入付款流程。

本專案不串接第三方金流服務，而是保留完整的信用卡輸入與驗證流程，模擬付款成功或失敗。

付款驗證成功後：

1. 建立正式訂單。
2. 訂單狀態設為 `Paid`。
3. 該住宿期間占用對應房型可售量。

付款失敗則不建立正式訂單。

> 本專案不會對任何信用卡或銀行發出實際扣款請求。

---

## 2. 可售房量計算

系統不是單純計算房間總數，而是依住宿日期逐日計算可售量。

概念上：

```text
可售量
=
目前 Open 房間數
- 該日期有效訂單占用
- 必要的逾期住房額外占用
```

查詢多晚住宿時，系統會計算住宿期間內各日剩餘房量，並以整段期間可成立的數量作為結果。

房間的 `Reserved` 或 `Disabled` 狀態不計入可供應房間。

清潔狀態則主要影響入住當下是否可以指派，不直接扣除未來住宿日期的房型可售數量。

---

## 3. Check-in

顧客抵達分館後，由分館員工查詢訂單。

符合入住條件後，系統列出可指派房間。

房間必須符合：

- 與訂單相同分館
- 與原訂房型相同
- 房間供應狀態為 Open
- 清潔狀態為 Clean
- 目前沒有其他有效住房

Check-in 成功後：

1. 建立 `StayRecord`
2. 保存實際入住房號
3. 保存實際入住時間與入住人數
4. 訂單狀態改為 `CheckedIn`

---

## 4. Check-out

分館員工完成 Check-out 後：

1. 訂單狀態改為 `Completed`
2. `StayRecord` 保存實際退房時間
3. 房間清潔狀態改為待清潔

房間完成清潔後，由員工手動更新為 Clean，才可再次安排新的入住顧客。

---

## 5. No-Show

系統依：

- 訂單狀態
- 入住／退房日期
- 目前時間
- 是否已有住房紀錄

判斷逾期仍未入住的訂單。

符合條件的訂單會轉為 `NoShow`。

系統目前不使用背景排程，而是在既有業務流程入口進行必要的狀態補判。

---

# 房間供應管理

房間供應狀態包含：

- Open
- Reserved
- Disabled

分館員工或管理員調整房間供應狀態時，系統會檢查是否可能造成原房型未來住宿日期的供應不足。

若調整可能影響既有有效訂單，系統會顯示受影響日期與不足數量，再由使用者確認是否繼續。

房間修改所屬房型時，若會直接造成原房型未來供應不足，則系統會阻擋修改。

---

# 系統公告

總系統管理員可以管理全系統公告。

公告可設定：

- 標題
- 內容
- 顯示起始時間
- 顯示結束時間
- 是否啟用
- 是否公開給顧客

顧客只會看到：

- 已啟用
- 在有效時間內
- 設定為公開

的公告。

分館員工登入後則可以唯讀查看目前有效的系統公告。

公告異動會建立對應的操作紀錄。

---

# 顧客意見回饋

顧客可從公開頁面提交意見，不需登入或提供訂單驗證。

回饋內容包含：

- 顧客姓名
- Email
- 選填電話
- 所屬分館
- 意見內容
- 建立時間

分館員工不具有顧客意見回饋的查看或操作權限。總系統管理員是唯一內部查看角色，只提供全部分館回饋的唯讀檢視，預設依 `CreatedAt` 由新至舊；不提供分館或日期篩選。

目前顧客意見回饋僅提供意見提交與唯讀檢視，不包含：

- 修改或刪除
- 回覆
- 已讀／未讀
- 處理狀態
- CSV 匯出

---

# 操作紀錄

系統對重要的內部資料異動建立 `OperationLog`。

例如：

- 分館建立與修改
- 分館接受／停止新訂房
- 房型異動
- 房間異動
- 員工帳號管理
- 訂單取消
- 公告管理

總系統管理員可以依條件查詢操作紀錄。

畫面顯示的操作對象會盡可能使用可辨識資訊，例如：

- 分館名稱
- 房型名稱
- 房號
- 訂單編號
- 員工編號

而不是只顯示資料庫流水號。

---

# 資料庫

主要資料表目前包含：

- `Branches`
- `RoomTypes`
- `Rooms`
- `Employees`
- `Bookings`
- `StayRecords`
- `OperationTypes`
- `OperationLogs`
- `CustomerFeedbacks`
- `Announcements`

資料庫使用 Microsoft SQL Server。

專案保留：

```text
database/
```

作為 Schema、Required Seed、Demo Data、Development Scenario 與後續部署 SQL 的管理位置。

既有正式資料庫的 Schema 演進則透過：

```text
database/deploy/
```

中的編號 SQL 依序套用，而不是重新建立整個 Production Database。

---

# 系統時間

住宿流程涉及：

- Check-in 時段
- Check-out 截止時間
- No-Show
- 訂單日期
- 公告有效期間
- 操作紀錄

因此系統透過集中式 `TaipeiClock` 統一取得台灣時間，避免不同功能自行取得系統時間而產生判斷差異。

測試環境則可使用 Fake Clock 固定時間，驗證時間邊界條件。

---

# 技術棧

## Backend

- C#
- .NET 10
- ASP.NET Core MVC
- Entity Framework Core
- LINQ

## Database

- Microsoft SQL Server
- T-SQL

## Frontend

- Razor Views
- HTML
- CSS
- Bootstrap
- JavaScript
- jQuery

## Testing

- xUnit
- EF Core InMemory Database
- Fake Clock

## DevOps / Deployment

- Git
- GitHub
- Pull Request workflow
- GitHub Actions
- Azure App Service
- Azure SQL

---

# 測試與 CI

專案使用 xUnit 建立核心 Service 單元測試。

目前自動化測試主要涵蓋：

- 可售房量計算
- Paid / CheckedIn 訂單占用
- 相鄰訂單
- 部分日期重疊
- 跨查詢區間住宿
- 逾期未退房
- Check-out 12:00 時間邊界
- RoomType 隔離
- No-Show 判定

GitHub Actions 在 Pull Request 合併至 `develop` 前執行：

```text
Restore
→ Build
→ Test
```

降低整合後才發現編譯錯誤或核心測試失敗的風險。

Production 部署則由 `master` 分支觸發正式部署流程。

---

# Git 協作流程

團隊以 `develop` 作為主要整合分支。

一般功能開發流程：

```text
develop
↓
feature / fix / chore branch
↓
Pull Request
↓
Code Review
↓
CI Build + Test
↓
merge into develop
```

正式版本發布：

```text
develop
↓
Release Pull Request
↓
master
↓
Production Deployment
```

透過功能分支與 Pull Request 管理各成員開發內容，避免直接在整合分支進行未驗證修改。

---

# 專案結構

```text
HotelManagementSystem
├─ Controllers/                 MVC Controllers
├─ Helper/                      小型共用 Helper
├─ Models/
│  ├─ Entities/                 EF Core Entities
│  └─ ViewModels/               頁面資料模型
├─ Services/                    共用業務邏輯
├─ Views/                       Razor Views
├─ database/
│  ├─ deploy/                   已部署資料庫增量 SQL
│  ├─ 01_create_...             Fresh Schema
│  ├─ 02_required_seed.sql      必要基準資料
│  ├─ 03_demo_data.sql          Demo 資料
│  └─ 04_development_...        開發情境資料
├─ HotelManagementSystem.Tests/ xUnit Tests
├─ wwwroot/                     CSS、JavaScript、Images
└─ .github/workflows/           CI / Production Deployment
```

---

# 版本規劃

## v1.0.0

第一個 Production 基準版本。

主要完成：

- 顧客訂房
- 模擬付款
- 訂單查詢
- 可售房量計算
- 分館員工登入
- 分館營運首頁
- 訂單取消
- No-Show
- Check-in
- Check-out
- 房號指派
- 住房紀錄
- 房間清潔
- 房間供應狀態管理
- 分館／房型／房間管理
- 分館員工帳號管理
- 訂單 CSV 匯出
- 操作紀錄
- xUnit 核心單元測試
- GitHub Actions CI
- Azure Production Deployment

## v1.1.0

主要新增：

- 系統公告
- 顧客意見回饋
- 顧客端 UI / UX 調整
- 正式展示文案與操作體驗整理

## v1.2.0（規劃中）

預計整合：

- 訂房成功 Email 通知
- Email 補寄流程
- AI Q&A / FAQ 旅宿小助手

---

# 專案目前狀態

核心訂房與住宿管理流程已完成，並已建立 Production 部署與版本發布流程。

目前版本已涵蓋：

- 顧客訂房
- 分館住宿營運
- 房間供應與清潔
- 跨分館系統管理
- 系統公告
- 顧客意見回饋
- 自動化核心測試
- GitHub Actions CI
- Azure 部署

後續開發以既有正式規格與版本規劃為基礎，持續進行功能延伸與整合，而不再重新設計已確認的核心住宿流程。

---

# Disclaimer

本系統為軟體工程學習與專題開發作品。

系統中的：

- 商旅名稱
- 地址
- 電話
- 房型
- 房價
- 圖片
- 顧客及訂單資料

皆為示範或測試用途資料。

付款功能亦為模擬流程，不會產生實際金融交易。
