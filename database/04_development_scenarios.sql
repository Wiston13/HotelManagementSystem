/*
    HotelManagementSystem - 開發情境資料
    SQL Server

    執行順序：
    1. 01_create_hotel_management_schema.sql
    2. 02_required_seed.sql
    3. 03_demo_data.sql
    4. 04_development_scenarios.sql（本檔）

    本檔責任：
    - 重設情境用房間供應／清潔狀態
    - 訂單、住房紀錄、取消、No-show、操作紀錄
    - 所有相對日期以執行當下的台灣日期為基準

    可重跑方式：
    - 可在 03_demo_data.sql 之後重複單獨執行本檔。
    - 本檔只清除情境表，並把 03_demo_data.sql 建立的 188 間房重設為固定情境；
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
       OR (SELECT COUNT(*) FROM [dbo].[Employees]) <> 19
       OR (SELECT COUNT(*) FROM [dbo].[OperationTypes]) <> 25
    BEGIN
        THROW 50002, N'展示基準資料不完整，請依序執行 01_create_hotel_management_schema.sql、02_required_seed.sql、03_demo_data.sql。', 1;
    END;

    DELETE FROM [dbo].[OperationLogs];
    DELETE FROM [dbo].[StayRecords];
    DELETE FROM [dbo].[Bookings];

    DECLARE @NowTaipei datetime2(0) =
        CONVERT(datetime2(0), SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time');
    DECLARE @Today date = CAST(@NowTaipei AS date);
    DECLARE @CapacityRiskDate date = DATEADD(DAY,55,@Today);
    /*
       此情境刻意安排在入住日 16:05，驗證已進入合法 Check-in 時段後，
       只要訂單仍為 Paid、尚無 StayRecord，且尚未到原退房日 12:00，
       飯店因素取消仍可成立。
       若本檔在 16:05 前執行，改用昨天，避免建立未來時間的 CancelledAt。
    */
    DECLARE @SameDayHotelCancellationDate date =
        CASE
            WHEN @NowTaipei >= DATEADD(MINUTE,965,CAST(@Today AS datetime2(0))) THEN @Today
            ELSE DATEADD(DAY,-1,@Today)
        END;
    DECLARE @SameDayHotelCancelledAt datetime2(0) =
        DATEADD(MINUTE,965,CAST(@SameDayHotelCancellationDate AS datetime2(0)));
    DECLARE @HualienBookingStoppedAt datetime2(0) =
        DATEADD(MINUTE,820,CAST(DATEADD(DAY,-9,@Today) AS datetime2(0)));
    DECLARE @PanoramicTripleDisabledAt datetime2(0) =
        DATEADD(MINUTE,720,CAST(DATEADD(DAY,-8,@Today) AS datetime2(0)));

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
       2. Bookings：42 筆既有測試矩陣 + 4 筆容量臨界資料

       001～019：一般 Paid（合法入住、今日 16:00、未來、跨館、匯出）
       020～023：CheckedIn（一般、今日退房、逾期未退、提早退房）
       024、025、029、041、042：Completed
       026、034、035、040：歷史 NoShow
       027、028：仍為 Paid 的 NoShowService 候選
       030～033、039：兩種取消因素
       036～038：重疊與相鄰房量
       043～046：容量臨界／供應異動風險（搭配既有 018）
       ========================================================= */
    DECLARE @BookingScenarios table
    (
        [ScenarioId] int NOT NULL PRIMARY KEY,
        [BookingNumber] varchar(20) NULL,
        [BranchId] int NOT NULL,
        [RoomTypeId] int NOT NULL,
        [BookerName] nvarchar(50) NOT NULL,
        [ContactPhone] varchar(20) NOT NULL,
        [Email] varchar(254) NOT NULL,
        [CheckInDate] date NOT NULL,
        [CheckOutDate] date NOT NULL,
        [RoomTypeNameSnapshot] nvarchar(50) NOT NULL,
        [MaxOccupancySnapshot] tinyint NOT NULL,
        [NightlyPriceSnapshot] decimal(10,2) NOT NULL,
        [TotalAmount] decimal(12,2) NOT NULL,
        [BookingStatus] varchar(20) NOT NULL,
        [CreatedAt] datetime2(0) NOT NULL,
        [CancellationCause] varchar(30) NULL,
        [CancellationReason] nvarchar(500) NULL,
        [CancelledAt] datetime2(0) NULL,
        [CancelledByEmployeeNumber] varchar(20) NULL
    );

    INSERT INTO @BookingScenarios
    (
        [ScenarioId], [BranchId], [RoomTypeId], [BookerName], [ContactPhone], [Email],
        [CheckInDate], [CheckOutDate], [RoomTypeNameSnapshot], [MaxOccupancySnapshot],
        [NightlyPriceSnapshot], [TotalAmount], [BookingStatus], [CreatedAt],
        [CancellationCause], [CancellationReason], [CancelledAt], [CancelledByEmployeeNumber]
    )
    VALUES
    (1, 1, 2, N'陳冠宇', '0912345001', 'guest001@example.com',
        DATEADD(DAY,-1,@Today), DATEADD(DAY, 1,@Today), N'標準雙人房', 2, 3200.00,  6400.00, 'Paid',
        DATEADD(MINUTE,555,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (2, 1, 2, N'李佳穎', '0912345002', 'guest002@example.com',
        @Today, DATEADD(DAY,2,@Today), N'標準雙人房', 2, 3200.00, 6400.00, 'Paid',
        DATEADD(MINUTE,610,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (3, 1, 3, N'周柏勳', '0912345003', 'guest003@example.com',
        DATEADD(DAY,5,@Today), DATEADD(DAY,7,@Today), N'豪華雙床房', 2, 3800.00, 7600.00, 'Paid',
        DATEADD(MINUTE,680,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (4, 1, 5, N'林書妍', '0912345004', 'guest004@example.com',
        DATEADD(DAY,-1,@Today), DATEADD(DAY,1,@Today), N'行政三人房', 3, 4500.00, 9000.00, 'Paid',
        DATEADD(MINUTE,725,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (5, 1, 4, N'吳家豪', '0912345005', 'guest005@example.com',
        DATEADD(DAY,-1,@Today), DATEADD(DAY,2,@Today), N'家庭四人房', 4, 5200.00, 15600.00, 'Paid',
        DATEADD(MINUTE,845,CAST(DATEADD(DAY,-7,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (6, 2, 7, N'許博翔', '0912345006', 'guest006@example.com',
        DATEADD(DAY,3,@Today), DATEADD(DAY,5,@Today), N'景觀雙人房', 2, 4200.00, 8400.00, 'Paid',
        DATEADD(MINUTE,540,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (7, 3,10, N'黃筱涵', '0912345007', 'guest007@example.com',
        DATEADD(DAY,10,@Today), DATEADD(DAY,12,@Today), N'標準雙人房', 2, 2900.00, 5800.00, 'Paid',
        DATEADD(MINUTE,600,CAST(DATEADD(DAY,-3,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (8, 4,14, N'楊宗翰', '0912345008', 'guest008@example.com',
        DATEADD(DAY,1,@Today), DATEADD(DAY,4,@Today), N'古都雙人房', 2, 2700.00, 8100.00, 'Paid',
        DATEADD(MINUTE,735,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (9, 5,18, N'謝宜庭', '0912345009', 'guest009@example.com',
        DATEADD(DAY,6,@Today), DATEADD(DAY,9,@Today), N'港景雙人房', 2, 3300.00, 9900.00, 'Paid',
        DATEADD(MINUTE,810,CAST(DATEADD(DAY,-4,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (10, 6,22, N'張凱翔', '0912345010', 'guest010@example.com',
        DATEADD(DAY,4,@Today), DATEADD(DAY,6,@Today), N'山海雙人房', 2, 2600.00, 5200.00, 'Paid',
        DATEADD(MINUTE,795,CAST(DATEADD(DAY,-10,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (11, 2, 9, N'劉思妤', '0912345011', 'guest011@example.com',
        DATEADD(DAY,14,@Today), DATEADD(DAY,18,@Today), N'家庭套房', 4, 6500.00, 26000.00, 'Paid',
        DATEADD(MINUTE,565,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (12, 3,12, N'王柏鈞', '0912345012', 'guest012@example.com',
        DATEADD(DAY,2,@Today), DATEADD(DAY,5,@Today), N'家庭四人房', 4, 4800.00, 14400.00, 'Paid',
        DATEADD(MINUTE,705,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (13, 4,15, N'陳怡安', '0912345013', 'guest013@example.com',
        DATEADD(DAY,8,@Today), DATEADD(DAY,9,@Today), N'庭院雙床房', 2, 3400.00, 3400.00, 'Paid',
        DATEADD(MINUTE,845,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (14, 5,19, N'郭家宏', '0912345014', 'guest014@example.com',
        DATEADD(DAY,20,@Today), DATEADD(DAY,23,@Today), N'豪華雙床房', 2, 3900.00, 11700.00, 'Paid',
        DATEADD(MINUTE,930,CAST(DATEADD(DAY,-3,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (15, 6,24, N'蔡依婷', '0912345015', 'guest015@example.com',
        DATEADD(DAY,7,@Today), DATEADD(DAY,10,@Today), N'家庭四人房', 4, 4400.00, 13200.00, 'Paid',
        DATEADD(MINUTE,670,CAST(DATEADD(DAY,-9,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (16, 1, 2, N'趙建國', '0912345016', 'guest016@example.com',
        DATEADD(DAY,30,@Today), DATEADD(DAY,32,@Today), N'標準雙人房', 2, 3200.00, 6400.00, 'Paid',
        DATEADD(MINUTE,580,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (17, 2, 6, N'洪雅雯', '0912345017', 'guest017@example.com',
        DATEADD(DAY,45,@Today), DATEADD(DAY,46,@Today), N'都會單人房', 1, 2600.00, 2600.00, 'Paid',
        DATEADD(MINUTE,640,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (18, 3,13, N'鄭文傑', '0912345018', 'guest018@example.com',
        DATEADD(DAY,55,@Today), DATEADD(DAY,58,@Today), N'和風雙人房', 2, 3500.00, 10500.00, 'Paid',
        DATEADD(MINUTE,780,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (19, 5,17, N'曾郁婷', '0912345019', 'guest019@example.com',
        DATEADD(DAY,1,@Today), DATEADD(DAY,2,@Today), N'商務單人房', 1, 2400.00, 2400.00, 'Paid',
        DATEADD(MINUTE,860,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),

    (20, 1, 2, N'周子晴', '0912345020', 'guest020@example.com',
        DATEADD(DAY,-1,@Today), DATEADD(DAY,1,@Today), N'標準雙人房', 2, 3200.00, 6400.00, 'CheckedIn',
        DATEADD(MINUTE,520,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (21, 1, 5, N'彭俊傑', '0912345021', 'guest021@example.com',
        DATEADD(DAY,-2,@Today), @Today, N'行政三人房', 3, 4500.00, 9000.00, 'CheckedIn',
        DATEADD(MINUTE,590,CAST(DATEADD(DAY,-8,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (22, 3,10, N'蘇怡靜', '0912345022', 'guest022@example.com',
        DATEADD(DAY,-5,@Today), DATEADD(DAY,-2,@Today), N'標準雙人房', 2, 2900.00, 8700.00, 'CheckedIn',
        DATEADD(MINUTE,620,CAST(DATEADD(DAY,-12,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (23, 4,14, N'游承恩', '0912345023', 'guest023@example.com',
        DATEADD(DAY,-1,@Today), DATEADD(DAY,4,@Today), N'古都雙人房', 2, 2700.00, 13500.00, 'CheckedIn',
        DATEADD(MINUTE,710,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),

    (24, 2, 7, N'黃詩涵', '0912345024', 'guest024@example.com',
        DATEADD(DAY,-6,@Today), DATEADD(DAY,-4,@Today), N'景觀雙人房', 2, 4200.00, 8400.00, 'Completed',
        DATEADD(MINUTE,860,CAST(DATEADD(DAY,-14,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (25, 5,18, N'方映辰', '0912345025', 'guest025@example.com',
        DATEADD(DAY,-10,@Today), DATEADD(DAY,-8,@Today), N'港景雙人房', 2, 3300.00, 6600.00, 'Completed',
        DATEADD(MINUTE,575,CAST(DATEADD(DAY,-18,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (26, 1, 1, N'許家寧', '0912345026', 'guest026@example.com',
        DATEADD(DAY,-5,@Today), DATEADD(DAY,-3,@Today), N'經典單人房', 1, 2300.00, 4600.00, 'NoShow',
        DATEADD(MINUTE,530,CAST(DATEADD(DAY,-11,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (27, 2, 6, N'何志明', '0912345027', 'guest027@example.com',
        DATEADD(DAY,-4,@Today), DATEADD(DAY,-1,@Today), N'都會單人房', 1, 2600.00, 7800.00, 'Paid',
        DATEADD(MINUTE,545,CAST(DATEADD(DAY,-10,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (28, 3,13, N'徐安琪', '0912345028', 'guest028@example.com',
        DATEADD(DAY,-1,@Today), @Today, N'和風雙人房', 2, 3500.00, 3500.00, 'Paid',
        DATEADD(MINUTE,625,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (29, 6,23, N'杜佩玲', '0912345029', 'guest029@example.com',
        DATEADD(DAY,-7,@Today), DATEADD(DAY,-5,@Today), N'山景雙床房', 2, 3200.00, 6400.00, 'Completed',
        DATEADD(MINUTE,700,CAST(DATEADD(DAY,-15,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),

    (30, 1, 4, N'鍾雅婷', '0912345030', 'guest030@example.com',
        DATEADD(DAY,10,@Today), DATEADD(DAY,12,@Today), N'家庭四人房', 4, 5200.00, 10400.00, 'Cancelled',
        DATEADD(MINUTE,570,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))),
        'GuestRequest', N'顧客行程變更，於入住日前提出取消。', DATEADD(MINUTE,630,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), 'E20260807002'),
    (31, 2, 8, N'羅淑芬', '0912345031', 'guest031@example.com',
        DATEADD(DAY,2,@Today), DATEADD(DAY,4,@Today), N'豪華雙床房', 2, 4600.00, 9200.00, 'Cancelled',
        DATEADD(MINUTE,660,CAST(DATEADD(DAY,-7,@Today) AS datetime2(0))),
        'HotelUnableToFulfill', N'原房型電力設備檢修，分館已確認無法依原訂單履約。', DATEADD(MINUTE,680,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), 'E20260807004'),
    (32, 4,16, N'廖信宏', '0912345032', 'guest032@example.com',
        DATEADD(DAY,12,@Today), DATEADD(DAY,14,@Today), N'家庭四人房', 4, 4500.00, 9000.00, 'Cancelled',
        DATEADD(MINUTE,750,CAST(DATEADD(DAY,-4,@Today) AS datetime2(0))),
        'GuestRequest', N'顧客家庭活動取消，於期限內完成核對。', DATEADD(MINUTE,800,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), 'E20260807013'),
    (33, 5,20, N'江美華', '0912345033', 'guest033@example.com',
        @SameDayHotelCancellationDate, DATEADD(DAY,2,@SameDayHotelCancellationDate), N'家庭四人房', 4, 5000.00, 10000.00, 'Cancelled',
        DATEADD(MINUTE,480,CAST(DATEADD(DAY,-9,@SameDayHotelCancellationDate) AS datetime2(0))),
        'HotelUnableToFulfill', N'合法入住時原房型全部房間經確認均無法提供。', @SameDayHotelCancelledAt, 'E20260807005'),
    (34, 4,14, N'邱冠宇', '0912345034', 'guest034@example.com',
        DATEADD(DAY,-8,@Today), DATEADD(DAY,-7,@Today), N'古都雙人房', 2, 2700.00, 2700.00, 'NoShow',
        DATEADD(MINUTE,515,CAST(DATEADD(DAY,-14,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (35, 6,22, N'謝佩如', '0912345035', 'guest035@example.com',
        DATEADD(DAY,-4,@Today), DATEADD(DAY,-2,@Today), N'山海雙人房', 2, 2600.00, 5200.00, 'NoShow',
        DATEADD(MINUTE,625,CAST(DATEADD(DAY,-12,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),

    (36, 5,18, N'潘宥辰', '0912345036', 'guest036@example.com',
        DATEADD(DAY,10,@Today), DATEADD(DAY,13,@Today), N'港景雙人房', 2, 3300.00, 9900.00, 'Paid',
        DATEADD(MINUTE,540,CAST(DATEADD(DAY,-3,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (37, 5,18, N'高婉庭', '0912345037', 'guest037@example.com',
        DATEADD(DAY,12,@Today), DATEADD(DAY,15,@Today), N'港景雙人房', 2, 3300.00, 9900.00, 'Paid',
        DATEADD(MINUTE,600,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (38, 5,18, N'葉承翰', '0912345038', 'guest038@example.com',
        DATEADD(DAY,15,@Today), DATEADD(DAY,17,@Today), N'港景雙人房', 2, 3300.00, 6600.00, 'Paid',
        DATEADD(MINUTE,660,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (39, 5,18, N'朱雅筑', '0912345039', 'guest039@example.com',
        DATEADD(DAY,12,@Today), DATEADD(DAY,15,@Today), N'港景雙人房', 2, 3300.00, 9900.00, 'Cancelled',
        DATEADD(MINUTE,720,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))),
        'GuestRequest', N'與有效訂單日期重疊，但取消後不得占用房量。', DATEADD(MINUTE,780,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), 'E20260807016'),
    (40, 5,18, N'沈品妤', '0912345040', 'guest040@example.com',
        DATEADD(DAY,-15,@Today), DATEADD(DAY,-12,@Today), N'港景雙人房', 2, 3300.00, 9900.00, 'NoShow',
        DATEADD(MINUTE,585,CAST(DATEADD(DAY,-22,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (41, 3,11, N'顏子軒', '0912345041', 'guest041@example.com',
        DATEADD(DAY,-12,@Today), DATEADD(DAY,-10,@Today), N'舒適三人房', 3, 3900.00, 7800.00, 'Completed',
        DATEADD(MINUTE,630,CAST(DATEADD(DAY,-20,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (42, 1, 1, N'簡郁雯', '0912345042', 'guest042@example.com',
        DATEADD(DAY,-20,@Today), DATEADD(DAY,-18,@Today), N'經典單人房', 1, 2300.00, 4600.00, 'Completed',
        DATEADD(MINUTE,780,CAST(DATEADD(DAY,-28,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),

    /*
       容量臨界／供應異動風險：台中草悟館和風雙人房共有 5 間 Open 房。
       既有 018 加上 043～046，在未來第 55～57 天形成 5 筆有效 Paid 需求。
       房間 501（RoomId 99）可用於 Open → Reserved、Open → Disabled，
       或由管理員變更 RoomType；前三者會分別觸發確認或硬性阻擋流程。
    */
    (43, 3,13, N'林育安', '0912345043', 'guest043@example.com',
        @CapacityRiskDate, DATEADD(DAY,3,@CapacityRiskDate), N'和風雙人房', 2, 3500.00, 10500.00, 'Paid',
        DATEADD(MINUTE,540,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (44, 3,13, N'張庭瑜', '0912345044', 'guest044@example.com',
        @CapacityRiskDate, DATEADD(DAY,3,@CapacityRiskDate), N'和風雙人房', 2, 3500.00, 10500.00, 'Paid',
        DATEADD(MINUTE,600,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (45, 3,13, N'吳承恩', '0912345045', 'guest045@example.com',
        @CapacityRiskDate, DATEADD(DAY,3,@CapacityRiskDate), N'和風雙人房', 2, 3500.00, 10500.00, 'Paid',
        DATEADD(MINUTE,660,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL),
    (46, 3,13, N'陳品妍', '0912345046', 'guest046@example.com',
        @CapacityRiskDate, DATEADD(DAY,3,@CapacityRiskDate), N'和風雙人房', 2, 3500.00, 10500.00, 'Paid',
        DATEADD(MINUTE,720,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL,NULL,NULL,NULL);

    /*
       訂單編號沿用 BookingController.Success 的正式格式：
       BK + CreatedAt 的 yyMMdd + RoomTypeId(D4) + 同日同房型流水號(D4)。
       ScenarioId 001～046 只作為測試案例識別與穩定排序，不寫入正式訂單編號。
    */
    ;WITH [SequencedScenarios] AS
    (
        SELECT
            [ScenarioId],
            ROW_NUMBER() OVER
            (
                PARTITION BY CAST([CreatedAt] AS date), [RoomTypeId]
                ORDER BY [ScenarioId]
            ) AS [SequenceNumber]
        FROM @BookingScenarios
    )
    UPDATE S
    SET [BookingNumber] =
        'BK'
        + CONVERT(char(6), CAST(S.[CreatedAt] AS date), 12)
        + RIGHT('0000' + CONVERT(varchar(10), S.[RoomTypeId]), 4)
        + RIGHT('0000' + CONVERT(varchar(10), Q.[SequenceNumber]), 4)
    FROM @BookingScenarios AS S
    INNER JOIN [SequencedScenarios] AS Q ON Q.[ScenarioId] = S.[ScenarioId];

    IF (SELECT COUNT(*) FROM @BookingScenarios) <> 46
       OR EXISTS
       (
           SELECT 1
           FROM @BookingScenarios
           WHERE [BookingNumber] IS NULL
              OR [CheckInDate] < CAST([CreatedAt] AS date)
              OR [CheckOutDate] <= [CheckInDate]
              OR [CheckOutDate] > DATEADD(DAY,60,CAST([CreatedAt] AS date))
       )
    BEGIN
        THROW 50004, N'訂單情境不符合成立當時的日期範圍或訂單編號生成前置條件。', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @BookingScenarios AS S
        INNER JOIN [dbo].[RoomTypes] AS RT ON RT.[RoomTypeId] = S.[RoomTypeId]
        INNER JOIN [dbo].[Branches] AS B ON B.[BranchId] = S.[BranchId]
        WHERE RT.[BranchId] <> S.[BranchId]
           OR RT.[RoomTypeName] <> S.[RoomTypeNameSnapshot]
           OR RT.[MaxOccupancy] <> S.[MaxOccupancySnapshot]
           OR RT.[NightlyPrice] <> S.[NightlyPriceSnapshot]
           OR S.[TotalAmount] <> S.[NightlyPriceSnapshot] * DATEDIFF(DAY,S.[CheckInDate],S.[CheckOutDate])
           OR (RT.[IsActive] = 0 AND NOT (S.[RoomTypeId] = 21 AND S.[CreatedAt] < @PanoramicTripleDisabledAt))
           OR (B.[AcceptsNewBookings] = 0 AND NOT (S.[BranchId] = 6 AND S.[CreatedAt] < @HualienBookingStoppedAt))
    )
    BEGIN
        THROW 50005, N'訂單情境的分館、房型、快照、金額或成立當時狀態不一致。', 1;
    END;

    INSERT INTO [dbo].[Bookings]
    (
        [BookingNumber], [BranchId], [RoomTypeId], [BookerName], [ContactPhone], [Email],
        [CheckInDate], [CheckOutDate], [RoomTypeNameSnapshot], [MaxOccupancySnapshot],
        [NightlyPriceSnapshot], [TotalAmount], [BookingStatus], [CreatedAt],
        [CancellationCause], [CancellationReason], [CancelledAt], [CancelledByEmployeeNumber]
    )
    SELECT
        [BookingNumber], [BranchId], [RoomTypeId], [BookerName], [ContactPhone], [Email],
        [CheckInDate], [CheckOutDate], [RoomTypeNameSnapshot], [MaxOccupancySnapshot],
        [NightlyPriceSnapshot], [TotalAmount], [BookingStatus], [CreatedAt],
        [CancellationCause], [CancellationReason], [CancelledAt], [CancelledByEmployeeNumber]
    FROM @BookingScenarios;

    DECLARE @Booking020 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 20);
    DECLARE @Booking021 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 21);
    DECLARE @Booking022 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 22);
    DECLARE @Booking023 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 23);
    DECLARE @Booking024 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 24);
    DECLARE @Booking025 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 25);
    DECLARE @Booking029 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 29);
    DECLARE @Booking030 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 30);
    DECLARE @Booking031 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 31);
    DECLARE @Booking032 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 32);
    DECLARE @Booking033 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 33);
    DECLARE @Booking039 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 39);
    DECLARE @Booking041 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 41);
    DECLARE @Booking042 varchar(20) = (SELECT [BookingNumber] FROM @BookingScenarios WHERE [ScenarioId] = 42);

    IF (SELECT COUNT(*) FROM [dbo].[Rooms]
        WHERE [BranchId] = 3 AND [RoomTypeId] = 13 AND [SupplyStatus] = 'Open') <> 5
       OR (SELECT COUNT(*) FROM [dbo].[Bookings]
           WHERE [BranchId] = 3
             AND [RoomTypeId] = 13
             AND [BookingStatus] IN ('Paid','CheckedIn')
             AND [CheckInDate] <= @CapacityRiskDate
             AND [CheckOutDate] > @CapacityRiskDate) <> 5
    BEGIN
        THROW 50003, N'容量臨界情境不完整：台中草悟館和風雙人房必須為 5 間 Open 對 5 筆有效需求。', 1;
    END;

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
    (1, @Booking020,  13, N'301', DATEADD(MINUTE,990,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL, N'周子晴', 2, 'E20260807002', NULL),
    (2, @Booking021,  35, N'601', DATEADD(MINUTE,970,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))), NULL, N'彭俊傑', 3, 'E20260807006', NULL),
    (3, @Booking022,  73, N'201', DATEADD(MINUTE,1020,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))), NULL, N'蘇怡靜', 2, 'E20260807007', NULL),
    (4, @Booking023, 104, N'201', DATEADD(MINUTE,1005,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))), NULL, N'游承恩', 2, 'E20260807013', NULL),

    (5, @Booking024,  50, N'301', DATEADD(MINUTE,980,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))), DATEADD(MINUTE,660,CAST(DATEADD(DAY,-4,@Today) AS datetime2(0))), N'黃詩涵', 2, 'E20260807004', 'E20260807004'),
    (6, @Booking025, 138, N'301', DATEADD(MINUTE,1010,CAST(DATEADD(DAY,-10,@Today) AS datetime2(0))), DATEADD(MINUTE,640,CAST(DATEADD(DAY,-8,@Today) AS datetime2(0))), N'方映辰', 2, 'E20260807005', 'E20260807005'),
    (7, @Booking029, 176, N'301', DATEADD(MINUTE,995,CAST(DATEADD(DAY,-7,@Today) AS datetime2(0))), DATEADD(MINUTE,650,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))), N'杜佩玲', 2, 'E20260807018', 'E20260807018'),
    (8, @Booking041,  84, N'301', DATEADD(MINUTE,985,CAST(DATEADD(DAY,-12,@Today) AS datetime2(0))), DATEADD(MINUTE,635,CAST(DATEADD(DAY,-10,@Today) AS datetime2(0))), N'顏子軒', 3, 'E20260807011', 'E20260807011'),
    (9, @Booking042,   1, N'201', DATEADD(MINUTE,975,CAST(DATEADD(DAY,-20,@Today) AS datetime2(0))), DATEADD(MINUTE,625,CAST(DATEADD(DAY,-18,@Today) AS datetime2(0))), N'簡郁雯', 1, 'E20260807002', 'E20260807002');

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
    ( 1,1,DATEADD(MINUTE,540,CAST(DATEADD(DAY,-60,@Today) AS datetime2(0))),'E20260807001', 1,'Branch',N'台北中山商旅',N'建立分館：台北中山商旅。'),
    ( 2,2,DATEADD(MINUTE,555,CAST(DATEADD(DAY,-60,@Today) AS datetime2(0))),'E20260807001', 1,'Branch',N'台北信義商旅',N'建立分館：台北信義商旅。'),
    ( 3,3,DATEADD(MINUTE,570,CAST(DATEADD(DAY,-60,@Today) AS datetime2(0))),'E20260807001', 1,'Branch',N'台中草悟商旅',N'建立分館：台中草悟商旅。'),
    ( 4,4,DATEADD(MINUTE,585,CAST(DATEADD(DAY,-60,@Today) AS datetime2(0))),'E20260807001', 1,'Branch',N'台南安平商旅',N'建立分館：台南安平商旅。'),
    ( 5,5,DATEADD(MINUTE,600,CAST(DATEADD(DAY,-60,@Today) AS datetime2(0))),'E20260807001', 1,'Branch',N'高雄港灣商旅',N'建立分館：高雄港灣商旅。'),
    ( 6,6,DATEADD(MINUTE,615,CAST(DATEADD(DAY,-60,@Today) AS datetime2(0))),'E20260807001', 1,'Branch',N'花蓮站前商旅',N'建立分館：花蓮站前商旅。'),
    ( 7,6,@HualienBookingStoppedAt,'E20260807001', 4,'Branch',N'花蓮站前商旅',N'將分館 花蓮站前商旅 設定為停止接受新訂房。'),
    ( 8,1,DATEADD(MINUTE,770,CAST(DATEADD(DAY,-4,@Today) AS datetime2(0))),'E20260807002',18,'Room',N'202',N'將房間 202 供應狀態更新為 Reserved。'),
    ( 9,1,DATEADD(MINUTE,790,CAST(DATEADD(DAY,-4,@Today) AS datetime2(0))),'E20260807002',20,'Room',N'203',N'將房間 203 標記為 NeedsCleaning。'),
    (10,1,DATEADD(MINUTE,810,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))),'E20260807002',11,'Room',N'205',N'將房間 205 停用，原因：空調異音，等待初步檢查。'),
    (11,2,DATEADD(MINUTE,830,CAST(DATEADD(DAY,-3,@Today) AS datetime2(0))),'E20260807004',18,'Room',N'201',N'將房間 201 供應狀態更新為 Reserved。'),
    (12,5,DATEADD(MINUTE,850,CAST(DATEADD(DAY,-3,@Today) AS datetime2(0))),'E20260807016',20,'Room',N'201',N'將房間 201 標記為 NeedsCleaning。'),
    (13,1,DATEADD(MINUTE,900,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))),'E20260807001',15,'Employee',N'E20260807003',N'停用員工帳號：E20260807003(陳柏宇)。'),
    (14,4,DATEADD(MINUTE,910,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))),'E20260807001',15,'Employee',N'E20260807015',N'停用員工帳號：E20260807015(鄭雅文)。'),
    (15,6,DATEADD(MINUTE,920,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))),'E20260807001',15,'Employee',N'E20260807008',N'停用員工帳號：E20260807008(劉思妤)。'),

    (16,1,DATEADD(MINUTE,630,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))),'E20260807002',21,'Booking',@Booking030,N'因顧客因素取消訂單 ' + @Booking030 + N'。'),
    (17,2,DATEADD(MINUTE,680,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))),'E20260807004',21,'Booking',@Booking031,N'因飯店因素取消訂單 ' + @Booking031 + N'。'),
    (18,4,DATEADD(MINUTE,800,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))),'E20260807013',21,'Booking',@Booking032,N'因顧客因素取消訂單 ' + @Booking032 + N'。'),
    (19,5,@SameDayHotelCancelledAt,'E20260807005',21,'Booking',@Booking033,N'因飯店因素取消訂單 ' + @Booking033 + N'。'),
    (20,5,DATEADD(MINUTE,780,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))),'E20260807016',21,'Booking',@Booking039,N'因顧客因素取消訂單 ' + @Booking039 + N'。'),

    (21,1,DATEADD(MINUTE,990,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))),'E20260807002',22,'Booking',@Booking020,N'完成訂單 ' + @Booking020 + N' 的 Check-in，指派房間 301。'),
    (22,1,DATEADD(MINUTE,970,CAST(DATEADD(DAY,-2,@Today) AS datetime2(0))),'E20260807006',22,'Booking',@Booking021,N'完成訂單 ' + @Booking021 + N' 的 Check-in，指派房間 601。'),
    (23,3,DATEADD(MINUTE,1020,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))),'E20260807007',22,'Booking',@Booking022,N'完成訂單 ' + @Booking022 + N' 的 Check-in，指派房間 201。'),
    (24,4,DATEADD(MINUTE,1005,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))),'E20260807013',22,'Booking',@Booking023,N'完成訂單 ' + @Booking023 + N' 的 Check-in，指派房間 201。'),
    (25,2,DATEADD(MINUTE,980,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))),'E20260807004',22,'Booking',@Booking024,N'完成訂單 ' + @Booking024 + N' 的 Check-in，指派房間 301。'),
    (26,2,DATEADD(MINUTE,660,CAST(DATEADD(DAY,-4,@Today) AS datetime2(0))),'E20260807004',23,'Booking',@Booking024,N'完成訂單 ' + @Booking024 + N' 的 Check-Out，房間 301 已轉為待清潔。'),
    (27,5,DATEADD(MINUTE,1010,CAST(DATEADD(DAY,-10,@Today) AS datetime2(0))),'E20260807005',22,'Booking',@Booking025,N'完成訂單 ' + @Booking025 + N' 的 Check-in，指派房間 301。'),
    (28,5,DATEADD(MINUTE,640,CAST(DATEADD(DAY,-8,@Today) AS datetime2(0))),'E20260807005',23,'Booking',@Booking025,N'完成訂單 ' + @Booking025 + N' 的 Check-Out，房間 301 已轉為待清潔。'),
    (29,5,DATEADD(MINUTE,810,CAST(DATEADD(DAY,-8,@Today) AS datetime2(0))),'E20260807016',20,'Room',N'301',N'將房間 301 標記為 Clean。'),
    (30,6,DATEADD(MINUTE,995,CAST(DATEADD(DAY,-7,@Today) AS datetime2(0))),'E20260807018',22,'Booking',@Booking029,N'完成訂單 ' + @Booking029 + N' 的 Check-in，指派房間 301。'),
    (31,6,DATEADD(MINUTE,650,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))),'E20260807018',23,'Booking',@Booking029,N'完成訂單 ' + @Booking029 + N' 的 Check-Out，房間 301 已轉為待清潔。'),
    (32,6,DATEADD(MINUTE,830,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))),'E20260807019',20,'Room',N'301',N'將房間 301 標記為 Clean。'),
    (33,3,DATEADD(MINUTE,985,CAST(DATEADD(DAY,-12,@Today) AS datetime2(0))),'E20260807011',22,'Booking',@Booking041,N'完成訂單 ' + @Booking041 + N' 的 Check-in，指派房間 301。'),
    (34,3,DATEADD(MINUTE,635,CAST(DATEADD(DAY,-10,@Today) AS datetime2(0))),'E20260807011',23,'Booking',@Booking041,N'完成訂單 ' + @Booking041 + N' 的 Check-Out，房間 301 已轉為待清潔。'),
    (35,1,DATEADD(MINUTE,975,CAST(DATEADD(DAY,-20,@Today) AS datetime2(0))),'E20260807002',22,'Booking',@Booking042,N'完成訂單 ' + @Booking042 + N' 的 Check-in，指派房間 201。'),
    (36,1,DATEADD(MINUTE,625,CAST(DATEADD(DAY,-18,@Today) AS datetime2(0))),'E20260807002',23,'Booking',@Booking042,N'完成訂單 ' + @Booking042 + N' 的 Check-Out，房間 201 已轉為待清潔。'),
    (37,1,DATEADD(MINUTE,800,CAST(DATEADD(DAY,-18,@Today) AS datetime2(0))),'E20260807006',20,'Room',N'201',N'將房間 201 標記為 Clean。'),

    /* 補齊目前 develop 實際成功寫入格式的 OperationType 1～25 coverage。 */
    (38,1,DATEADD(MINUTE,600,CAST(DATEADD(DAY,-45,@Today) AS datetime2(0))),'E20260807001', 2,'Branch',N'台北中山商旅',N'修改分館資料：台北中山商旅。'),
    (39,6,DATEADD(MINUTE,780,CAST(DATEADD(DAY,-10,@Today) AS datetime2(0))),'E20260807001', 3,'Branch',N'花蓮站前商旅',N'將分館 花蓮站前商旅 設定為開放接受新訂房。'),
    (40,1,DATEADD(MINUTE,660,CAST(DATEADD(DAY,-59,@Today) AS datetime2(0))),'E20260807001', 5,'RoomType',N'經典單人房',N'新增房型：經典單人房'),
    (41,1,DATEADD(MINUTE,720,CAST(DATEADD(DAY,-40,@Today) AS datetime2(0))),'E20260807001', 6,'RoomType',N'經典單人房',N'修改房型：經典單人房。'),
    (42,5,@PanoramicTripleDisabledAt,'E20260807001', 7,'RoomType',N'全景三人房',N'停用房型：全景三人房。'),
    (43,6,DATEADD(MINUTE,720,CAST(DATEADD(DAY,-7,@Today) AS datetime2(0))),'E20260807001', 8,'RoomType',N'山海雙人房',N'啟用房型：山海雙人房。'),
    (44,1,DATEADD(MINUTE,720,CAST(DATEADD(DAY,-58,@Today) AS datetime2(0))),'E20260807001', 9,'Room',N'200',N'新增房間【200】(房型: 經典單人房, 樓層: 2, 初始狀態: Open)'),
    (45,1,DATEADD(MINUTE,720,CAST(DATEADD(DAY,-30,@Today) AS datetime2(0))),'E20260807001',10,'Room',N'201',N'修改房間【200】(房號: 200 -> 201, 樓層: 2 -> 2, 房型: 經典單人房 -> 經典單人房)'),
    (46,1,DATEADD(MINUTE,720,CAST(DATEADD(DAY,-6,@Today) AS datetime2(0))),'E20260807002',12,'Room',N'205',N'將房間 205 恢復開放販售。'),
    (47,1,CONVERT(datetime2(0),'2026-08-07T12:00:00'),'E20260807001',13,'Employee',N'E20260807002',N'建立分館員工 E20260807002(林怡君)。'),
    (48,1,DATEADD(MINUTE,720,CAST(DATEADD(DAY,-20,@Today) AS datetime2(0))),'E20260807001',14,'Employee',N'E20260807006',N'修改員工資料：E20260807006(蔡佩珊)。'),
    (49,1,DATEADD(MINUTE,720,CAST(DATEADD(DAY,-20,@Today) AS datetime2(0))),'E20260807001',16,'Employee',N'E20260807003',N'啟用員工帳號：E20260807003(陳柏宇)。'),
    (50,1,DATEADD(MINUTE,720,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))),'E20260807001',17,'Employee',N'E20260807006',N'重設員工密碼：E20260807006(蔡佩珊)。'),
    (51,1,DATEADD(MINUTE,720,CAST(DATEADD(DAY,-5,@Today) AS datetime2(0))),'E20260807002',19,'Room',N'202',N'將房間 202 供應狀態更新為 Open。'),
    (52,1,DATEADD(MINUTE,780,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))),'E20260807002',24,'Room',N'205',N'將房間 205 的停用原因由「空調異音，等待初步檢查。」修改為「空調主機異常，等待維修廠商到場。」。'),
    (53,1,DATEADD(MINUTE,840,CAST(DATEADD(DAY,-1,@Today) AS datetime2(0))),'E20260807002',25,'Employee',N'E20260807002',N'員工修改自己的登入密碼。');

    SET IDENTITY_INSERT [dbo].[OperationLogs] OFF;
    SET @IdentityInsertTable = NULL;

    DBCC CHECKIDENT ('dbo.StayRecords',   RESEED, 9)  WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.OperationLogs', RESEED, 53) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    SELECT N'開發情境完成' AS [Result],
           @NowTaipei AS [TaipeiNow],
           (SELECT COUNT(*) FROM [dbo].[Bookings]) AS [Bookings],
           (SELECT COUNT(*) FROM [dbo].[StayRecords]) AS [StayRecords],
           (SELECT COUNT(*) FROM [dbo].[OperationLogs]) AS [OperationLogs];

    SELECT
        RIGHT('000' + CONVERT(varchar(3), [ScenarioId]), 3) AS [ScenarioId],
        [BookingNumber],
        [RoomTypeId],
        [CreatedAt],
        [CheckInDate],
        [CheckOutDate],
        [BookingStatus]
    FROM @BookingScenarios
    ORDER BY [ScenarioId];
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
