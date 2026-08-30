/*
    HotelManagementSystem - 第一版穩定基準資料
    SQL Server

    建議執行順序：
    1. 01_create_hotel_management_schema.sql
    2. 02_sample_data.sql（本檔：穩定基礎資料）
    3. 03_development_scenarios.sql（動態開發情境）
    4. 04_development_volume_data.sql（可選：營運量體資料）
    5. 05_validate_sample_data.sql（唯讀驗證與摘要）

    本檔責任：
    - 分館、房型、實體房間、員工、固定操作類型
    - 固定主鍵、密碼雜湊、Identity seed 與可重跑清除順序

    注意：
    - 本檔會清除八張表的資料；只適用開發／測試資料庫。
    - 核心開發資料須依序執行至 03；需要查詢／匯出量體時再執行 04。
    - 全部測試帳號密碼固定為 Hotel@123。
    - 固定 PasswordHash 僅供本機開發／展示，不得用於正式環境。
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

    /* 依 FK 相依順序清除，避免與舊版單檔測資重複寫入。 */
    DELETE FROM [dbo].[OperationLogs];
    DELETE FROM [dbo].[StayRecords];
    DELETE FROM [dbo].[Bookings];
    DELETE FROM [dbo].[Rooms];
    DELETE FROM [dbo].[Employees];
    DELETE FROM [dbo].[OperationTypes];
    DELETE FROM [dbo].[RoomTypes];
    DELETE FROM [dbo].[Branches];

    DECLARE @SamplePasswordHash varchar(255) =
        'AQAAAAIAAYagAAAAEAARIjNEVWZ3iJmqu8zd7v+PeRFk6r5bp/etR1cXSVRJ3jQ7XCpEip30m5ie+Qu5vg==';

    /* =========================================================
       1. Branches：6 間分館、5 個縣市，台北市 2 間
       ========================================================= */
    SET @IdentityInsertTable = N'dbo.Branches';
    SET IDENTITY_INSERT [dbo].[Branches] ON;

    INSERT INTO [dbo].[Branches]
    (
        [BranchId], [BranchName], [Phone], [Address], [Description],
        [AcceptsNewBookings], [Region], [ImageUrl]
    )
    VALUES
    (1, N'台北中山商旅', '0225318801', N'台北市中山區南京東路一段88號',
        N'鄰近捷運中山站與商圈，以都會短住及商務旅客為主要客群。',
        1, N'北部', N'/images/seed/branches/taipei-zhongshan.jpg'),
    (2, N'台北信義商旅', '0227296602', N'台北市信義區松仁路120號',
        N'位於信義商圈，提供市景房型與較寬敞的家庭住宿選擇。',
        1, N'北部', N'/images/seed/branches/taipei-xinyi.jpg'),
    (3, N'台中草悟商旅', '0423267703', N'台中市西區公益路68號',
        N'鄰近草悟道，兼顧商務、親子與中部城市旅遊需求。',
        1, N'中部', N'/images/seed/branches/taichung-calligraphy-greenway.jpg'),
    (4, N'台南安平商旅', '062980804', N'台南市安平區永華路二段168號',
        N'以古都慢旅為主題，鄰近安平與市區主要景點。',
        1, N'南部', N'/images/seed/branches/tainan-anping.jpg'),
    (5, N'高雄港灣商旅', '072410805', N'高雄市前金區中華三路80號',
        N'鄰近港區與捷運，房型兼顧商務及家庭旅客。',
        1, N'南部', N'/images/seed/branches/kaohsiung-harbor.jpg'),
    (6, N'花蓮站前商旅', '038330806', N'花蓮縣花蓮市國聯一路55號',
        N'目前停止接受新訂房，用於驗證既有訂單仍可入住與退房。',
        0, N'東部', N'/images/seed/branches/hualien-station.jpg');

    SET IDENTITY_INSERT [dbo].[Branches] OFF;
    SET @IdentityInsertTable = NULL;

    /* =========================================================
       2. RoomTypes：每館 3～5 種，共 24 種
       ========================================================= */
    SET @IdentityInsertTable = N'dbo.RoomTypes';
    SET IDENTITY_INSERT [dbo].[RoomTypes] ON;

    INSERT INTO [dbo].[RoomTypes]
    (
        [RoomTypeId], [BranchId], [RoomTypeName], [MaxOccupancy],
        [BedType], [NightlyPrice], [IsActive], [Description], [ImageUrl]
    )
    VALUES
    ( 1, 1, N'經典單人房', 1, N'一小床',             2300.00, 1, N'適合單人商務短住，配置書桌與基本收納。', N'/images/seed/room-types/taipei-zhongshan-classic-single.jpg'),
    ( 2, 1, N'標準雙人房', 2, N'一大床',             3200.00, 1, N'中山館主要雙人房型，適合一般訂房流程測試。', N'/images/seed/room-types/taipei-zhongshan-standard-double.jpg'),
    ( 3, 1, N'豪華雙床房', 2, N'兩小床',             3800.00, 1, N'兩床配置，適合同行旅客。', N'/images/seed/room-types/taipei-zhongshan-deluxe-twin.jpg'),
    ( 4, 1, N'家庭四人房', 4, N'兩大床',             5200.00, 1, N'保留四人容量快照與實際入住人數上限測試。', N'/images/seed/room-types/taipei-zhongshan-family-quad.jpg'),
    ( 5, 1, N'行政三人房', 3, N'一大床＋一小床',     4500.00, 1, N'用於無合格房間可指派等櫃檯情境。', N'/images/seed/room-types/taipei-zhongshan-executive-triple.jpg'),

    ( 6, 2, N'都會單人房', 1, N'一小床',             2600.00, 1, N'信義商圈單人住宿選擇。', N'/images/seed/room-types/taipei-xinyi-city-single.jpg'),
    ( 7, 2, N'景觀雙人房', 2, N'一大床',             4200.00, 1, N'提供城市景觀的雙人房型。', N'/images/seed/room-types/taipei-xinyi-skyline-double.jpg'),
    ( 8, 2, N'豪華雙床房', 2, N'兩小床',             4600.00, 1, N'較寬敞的兩床房型。', N'/images/seed/room-types/taipei-xinyi-deluxe-twin.jpg'),
    ( 9, 2, N'家庭套房',   4, N'兩大床',             6500.00, 1, N'信義館家庭房，適合多晚訂單與匯出測試。', N'/images/seed/room-types/taipei-xinyi-family-suite.jpg'),

    (10, 3, N'標準雙人房', 2, N'一大床',             2900.00, 1, N'台中館基本雙人房。', N'/images/seed/room-types/taichung-standard-double.jpg'),
    (11, 3, N'舒適三人房', 3, N'一大床＋一小床',     3900.00, 1, N'適合三人同行。', N'/images/seed/room-types/taichung-comfort-triple.jpg'),
    (12, 3, N'家庭四人房', 4, N'兩大床',             4800.00, 1, N'台中館家庭房。', N'/images/seed/room-types/taichung-family-quad.jpg'),
    (13, 3, N'和風雙人房', 2, N'兩張日式床墊',       3500.00, 1, N'與同館其他雙人房形成不同價格與床型。', N'/images/seed/room-types/taichung-japanese-double.jpg'),

    (14, 4, N'古都雙人房', 2, N'一大床',             2700.00, 1, N'台南館基本雙人房。', N'/images/seed/room-types/tainan-classic-double.jpg'),
    (15, 4, N'庭院雙床房', 2, N'兩小床',             3400.00, 1, N'以庭院風格區分的兩床房型。', N'/images/seed/room-types/tainan-courtyard-twin.jpg'),
    (16, 4, N'家庭四人房', 4, N'兩大床',             4500.00, 1, N'台南館家庭住宿選擇。', N'/images/seed/room-types/tainan-family-quad.jpg'),

    (17, 5, N'商務單人房', 1, N'一小床',             2400.00, 1, N'高雄館單人商務房。', N'/images/seed/room-types/kaohsiung-business-single.jpg'),
    (18, 5, N'港景雙人房', 2, N'一大床',             3300.00, 1, N'可辨識的港景雙人房。', N'/images/seed/room-types/kaohsiung-harbor-double.jpg'),
    (19, 5, N'豪華雙床房', 2, N'兩小床',             3900.00, 1, N'高雄館兩床房。', N'/images/seed/room-types/kaohsiung-deluxe-twin.jpg'),
    (20, 5, N'家庭四人房', 4, N'兩大床',             5000.00, 1, N'高雄館家庭房。', N'/images/seed/room-types/kaohsiung-family-quad.jpg'),
    (21, 5, N'全景三人房', 3, N'一大床＋一小床',     4300.00, 0, N'停用房型；既有訂單仍可保留歷史快照。', N'/images/seed/room-types/kaohsiung-panoramic-triple.jpg'),

    (22, 6, N'山海雙人房', 2, N'一大床',             2600.00, 1, N'花蓮館基本雙人房。', N'/images/seed/room-types/hualien-classic-double.jpg'),
    (23, 6, N'山景雙床房', 2, N'兩小床',             3200.00, 1, N'花蓮館兩床房。', N'/images/seed/room-types/hualien-mountain-twin.jpg'),
    (24, 6, N'家庭四人房', 4, N'兩大床',             4400.00, 1, N'花蓮館家庭房；分館停接新訂房但可承接既有訂單。', N'/images/seed/room-types/hualien-family-quad.jpg');

    SET IDENTITY_INSERT [dbo].[RoomTypes] OFF;
    SET @IdentityInsertTable = NULL;

    /* =========================================================
       3. Rooms：每房型 5～12 間，共 188 間

       以固定計畫表產生可讀房號與固定 RoomId，避免維護 188 列
       幾乎相同的 INSERT。03 會重設並套用情境用供應／清潔狀態。
       ========================================================= */
    DECLARE @RoomPlan table
    (
        [RoomTypeId] int NOT NULL,
        [BranchId] int NOT NULL,
        [FirstRoomId] int NOT NULL,
        [FirstRoomNumber] int NOT NULL,
        [RoomCount] int NOT NULL
    );

    INSERT INTO @RoomPlan
        ([RoomTypeId], [BranchId], [FirstRoomId], [FirstRoomNumber], [RoomCount])
    VALUES
    ( 1, 1,   1, 201, 12), ( 2, 1,  13, 301,  9), ( 3, 1,  22, 401,  7), ( 4, 1,  29, 501,  6), ( 5, 1,  35, 601, 5),
    ( 6, 2,  40, 201, 10), ( 7, 2,  50, 301,  8), ( 8, 2,  58, 401,  9), ( 9, 2,  67, 501, 6),
    (10, 3,  73, 201, 11), (11, 3,  84, 301,  7), (12, 3,  91, 401,  8), (13, 3,  99, 501, 5),
    (14, 4, 104, 201,  9), (15, 4, 113, 301,  7), (16, 4, 120, 401,  6),
    (17, 5, 126, 201, 12), (18, 5, 138, 301, 10), (19, 5, 148, 401,  8), (20, 5, 156, 501, 7), (21, 5, 163, 601, 5),
    (22, 6, 168, 201,  8), (23, 6, 176, 301,  7), (24, 6, 183, 401,  6);

    IF EXISTS (SELECT 1 FROM @RoomPlan WHERE [RoomCount] NOT BETWEEN 5 AND 20)
        THROW 50001, N'每個房型的實體房間數必須介於 5 到 20。', 1;

    SET @IdentityInsertTable = N'dbo.Rooms';
    SET IDENTITY_INSERT [dbo].[Rooms] ON;

    ;WITH [Numbers] AS
    (
        SELECT [n]
        FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),
                     (11),(12),(13),(14),(15),(16),(17),(18),(19),(20)) AS V([n])
    )
    INSERT INTO [dbo].[Rooms]
    (
        [RoomId], [BranchId], [RoomTypeId], [RoomNumber], [Floor],
        [SupplyStatus], [CleaningStatus], [DisabledReason]
    )
    SELECT
        P.[FirstRoomId] + N.[n] - 1,
        P.[BranchId],
        P.[RoomTypeId],
        CONVERT(nvarchar(10), P.[FirstRoomNumber] + N.[n] - 1),
        CONVERT(smallint, (P.[FirstRoomNumber] + N.[n] - 1) / 100),
        'Open',
        'Clean',
        NULL
    FROM @RoomPlan AS P
    INNER JOIN [Numbers] AS N ON N.[n] <= P.[RoomCount];

    SET IDENTITY_INSERT [dbo].[Rooms] OFF;
    SET @IdentityInsertTable = NULL;

    /* =========================================================
       4. Employees：1 位總系統管理員、每館 2～3 位啟用員工、3 位停用員工
       ========================================================= */
    INSERT INTO [dbo].[Employees]
    (
        [EmployeeNumber], [EmployeeName], [IsActive], [BranchId], [PasswordHash], [Role]
    )
    VALUES
    ('E20260807001', N'系統管理員', 1, NULL, @SamplePasswordHash, 'SystemAdmin'),

    ('E20260807002', N'林怡君', 1, 1, @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807006', N'蔡佩珊', 1, 1, @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807009', N'陳冠廷', 1, 1, @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807003', N'陳柏宇', 0, 1, @SamplePasswordHash, 'BranchEmployee'),

    ('E20260807004', N'張雅婷', 1, 2, @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807010', N'吳承翰', 1, 2, @SamplePasswordHash, 'BranchEmployee'),

    ('E20260807007', N'黃詩涵', 1, 3, @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807011', N'林書妍', 1, 3, @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807012', N'江柏翰', 1, 3, @SamplePasswordHash, 'BranchEmployee'),

    ('E20260807013', N'郭怡安', 1, 4, @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807014', N'許家豪', 1, 4, @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807015', N'鄭雅文', 0, 4, @SamplePasswordHash, 'BranchEmployee'),

    ('E20260807005', N'王志豪', 1, 5, @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807016', N'周子晴', 1, 5, @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807017', N'謝宜庭', 1, 5, @SamplePasswordHash, 'BranchEmployee'),

    ('E20260807018', N'李佳穎', 1, 6, @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807019', N'楊宗翰', 1, 6, @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807008', N'劉思妤', 0, 6, @SamplePasswordHash, 'BranchEmployee');

    /* =========================================================
       5. OperationTypes：固定 ID 1～25

       StayController 目前以 22 / 23 寫入 Check-in / Check-out，
       因此保留既有 ID 與代碼，避免種子資料和程式不相容。
       ========================================================= */
    SET @IdentityInsertTable = N'dbo.OperationTypes';
    SET IDENTITY_INSERT [dbo].[OperationTypes] ON;

    INSERT INTO [dbo].[OperationTypes]
        ([OperationTypeId], [OperationTypeCode], [OperationTypeName])
    VALUES
    ( 1, 'BranchCreated',             N'建立分館'),
    ( 2, 'BranchUpdated',             N'修改分館'),
    ( 3, 'BranchBookingOpened',       N'開放新訂房'),
    ( 4, 'BranchBookingStopped',      N'停止新訂房'),
    ( 5, 'RoomTypeCreated',           N'建立房型'),
    ( 6, 'RoomTypeUpdated',           N'修改房型'),
    ( 7, 'RoomTypeDisabled',          N'停用房型'),
    ( 8, 'RoomTypeEnabled',           N'啟用房型'),
    ( 9, 'RoomCreated',               N'建立房間'),
    (10, 'RoomUpdated',               N'修改房間'),
    (11, 'RoomDisabled',              N'停用房間'),
    (12, 'RoomEnabled',               N'啟用房間'),
    (13, 'EmployeeCreated',           N'建立帳號'),
    (14, 'EmployeeUpdated',           N'修改帳號'),
    (15, 'EmployeeDisabled',          N'停用帳號'),
    (16, 'EmployeeEnabled',           N'啟用帳號'),
    (17, 'EmployeePasswordReset',     N'重設密碼'),
    (18, 'RoomReserved',              N'設為保留'),
    (19, 'RoomReservationReleased',   N'解除保留'),
    (20, 'RoomCleaningStatusChanged', N'更新清潔狀態'),
    (21, 'BookingCancelled',          N'取消訂單'),
    (22, 'CheckIn',                   N'Check-in'),
    (23, 'CheckOut',                  N'Check-out'),
    (24, 'RoomDisabledReasonUpdated', N'修改房間停用原因'),
    (25, 'EmployeePasswordChanged',   N'員工修改密碼');

    SET IDENTITY_INSERT [dbo].[OperationTypes] OFF;
    SET @IdentityInsertTable = NULL;

    /* 固定 ID 寫入後校正 seed，下一筆一般 INSERT 從 MAX + 1 接續。 */
    DBCC CHECKIDENT ('dbo.Branches',       RESEED, 6)   WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.RoomTypes',      RESEED, 24)  WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.Rooms',          RESEED, 188) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.StayRecords',    RESEED, 0)   WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.OperationTypes', RESEED, 25) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.OperationLogs',  RESEED, 0)   WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    SELECT N'基準資料完成' AS [Result],
           (SELECT COUNT(*) FROM [dbo].[Branches]) AS [Branches],
           (SELECT COUNT(*) FROM [dbo].[RoomTypes]) AS [RoomTypes],
           (SELECT COUNT(*) FROM [dbo].[Rooms]) AS [Rooms],
           (SELECT COUNT(*) FROM [dbo].[Employees]) AS [Employees],
           (SELECT COUNT(*) FROM [dbo].[OperationTypes]) AS [OperationTypes];
END TRY
BEGIN CATCH
    IF @IdentityInsertTable = N'dbo.Branches'
        SET IDENTITY_INSERT [dbo].[Branches] OFF;
    ELSE IF @IdentityInsertTable = N'dbo.RoomTypes'
        SET IDENTITY_INSERT [dbo].[RoomTypes] OFF;
    ELSE IF @IdentityInsertTable = N'dbo.Rooms'
        SET IDENTITY_INSERT [dbo].[Rooms] OFF;
    ELSE IF @IdentityInsertTable = N'dbo.OperationTypes'
        SET IDENTITY_INSERT [dbo].[OperationTypes] OFF;

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
