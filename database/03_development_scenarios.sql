/*
    HotelManagementSystem - 第一版開發情境資料
    SQL Server

    前置：先依序執行 01_create_hotel_management_schema.sql、02_sample_data.sql。

    本檔責任：
    - 重設情境用房間供應／清潔狀態
    - 訂單、住房紀錄、取消、No-show、操作紀錄
    - 所有相對日期以執行當下的台灣日期為基準

    可重跑方式：
    - 可在 02 之後重複單獨執行本檔。
    - 本檔只清除情境表，並把 02 建立的 188 間房重設為固定情境；
      不重建分館、房型、員工或操作類型。
*/

USE [HotelManagementSystem];
GO

/* sqlcmd 執行本檔時請加 -f 65001，避免 UTF-8 中文字串被錯誤解碼。 */
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @IdentityInsertTable nvarchar(128) = NULL;

BEGIN TRY
    BEGIN TRANSACTION;

    IF (SELECT COUNT(*) FROM [dbo].[Branches]) <> 6
       OR (SELECT COUNT(*) FROM [dbo].[RoomTypes]) <> 24
       OR (SELECT COUNT(*) FROM [dbo].[Rooms]) <> 188
       OR (SELECT COUNT(*) FROM [dbo].[OperationTypes]) <> 23
    BEGIN
        THROW 50002, N'基準資料不完整，請先執行 02_sample_data.sql。', 1;
    END;

    DELETE FROM [dbo].[OperationLogs];
    DELETE FROM [dbo].[StayRecords];
    DELETE FROM [dbo].[Bookings];

    DECLARE @NowTaipei datetime2(0) =
        CONVERT(datetime2(0), SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time');
    DECLARE @Today date = CAST(@NowTaipei AS date);

    /* =========================================================
       1. 固定房間情境

       先重設所有房間，確保本檔在開發操作後仍能恢復同一基準。
       入住中狀態稍後由有效 StayRecord 推導，不另存欄位。
       ========================================================= */
    UPDATE [dbo].[Rooms]
    SET [SupplyStatus] = 'Open',
        [CleaningStatus] = 'Clean',
        [DisabledReason] = NULL;

    /* 台北中山館：六種供應／清潔組合。 */
    UPDATE [dbo].[Rooms] SET [SupplyStatus] = 'Reserved' WHERE [RoomId] = 2;
    UPDATE [dbo].[Rooms] SET [CleaningStatus] = 'NeedsCleaning' WHERE [RoomId] = 3;
    UPDATE [dbo].[Rooms] SET [SupplyStatus] = 'Reserved', [CleaningStatus] = 'NeedsCleaning' WHERE [RoomId] = 4;
    UPDATE [dbo].[Rooms] SET [SupplyStatus] = 'Disabled', [DisabledReason] = N'空調主機異常，等待維修廠商到場。' WHERE [RoomId] = 5;
    UPDATE [dbo].[Rooms] SET [SupplyStatus] = 'Disabled', [CleaningStatus] = 'NeedsCleaning', [DisabledReason] = N'浴室防水施工中，完工後需重新清潔。' WHERE [RoomId] = 6;

    /* 行政三人房：唯一 Open + Clean 房間 601 會有有效住房，其餘皆不可指派。 */
    UPDATE [dbo].[Rooms] SET [CleaningStatus] = 'NeedsCleaning' WHERE [RoomId] = 36;
    UPDATE [dbo].[Rooms] SET [SupplyStatus] = 'Reserved' WHERE [RoomId] = 37;
    UPDATE [dbo].[Rooms] SET [SupplyStatus] = 'Disabled', [DisabledReason] = N'門鎖讀卡機故障。' WHERE [RoomId] = 38;
    UPDATE [dbo].[Rooms] SET [SupplyStatus] = 'Disabled', [CleaningStatus] = 'NeedsCleaning', [DisabledReason] = N'窗簾軌道施工並待清潔。' WHERE [RoomId] = 39;

    /* 其他分館保留差異，供房間狀態頁與跨館資料隔離測試。 */
    UPDATE [dbo].[Rooms] SET [SupplyStatus] = 'Reserved' WHERE [RoomId] IN (40, 74, 105, 127, 168, 183);
    UPDATE [dbo].[Rooms] SET [CleaningStatus] = 'NeedsCleaning' WHERE [RoomId] IN (41, 75, 126, 169, 184);
    UPDATE [dbo].[Rooms] SET [SupplyStatus] = 'Reserved', [CleaningStatus] = 'NeedsCleaning' WHERE [RoomId] IN (42, 106, 128, 170, 185);
    UPDATE [dbo].[Rooms] SET [SupplyStatus] = 'Disabled', [DisabledReason] = N'設備年度檢修中。' WHERE [RoomId] IN (43, 76, 107, 129, 171, 186);
    UPDATE [dbo].[Rooms] SET [SupplyStatus] = 'Disabled', [CleaningStatus] = 'NeedsCleaning', [DisabledReason] = N'施工封閉並等待清潔驗收。' WHERE [RoomId] IN (44, 77, 108, 130, 172, 187);

    /* 已退房後待清潔：對應 Completed 住房。 */
    UPDATE [dbo].[Rooms] SET [CleaningStatus] = 'NeedsCleaning' WHERE [RoomId] IN (50, 84);

    /* =========================================================
       2. Bookings：42 筆可辨識測試矩陣

       001～019：一般 Paid（合法入住、今日 16:00、未來、跨館、匯出）
       020～023：CheckedIn（一般、今日退房、逾期未退、提早退房）
       024、025、029、041、042：Completed
       026、034、035、040：歷史 NoShow
       027、028：仍為 Paid 的 NoShowService 候選
       030～033、039：兩種取消因素
       036～038：重疊與相鄰房量
       ========================================================= */
    INSERT INTO [dbo].[Bookings]
    (
        [BookingNumber], [BranchId], [RoomTypeId], [BookerName], [ContactPhone], [Email],
        [CheckInDate], [CheckOutDate], [RoomTypeNameSnapshot], [MaxOccupancySnapshot],
        [NightlyPriceSnapshot], [TotalAmount], [BookingStatus], [CreatedAt],
        [CancellationCause], [CancellationReason], [CancelledAt], [CancelledByEmployeeNumber]
    )
    VALUES
    ('BK202608070001', 1, 2, N'陳冠宇', '0912345001', 'guest001@example.com',
        DATEADD(DAY,-1,@Today), DATEADD(DAY, 1,@Today), N'標準雙人房', 2, 3200.00,  6400.00, 'Paid',
        DATEADD(MINUTE,555,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070002', 1, 2, N'李佳穎', '0912345002', 'guest002@example.com',
        @Today, DATEADD(DAY,2,@Today), N'標準雙人房', 2, 3200.00, 6400.00, 'Paid',
        DATEADD(MINUTE,610,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070003', 1, 3, N'周柏勳', '0912345003', 'guest003@example.com',
        DATEADD(DAY,5,@Today), DATEADD(DAY,7,@Today), N'豪華雙床房', 2, 3800.00, 7600.00, 'Paid',
        DATEADD(MINUTE,680,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070004', 1, 5, N'林書妍', '0912345004', 'guest004@example.com',
        DATEADD(DAY,-1,@Today), DATEADD(DAY,1,@Today), N'行政三人房', 3, 4500.00, 9000.00, 'Paid',
        DATEADD(MINUTE,725,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070005', 1, 4, N'吳家豪', '0912345005', 'guest005@example.com',
        DATEADD(DAY,-1,@Today), DATEADD(DAY,2,@Today), N'家庭四人房', 4, 5200.00, 15600.00, 'Paid',
        DATEADD(MINUTE,845,CAST(DATEADD(DAY,-7,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070006', 2, 7, N'許博翔', '0912345006', 'guest006@example.com',
        DATEADD(DAY,3,@Today), DATEADD(DAY,5,@Today), N'景觀雙人房', 2, 4200.00, 8400.00, 'Paid',
        DATEADD(MINUTE,540,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070007', 3,10, N'黃筱涵', '0912345007', 'guest007@example.com',
        DATEADD(DAY,10,@Today), DATEADD(DAY,12,@Today), N'標準雙人房', 2, 2900.00, 5800.00, 'Paid',
        DATEADD(MINUTE,600,CAST(DATEADD(DAY,-3,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070008', 4,14, N'楊宗翰', '0912345008', 'guest008@example.com',
        DATEADD(DAY,1,@Today), DATEADD(DAY,4,@Today), N'古都雙人房', 2, 2700.00, 8100.00, 'Paid',
        DATEADD(MINUTE,735,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070009', 5,18, N'謝宜庭', '0912345009', 'guest009@example.com',
        DATEADD(DAY,6,@Today), DATEADD(DAY,9,@Today), N'港景雙人房', 2, 3300.00, 9900.00, 'Paid',
        DATEADD(MINUTE,810,CAST(DATEADD(DAY,-4,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070010', 6,22, N'張凱翔', '0912345010', 'guest010@example.com',
        DATEADD(DAY,4,@Today), DATEADD(DAY,6,@Today), N'山海雙人房', 2, 2600.00, 5200.00, 'Paid',
        DATEADD(MINUTE,795,CAST(DATEADD(DAY,-8,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070011', 2, 9, N'劉思妤', '0912345011', 'guest011@example.com',
        DATEADD(DAY,14,@Today), DATEADD(DAY,18,@Today), N'家庭套房', 4, 6500.00, 26000.00, 'Paid',
        DATEADD(MINUTE,565,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070012', 3,12, N'王柏鈞', '0912345012', 'guest012@example.com',
        DATEADD(DAY,2,@Today), DATEADD(DAY,5,@Today), N'家庭四人房', 4, 4800.00, 14400.00, 'Paid',
        DATEADD(MINUTE,705,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070013', 4,15, N'陳怡安', '0912345013', 'guest013@example.com',
        DATEADD(DAY,8,@Today), DATEADD(DAY,9,@Today), N'庭院雙床房', 2, 3400.00, 3400.00, 'Paid',
        DATEADD(MINUTE,845,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070014', 5,19, N'郭家宏', '0912345014', 'guest014@example.com',
        DATEADD(DAY,20,@Today), DATEADD(DAY,23,@Today), N'豪華雙床房', 2, 3900.00, 11700.00, 'Paid',
        DATEADD(MINUTE,930,CAST(DATEADD(DAY,-3,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070015', 6,24, N'蔡依婷', '0912345015', 'guest015@example.com',
        DATEADD(DAY,7,@Today), DATEADD(DAY,10,@Today), N'家庭四人房', 4, 4400.00, 13200.00, 'Paid',
        DATEADD(MINUTE,670,CAST(DATEADD(DAY,-9,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070016', 1, 2, N'趙建國', '0912345016', 'guest016@example.com',
        DATEADD(DAY,30,@Today), DATEADD(DAY,32,@Today), N'標準雙人房', 2, 3200.00, 6400.00, 'Paid',
        DATEADD(MINUTE,580,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070017', 2, 6, N'洪雅雯', '0912345017', 'guest017@example.com',
        DATEADD(DAY,45,@Today), DATEADD(DAY,46,@Today), N'都會單人房', 1, 2600.00, 2600.00, 'Paid',
        DATEADD(MINUTE,640,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070018', 3,13, N'鄭文傑', '0912345018', 'guest018@example.com',
        DATEADD(DAY,55,@Today), DATEADD(DAY,58,@Today), N'和風雙人房', 2, 3500.00, 10500.00, 'Paid',
        DATEADD(MINUTE,780,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070019', 5,17, N'曾郁婷', '0912345019', 'guest019@example.com',
        DATEADD(DAY,1,@Today), DATEADD(DAY,2,@Today), N'商務單人房', 1, 2400.00, 2400.00, 'Paid',
        DATEADD(MINUTE,860,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),

    ('BK202608070020', 1, 2, N'周子晴', '0912345020', 'guest020@example.com',
        DATEADD(DAY,-1,@Today), DATEADD(DAY,1,@Today), N'標準雙人房', 2, 3200.00, 6400.00, 'CheckedIn',
        DATEADD(MINUTE,520,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070021', 1, 5, N'彭俊傑', '0912345021', 'guest021@example.com',
        DATEADD(DAY,-2,@Today), @Today, N'行政三人房', 3, 4500.00, 9000.00, 'CheckedIn',
        DATEADD(MINUTE,590,CAST(DATEADD(DAY,-8,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070022', 3,10, N'蘇怡靜', '0912345022', 'guest022@example.com',
        DATEADD(DAY,-5,@Today), DATEADD(DAY,-2,@Today), N'標準雙人房', 2, 2900.00, 8700.00, 'CheckedIn',
        DATEADD(MINUTE,620,CAST(DATEADD(DAY,-12,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070023', 4,14, N'游承恩', '0912345023', 'guest023@example.com',
        DATEADD(DAY,-1,@Today), DATEADD(DAY,4,@Today), N'古都雙人房', 2, 2700.00, 13500.00, 'CheckedIn',
        DATEADD(MINUTE,710,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),

    ('BK202608070024', 2, 7, N'黃詩涵', '0912345024', 'guest024@example.com',
        DATEADD(DAY,-6,@Today), DATEADD(DAY,-4,@Today), N'景觀雙人房', 2, 4200.00, 8400.00, 'Completed',
        DATEADD(MINUTE,860,CAST(DATEADD(DAY,-14,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070025', 5,18, N'方映辰', '0912345025', 'guest025@example.com',
        DATEADD(DAY,-10,@Today), DATEADD(DAY,-8,@Today), N'港景雙人房', 2, 3300.00, 6600.00, 'Completed',
        DATEADD(MINUTE,575,CAST(DATEADD(DAY,-18,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070026', 1, 1, N'許家寧', '0912345026', 'guest026@example.com',
        DATEADD(DAY,-5,@Today), DATEADD(DAY,-3,@Today), N'經典單人房', 1, 2300.00, 4600.00, 'NoShow',
        DATEADD(MINUTE,530,CAST(DATEADD(DAY,-11,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070027', 2, 6, N'何志明', '0912345027', 'guest027@example.com',
        DATEADD(DAY,-4,@Today), DATEADD(DAY,-1,@Today), N'都會單人房', 1, 2600.00, 7800.00, 'Paid',
        DATEADD(MINUTE,545,CAST(DATEADD(DAY,-10,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070028', 3,13, N'徐安琪', '0912345028', 'guest028@example.com',
        DATEADD(DAY,-1,@Today), @Today, N'和風雙人房', 2, 3500.00, 3500.00, 'Paid',
        DATEADD(MINUTE,625,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070029', 6,23, N'杜佩玲', '0912345029', 'guest029@example.com',
        DATEADD(DAY,-7,@Today), DATEADD(DAY,-5,@Today), N'山景雙床房', 2, 3200.00, 6400.00, 'Completed',
        DATEADD(MINUTE,700,CAST(DATEADD(DAY,-15,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),

    ('BK202608070030', 1, 4, N'鍾雅婷', '0912345030', 'guest030@example.com',
        DATEADD(DAY,10,@Today), DATEADD(DAY,12,@Today), N'家庭四人房', 4, 5200.00, 10400.00, 'Cancelled',
        DATEADD(MINUTE,570,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))),
        'GuestRequest', N'顧客行程變更，於入住日前提出取消。', DATEADD(MINUTE,630,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), 'E20260807002'),
    ('BK202608070031', 2, 8, N'羅淑芬', '0912345031', 'guest031@example.com',
        DATEADD(DAY,2,@Today), DATEADD(DAY,4,@Today), N'豪華雙床房', 2, 4600.00, 9200.00, 'Cancelled',
        DATEADD(MINUTE,660,CAST(DATEADD(DAY,-7,@Today) AS datetime2(0))),
        'HotelUnableToFulfill', N'原房型電力設備檢修，分館已確認無法依原訂單履約。', DATEADD(MINUTE,680,CAST(@Today AS datetime2(0))), 'E20260807004'),
    ('BK202608070032', 4,16, N'廖信宏', '0912345032', 'guest032@example.com',
        DATEADD(DAY,12,@Today), DATEADD(DAY,14,@Today), N'家庭四人房', 4, 4500.00, 9000.00, 'Cancelled',
        DATEADD(MINUTE,750,CAST(DATEADD(DAY,-4,@Today) AS datetime2(0))),
        'GuestRequest', N'顧客家庭活動取消，於期限內完成核對。', DATEADD(MINUTE,800,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), 'E20260807013'),
    ('BK202608070033', 5,20, N'江美華', '0912345033', 'guest033@example.com',
        @Today, DATEADD(DAY,2,@Today), N'家庭四人房', 4, 5000.00, 10000.00, 'Cancelled',
        DATEADD(MINUTE,480,CAST(DATEADD(DAY,-9,@Today) AS datetime2(0))),
        'HotelUnableToFulfill', N'合法入住時原房型全部房間經確認均無法提供。', DATEADD(MINUTE,600,CAST(@Today AS datetime2(0))), 'E20260807005'),
    ('BK202608070034', 4,14, N'邱冠宇', '0912345034', 'guest034@example.com',
        DATEADD(DAY,-8,@Today), DATEADD(DAY,-7,@Today), N'古都雙人房', 2, 2700.00, 2700.00, 'NoShow',
        DATEADD(MINUTE,515,CAST(DATEADD(DAY,-14,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070035', 6,22, N'謝佩如', '0912345035', 'guest035@example.com',
        DATEADD(DAY,-4,@Today), DATEADD(DAY,-2,@Today), N'山海雙人房', 2, 2600.00, 5200.00, 'NoShow',
        DATEADD(MINUTE,625,CAST(DATEADD(DAY,-12,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),

    ('BK202608070036', 5,18, N'潘宥辰', '0912345036', 'guest036@example.com',
        DATEADD(DAY,10,@Today), DATEADD(DAY,13,@Today), N'港景雙人房', 2, 3300.00, 9900.00, 'Paid',
        DATEADD(MINUTE,540,CAST(DATEADD(DAY,-3,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070037', 5,18, N'高婉庭', '0912345037', 'guest037@example.com',
        DATEADD(DAY,12,@Today), DATEADD(DAY,15,@Today), N'港景雙人房', 2, 3300.00, 9900.00, 'Paid',
        DATEADD(MINUTE,600,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070038', 5,18, N'葉承翰', '0912345038', 'guest038@example.com',
        DATEADD(DAY,15,@Today), DATEADD(DAY,17,@Today), N'港景雙人房', 2, 3300.00, 6600.00, 'Paid',
        DATEADD(MINUTE,660,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070039', 5,18, N'朱雅筑', '0912345039', 'guest039@example.com',
        DATEADD(DAY,12,@Today), DATEADD(DAY,15,@Today), N'港景雙人房', 2, 3300.00, 9900.00, 'Cancelled',
        DATEADD(MINUTE,720,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))),
        'GuestRequest', N'與有效訂單日期重疊，但取消後不得占用房量。', DATEADD(MINUTE,780,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), 'E20260807016'),
    ('BK202608070040', 5,18, N'沈品妤', '0912345040', 'guest040@example.com',
        DATEADD(DAY,-15,@Today), DATEADD(DAY,-12,@Today), N'港景雙人房', 2, 3300.00, 9900.00, 'NoShow',
        DATEADD(MINUTE,585,CAST(DATEADD(DAY,-22,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070041', 3,11, N'顏子軒', '0912345041', 'guest041@example.com',
        DATEADD(DAY,-12,@Today), DATEADD(DAY,-10,@Today), N'舒適三人房', 3, 3900.00, 7800.00, 'Completed',
        DATEADD(MINUTE,630,CAST(DATEADD(DAY,-20,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    ('BK202608070042', 1, 1, N'簡郁雯', '0912345042', 'guest042@example.com',
        DATEADD(DAY,-20,@Today), DATEADD(DAY,-18,@Today), N'經典單人房', 1, 2300.00, 4600.00, 'Completed',
        DATEADD(MINUTE,780,CAST(DATEADD(DAY,-28,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL);

    /* =========================================================
       3. StayRecords：4 筆入住中、5 筆已完成
       ========================================================= */
    SET @IdentityInsertTable = N'dbo.StayRecords';
    SET IDENTITY_INSERT [dbo].[StayRecords] ON;

    INSERT INTO [dbo].[StayRecords]
    (
        [StayRecordId], [BookingNumber], [RoomId], [RoomNumberSnapshot],
        [ActualCheckInAt], [ActualCheckOutAt], [PrimaryGuestName], [ActualGuestCount],
        [CheckedInByEmployeeNumber], [CheckedOutByEmployeeNumber]
    )
    VALUES
    (1, 'BK202608070020',  13, N'301', DATEADD(MINUTE,990,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL, N'周子晴', 2, 'E20260807002', NULL),
    (2, 'BK202608070021',  35, N'601', DATEADD(MINUTE,970,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL, N'彭俊傑', 3, 'E20260807006', NULL),
    (3, 'BK202608070022',  73, N'201', DATEADD(MINUTE,1020,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))), NULL, N'蘇怡靜', 2, 'E20260807007', NULL),
    (4, 'BK202608070023', 104, N'201', DATEADD(MINUTE,1005,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL, N'游承恩', 2, 'E20260807013', NULL),

    (5, 'BK202608070024',  50, N'301', DATEADD(MINUTE,980,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))), DATEADD(MINUTE,660,CAST(DATEADD(DAY,-4,@Today) AS datetime2(0))), N'黃詩涵', 2, 'E20260807004', 'E20260807004'),
    (6, 'BK202608070025', 138, N'301', DATEADD(MINUTE,1010,CAST(DATEADD(DAY,-10,@Today) AS datetime2(0))), DATEADD(MINUTE,640,CAST(DATEADD(DAY,-8,@Today) AS datetime2(0))), N'方映辰', 2, 'E20260807005', 'E20260807005'),
    (7, 'BK202608070029', 176, N'301', DATEADD(MINUTE,995,CAST(DATEADD(DAY,-7,@Today) AS datetime2(0))), DATEADD(MINUTE,650,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))), N'杜佩玲', 2, 'E20260807018', 'E20260807018'),
    (8, 'BK202608070041',  84, N'301', DATEADD(MINUTE,985,CAST(DATEADD(DAY,-12,@Today) AS datetime2(0))), DATEADD(MINUTE,635,CAST(DATEADD(DAY,-10,@Today) AS datetime2(0))), N'顏子軒', 3, 'E20260807011', 'E20260807011'),
    (9, 'BK202608070042',   1, N'201', DATEADD(MINUTE,975,CAST(DATEADD(DAY,-20,@Today) AS datetime2(0))), DATEADD(MINUTE,625,CAST(DATEADD(DAY,-18,@Today) AS datetime2(0))), N'簡郁雯', 1, 'E20260807002', 'E20260807002');

    SET IDENTITY_INSERT [dbo].[StayRecords] OFF;
    SET @IdentityInsertTable = NULL;

    /* =========================================================
       4. OperationLogs：只記錄成功內部操作，不替 NoShow 建員工紀錄
       ========================================================= */
    SET @IdentityInsertTable = N'dbo.OperationLogs';
    SET IDENTITY_INSERT [dbo].[OperationLogs] ON;

    INSERT INTO [dbo].[OperationLogs]
    (
        [OperationLogId], [TargetBranchId], [OperatedAt], [OperatorEmployeeNumber],
        [OperationTypeId], [TargetType], [TargetIdentifier], [Description]
    )
    VALUES
    ( 1,1,DATEADD(MINUTE,540,CAST(DATEADD(DAY,-60,@Today) AS datetime2(0))),'E20260807001', 1,'Branch',N'1',N'建立台北中山商旅。'),
    ( 2,2,DATEADD(MINUTE,555,CAST(DATEADD(DAY,-60,@Today) AS datetime2(0))),'E20260807001', 1,'Branch',N'2',N'建立台北信義商旅。'),
    ( 3,3,DATEADD(MINUTE,570,CAST(DATEADD(DAY,-60,@Today) AS datetime2(0))),'E20260807001', 1,'Branch',N'3',N'建立台中草悟商旅。'),
    ( 4,4,DATEADD(MINUTE,585,CAST(DATEADD(DAY,-60,@Today) AS datetime2(0))),'E20260807001', 1,'Branch',N'4',N'建立台南安平商旅。'),
    ( 5,5,DATEADD(MINUTE,600,CAST(DATEADD(DAY,-60,@Today) AS datetime2(0))),'E20260807001', 1,'Branch',N'5',N'建立高雄港灣商旅。'),
    ( 6,6,DATEADD(MINUTE,615,CAST(DATEADD(DAY,-60,@Today) AS datetime2(0))),'E20260807001', 1,'Branch',N'6',N'建立花蓮站前商旅。'),
    ( 7,6,DATEADD(MINUTE,820,CAST(DATEADD(DAY,-9,@Today) AS datetime2(0))),'E20260807001', 4,'Branch',N'6',N'將花蓮站前商旅設定為停止接受新訂房。'),
    ( 8,1,DATEADD(MINUTE,770,CAST(DATEADD(DAY,-4,@Today) AS datetime2(0))),'E20260807002',18,'Room',N'202',N'將房間 202 設為保留。'),
    ( 9,1,DATEADD(MINUTE,790,CAST(DATEADD(DAY,-4,@Today) AS datetime2(0))),'E20260807002',20,'Room',N'203',N'將房間 203 標記為待清潔。'),
    (10,1,DATEADD(MINUTE,810,CAST(DATEADD(DAY,-4,@Today) AS datetime2(0))),'E20260807001',11,'Room',N'205',N'將房間 205 停用：空調主機異常。'),
    (11,2,DATEADD(MINUTE,830,CAST(DATEADD(DAY,-3,@Today) AS datetime2(0))),'E20260807004',18,'Room',N'201',N'將台北信義館房間 201 設為保留。'),
    (12,5,DATEADD(MINUTE,850,CAST(DATEADD(DAY,-3,@Today) AS datetime2(0))),'E20260807016',20,'Room',N'201',N'將高雄港灣館房間 201 標記為待清潔。'),
    (13,1,DATEADD(MINUTE,900,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))),'E20260807001',15,'Employee',N'E20260807003',N'停用員工帳號 E20260807003。'),
    (14,4,DATEADD(MINUTE,910,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))),'E20260807001',15,'Employee',N'E20260807015',N'停用員工帳號 E20260807015。'),
    (15,6,DATEADD(MINUTE,920,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))),'E20260807001',15,'Employee',N'E20260807008',N'停用員工帳號 E20260807008。'),

    (16,1,DATEADD(MINUTE,630,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))),'E20260807002',21,'Booking',N'BK202608070030',N'因顧客因素取消訂單 BK202608070030。'),
    (17,2,DATEADD(MINUTE,680,CAST(@Today AS datetime2(0))),'E20260807004',21,'Booking',N'BK202608070031',N'因飯店因素取消訂單 BK202608070031。'),
    (18,4,DATEADD(MINUTE,800,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))),'E20260807013',21,'Booking',N'BK202608070032',N'因顧客因素取消訂單 BK202608070032。'),
    (19,5,DATEADD(MINUTE,600,CAST(@Today AS datetime2(0))),'E20260807005',21,'Booking',N'BK202608070033',N'因飯店因素取消訂單 BK202608070033。'),
    (20,5,DATEADD(MINUTE,780,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))),'E20260807016',21,'Booking',N'BK202608070039',N'因顧客因素取消訂單 BK202608070039。'),

    (21,1,DATEADD(MINUTE,990,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))),'E20260807002',22,'Booking',N'BK202608070020',N'完成 Check-in，指派房間 301。'),
    (22,1,DATEADD(MINUTE,970,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))),'E20260807006',22,'Booking',N'BK202608070021',N'完成 Check-in，指派房間 601。'),
    (23,3,DATEADD(MINUTE,1020,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))),'E20260807007',22,'Booking',N'BK202608070022',N'完成 Check-in，指派房間 201。'),
    (24,4,DATEADD(MINUTE,1005,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))),'E20260807013',22,'Booking',N'BK202608070023',N'完成 Check-in，指派房間 201。'),
    (25,2,DATEADD(MINUTE,980,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))),'E20260807004',22,'Booking',N'BK202608070024',N'完成 Check-in，指派房間 301。'),
    (26,2,DATEADD(MINUTE,660,CAST(DATEADD(DAY,-4,@Today) AS datetime2(0))),'E20260807004',23,'Booking',N'BK202608070024',N'完成 Check-out，房間 301 轉為待清潔。'),
    (27,5,DATEADD(MINUTE,1010,CAST(DATEADD(DAY,-10,@Today) AS datetime2(0))),'E20260807005',22,'Booking',N'BK202608070025',N'完成 Check-in，指派房間 301。'),
    (28,5,DATEADD(MINUTE,640,CAST(DATEADD(DAY,-8,@Today) AS datetime2(0))),'E20260807005',23,'Booking',N'BK202608070025',N'完成 Check-out，房間 301 轉為待清潔。'),
    (29,5,DATEADD(MINUTE,810,CAST(DATEADD(DAY,-8,@Today) AS datetime2(0))),'E20260807016',20,'Room',N'301',N'清潔完成，將房間 301 改為已清潔。'),
    (30,6,DATEADD(MINUTE,995,CAST(DATEADD(DAY,-7,@Today) AS datetime2(0))),'E20260807018',22,'Booking',N'BK202608070029',N'完成 Check-in，指派房間 301。'),
    (31,6,DATEADD(MINUTE,650,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))),'E20260807018',23,'Booking',N'BK202608070029',N'完成 Check-out，房間 301 轉為待清潔。'),
    (32,6,DATEADD(MINUTE,830,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))),'E20260807019',20,'Room',N'301',N'清潔完成，將房間 301 改為已清潔。'),
    (33,3,DATEADD(MINUTE,985,CAST(DATEADD(DAY,-12,@Today) AS datetime2(0))),'E20260807011',22,'Booking',N'BK202608070041',N'完成 Check-in，指派房間 301。'),
    (34,3,DATEADD(MINUTE,635,CAST(DATEADD(DAY,-10,@Today) AS datetime2(0))),'E20260807011',23,'Booking',N'BK202608070041',N'完成 Check-out，房間 301 轉為待清潔。'),
    (35,1,DATEADD(MINUTE,975,CAST(DATEADD(DAY,-20,@Today) AS datetime2(0))),'E20260807002',22,'Booking',N'BK202608070042',N'完成 Check-in，指派房間 201。'),
    (36,1,DATEADD(MINUTE,625,CAST(DATEADD(DAY,-18,@Today) AS datetime2(0))),'E20260807002',23,'Booking',N'BK202608070042',N'完成 Check-out，房間 201 轉為待清潔。'),
    (37,1,DATEADD(MINUTE,800,CAST(DATEADD(DAY,-18,@Today) AS datetime2(0))),'E20260807006',20,'Room',N'201',N'清潔完成，將房間 201 改為已清潔。');

    SET IDENTITY_INSERT [dbo].[OperationLogs] OFF;
    SET @IdentityInsertTable = NULL;

    DBCC CHECKIDENT ('dbo.StayRecords',   RESEED, 9)  WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.OperationLogs', RESEED, 37) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    SELECT N'開發情境完成' AS [Result],
           @NowTaipei AS [TaipeiNow],
           (SELECT COUNT(*) FROM [dbo].[Bookings]) AS [Bookings],
           (SELECT COUNT(*) FROM [dbo].[StayRecords]) AS [StayRecords],
           (SELECT COUNT(*) FROM [dbo].[OperationLogs]) AS [OperationLogs];
END TRY
BEGIN CATCH
    IF @IdentityInsertTable = N'dbo.StayRecords'
        SET IDENTITY_INSERT [dbo].[StayRecords] OFF;
    ELSE IF @IdentityInsertTable = N'dbo.OperationLogs'
        SET IDENTITY_INSERT [dbo].[OperationLogs] OFF;

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
