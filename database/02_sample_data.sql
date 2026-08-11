/*
    HotelManagementSystem - 第一版固定測試資料（擴充版）
    SQL Server

    使用方式：
    1. 先執行 01_create_hotel_management_schema.sql
    2. 再執行本檔案
    3. 本檔案每次執行都會先清除既有測試資料，再重新建立固定情境
    4. Identity 資料表使用固定 ID 寫入，避免全新資料庫與重跑時 RESEED 行為不同
    5. 訂房／住房日期以執行當天（台灣時間）為基準動態產生，避免測資隨時間失效

    測試帳號說明：
    - 全部測試帳號密碼固定為 Hotel@123。
    - PasswordHash 使用 ASP.NET Core PasswordHasher IdentityV3 相容格式。
    - 固定測試雜湊只供本機開發／展示使用，不得沿用至正式環境。

    主要測試情境：
    - 三間分館：兩間接受新訂房、一間停止新訂房
    - 啟用／停用房型
    - 房間供應狀態：Open / Reserved / Disabled
    - 房間清潔狀態：Clean / NeedsCleaning
    - 六種供應 × 清潔合法組合皆有固定測資
    - Disabled 房間皆有 DisabledReason；非 Disabled 房間皆為 NULL
    - 今日待入住、未來訂單、入住中、今日待退房、已完成、兩種取消、NoShow
    - Check-out 自動待清潔，以及非 Check-out 來源的手動標記待清潔
    - 基本操作紀錄包含供應、清潔、取消、Check-in、Check-out 等情境
*/

USE [HotelManagementSystem];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @IdentityInsertTable nvarchar(128) = NULL;

BEGIN TRY
    BEGIN TRANSACTION;

    /* =========================================================
       0. 清除既有測試資料

       不在空表上先執行 RESEED, 0。
       後續以 IDENTITY_INSERT 寫入固定 ID，最後再把 Identity
       seed 校正到固定測資的最大 ID，確保下一筆正常接續。
       ========================================================= */
    DELETE FROM [dbo].[OperationLogs];
    DELETE FROM [dbo].[StayRecords];
    DELETE FROM [dbo].[Bookings];
    DELETE FROM [dbo].[Rooms];
    DELETE FROM [dbo].[Employees];
    DELETE FROM [dbo].[OperationTypes];
    DELETE FROM [dbo].[RoomTypes];
    DELETE FROM [dbo].[Branches];

    /* =========================================================
       共用日期
       ========================================================= */
    DECLARE @NowTaipei datetime2(0) =
        CONVERT(datetime2(0), SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time');
    DECLARE @Today date = CAST(@NowTaipei AS date);

    DECLARE @SamplePasswordHash varchar(255) =
        'AQAAAAIAAYagAAAAEAARIjNEVWZ3iJmqu8zd7v+PeRFk6r5bp/etR1cXSVRJ3jQ7XCpEip30m5ie+Qu5vg==';

    /* =========================================================
       1. Branches
       固定 BranchId：
       1 台北、2 台中、3 高雄
       ========================================================= */
    SET @IdentityInsertTable = N'dbo.Branches';
    SET IDENTITY_INSERT [dbo].[Branches] ON;

    INSERT INTO [dbo].[Branches]
    (
        [BranchId],
        [BranchName],
        [Phone],
        [Address],
        [Description],
        [AcceptsNewBookings],
        [Region],
        [ImageUrl]
    )
    VALUES
    (
        1,
        N'台北商旅',
        '0225550101',
        N'台北市中山區中山北路一段100號',
        N'位於台北市中心，鄰近捷運與主要商圈。',
        1,
        N'北部',
        N'https://example.com/images/branches/taipei.jpg'
    ),
    (
        2,
        N'台中商旅',
        '0422220202',
        N'台中市西區台灣大道二段200號',
        N'位於台中市區，適合商務與短期住宿。',
        1,
        N'中部',
        N'https://example.com/images/branches/taichung.jpg'
    ),
    (
        3,
        N'高雄商旅',
        '0733330303',
        N'高雄市前金區中華三路300號',
        N'目前停止接受新訂房，但既有訂單仍可繼續處理。',
        0,
        N'南部',
        N'https://example.com/images/branches/kaohsiung.jpg'
    );

    SET IDENTITY_INSERT [dbo].[Branches] OFF;
    SET @IdentityInsertTable = NULL;

    /* =========================================================
       2. RoomTypes

       1-3：台北
       4-6：台中
       7-8：高雄
       ========================================================= */
    SET @IdentityInsertTable = N'dbo.RoomTypes';
    SET IDENTITY_INSERT [dbo].[RoomTypes] ON;

    INSERT INTO [dbo].[RoomTypes]
    (
        [RoomTypeId],
        [BranchId],
        [RoomTypeName],
        [MaxOccupancy],
        [BedType],
        [NightlyPrice],
        [IsActive],
        [Description],
        [ImageUrl]
    )
    VALUES
    (
        1, 1, N'標準雙人房', 2, N'一大床', 2800.00, 1,
        N'適合兩人入住的基本雙人房型。',
        N'https://example.com/images/room-types/taipei-standard-double.jpg'
    ),
    (
        2, 1, N'家庭四人房', 4, N'兩大床', 4200.00, 1,
        N'適合家庭或四人同行入住。',
        N'https://example.com/images/room-types/taipei-family-quad.jpg'
    ),
    (
        3, 1, N'經濟雙人房', 2, N'一大床', 2200.00, 0,
        N'測試停用房型，不應出現在新訂房結果。',
        N'https://example.com/images/room-types/taipei-economy-double.jpg'
    ),
    (
        4, 2, N'標準雙人房', 2, N'一大床', 2500.00, 1,
        N'台中館標準雙人房。',
        N'https://example.com/images/room-types/taichung-standard-double.jpg'
    ),
    (
        5, 2, N'三人房', 3, N'一大床＋一單人床', 3300.00, 1,
        N'適合三人同行入住。',
        N'https://example.com/images/room-types/taichung-triple.jpg'
    ),
    (
        6, 2, N'家庭四人房', 4, N'兩大床', 4700.00, 1,
        N'台中館家庭房，可測試不同房型與未來房量。',
        N'https://example.com/images/room-types/taichung-family-quad.jpg'
    ),
    (
        7, 3, N'標準雙人房', 2, N'一大床', 2300.00, 1,
        N'高雄館房型；分館目前停止接受新訂房。',
        N'https://example.com/images/room-types/kaohsiung-standard-double.jpg'
    ),
    (
        8, 3, N'家庭四人房', 4, N'兩大床', 3900.00, 1,
        N'高雄館家庭房；供既有訂單與房間狀態測試。',
        N'https://example.com/images/room-types/kaohsiung-family-quad.jpg'
    );

    SET IDENTITY_INSERT [dbo].[RoomTypes] OFF;
    SET @IdentityInsertTable = NULL;

    /* =========================================================
       3. Rooms

       六種合法組合皆有：
       - Open + Clean
       - Open + NeedsCleaning
       - Reserved + Clean
       - Reserved + NeedsCleaning
       - Disabled + Clean
       - Disabled + NeedsCleaning

       入住中由 StayRecords 推導，不另存在 Rooms。
       ========================================================= */
    SET @IdentityInsertTable = N'dbo.Rooms';
    SET IDENTITY_INSERT [dbo].[Rooms] ON;

    INSERT INTO [dbo].[Rooms]
    (
        [RoomId],
        [BranchId],
        [RoomTypeId],
        [RoomNumber],
        [Floor],
        [SupplyStatus],
        [CleaningStatus],
        [DisabledReason]
    )
    VALUES
    /* 台北｜標準雙人房 */
    ( 1, 1, 1, N'201', 2, 'Open',     'Clean',         NULL),
    ( 2, 1, 1, N'202', 2, 'Reserved', 'Clean',         NULL),
    ( 3, 1, 1, N'203', 2, 'Open',     'NeedsCleaning', NULL),
    ( 4, 1, 1, N'204', 2, 'Disabled', 'Clean',         N'冷氣故障待修。'),
    ( 5, 1, 1, N'205', 2, 'Open',     'Clean',         NULL),
    ( 6, 1, 1, N'206', 2, 'Reserved', 'NeedsCleaning', NULL),
    ( 7, 1, 1, N'207', 2, 'Disabled', 'NeedsCleaning', N'浴室設備施工後待清潔。'),

    /* 台北｜家庭四人房 */
    ( 8, 1, 2, N'301', 3, 'Open',     'Clean',         NULL),
    ( 9, 1, 2, N'302', 3, 'Open',     'NeedsCleaning', NULL),
    (10, 1, 2, N'303', 3, 'Reserved', 'Clean',         NULL),
    (11, 1, 2, N'304', 3, 'Disabled', 'Clean',         N'門鎖感應器故障。'),

    /* 台北｜停用房型 */
    (12, 1, 3, N'101', 1, 'Open',     'Clean',         NULL),

    /* 台中｜標準雙人房 */
    (13, 2, 4, N'201', 2, 'Open',     'Clean',         NULL),
    (14, 2, 4, N'202', 2, 'Open',     'Clean',         NULL),
    (15, 2, 4, N'203', 2, 'Reserved', 'NeedsCleaning', NULL),
    (16, 2, 4, N'204', 2, 'Disabled', 'Clean',         N'窗戶五金故障待修。'),
    (17, 2, 4, N'205', 2, 'Open',     'NeedsCleaning', NULL),

    /* 台中｜三人房 */
    (18, 2, 5, N'301', 3, 'Open',     'Clean',         NULL),
    (19, 2, 5, N'302', 3, 'Open',     'Clean',         NULL),
    (20, 2, 5, N'303', 3, 'Reserved', 'Clean',         NULL),

    /* 台中｜家庭四人房 */
    (21, 2, 6, N'401', 4, 'Open',     'Clean',         NULL),
    (22, 2, 6, N'402', 4, 'Disabled', 'NeedsCleaning', N'地毯更換施工中。'),

    /* 高雄｜標準雙人房 */
    (23, 3, 7, N'201', 2, 'Open',     'Clean',         NULL),
    (24, 3, 7, N'202', 2, 'Disabled', 'NeedsCleaning', N'浴室漏水維修中。'),
    (25, 3, 7, N'203', 2, 'Reserved', 'Clean',         NULL),

    /* 高雄｜家庭四人房 */
    (26, 3, 8, N'301', 3, 'Open',     'Clean',         NULL),
    (27, 3, 8, N'302', 3, 'Open',     'NeedsCleaning', NULL);

    SET IDENTITY_INSERT [dbo].[Rooms] OFF;
    SET @IdentityInsertTable = NULL;

    /* =========================================================
       4. Employees

       測試登入帳號：
       - E20260807001：總系統管理員
       - E20260807002：台北館員工 林怡君
       - E20260807003：台北館停用員工 陳柏宇
       - E20260807004：台中館員工 張雅婷
       - E20260807005：高雄館員工 王志豪
       - E20260807006：台北館員工 蔡佩珊
       - E20260807007：台中館員工 吳承翰
       - E20260807008：高雄館停用員工 鄭雅文
       ========================================================= */
    INSERT INTO [dbo].[Employees]
    (
        [EmployeeNumber],
        [EmployeeName],
        [IsActive],
        [BranchId],
        [PasswordHash],
        [Role]
    )
    VALUES
    ('E20260807001', N'系統管理員', 1, NULL, @SamplePasswordHash, 'SystemAdmin'),
    ('E20260807002', N'林怡君',     1, 1,    @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807003', N'陳柏宇',     0, 1,    @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807004', N'張雅婷',     1, 2,    @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807005', N'王志豪',     1, 3,    @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807006', N'蔡佩珊',     1, 1,    @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807007', N'吳承翰',     1, 2,    @SamplePasswordHash, 'BranchEmployee'),
    ('E20260807008', N'鄭雅文',     0, 3,    @SamplePasswordHash, 'BranchEmployee');

    /* =========================================================
       5. Bookings

       001：台北今日待入住 Paid
       002：台北未來 Paid
       003：台北入住中，今日待退房 CheckedIn
       004：台中已完成且已清潔 Completed
       005：台北顧客因素取消 Cancelled
       006：台中飯店因素取消 Cancelled
       007：台北逾期未入住 NoShow
       008：台中今日待入住 Paid
       009：台中入住中 CheckedIn
       010：高雄既有未來 Paid（分館之後停止新訂房）
       011：台北已完成但房間仍待清潔 Completed
       012：台北未來 Paid，與其他訂單形成房量測試
       013：台中家庭四人房未來 Paid
       ========================================================= */
    INSERT INTO [dbo].[Bookings]
    (
        [BookingNumber],
        [BranchId],
        [RoomTypeId],
        [BookerName],
        [ContactPhone],
        [Email],
        [CheckInDate],
        [CheckOutDate],
        [RoomTypeNameSnapshot],
        [MaxOccupancySnapshot],
        [NightlyPriceSnapshot],
        [TotalAmount],
        [BookingStatus],
        [CreatedAt],
        [CancellationCause],
        [CancellationReason],
        [CancelledAt],
        [CancelledByEmployeeNumber]
    )
    VALUES
    (
        'BK202608070001', 1, 1, N'陳冠宇', '0912345001', 'guest001@example.com',
        @Today, DATEADD(DAY, 2, @Today),
        N'標準雙人房', 2, 2800.00, 5600.00, 'Paid',
        DATEADD(MINUTE, 9 * 60 + 15, CAST(DATEADD(DAY, -3, @Today) AS datetime2(0))),
        NULL, NULL, NULL, NULL
    ),
    (
        'BK202608070002', 1, 2, N'李佳穎', '0912345002', 'guest002@example.com',
        DATEADD(DAY, 5, @Today), DATEADD(DAY, 7, @Today),
        N'家庭四人房', 4, 4200.00, 8400.00, 'Paid',
        DATEADD(MINUTE, 10 * 60 + 10, CAST(DATEADD(DAY, -1, @Today) AS datetime2(0))),
        NULL, NULL, NULL, NULL
    ),
    (
        'BK202608070003', 1, 1, N'周子晴', '0912345003', 'guest003@example.com',
        DATEADD(DAY, -2, @Today), @Today,
        N'標準雙人房', 2, 2800.00, 5600.00, 'CheckedIn',
        DATEADD(MINUTE, 11 * 60, CAST(DATEADD(DAY, -7, @Today) AS datetime2(0))),
        NULL, NULL, NULL, NULL
    ),
    (
        'BK202608070004', 2, 4, N'黃詩涵', '0912345004', 'guest004@example.com',
        DATEADD(DAY, -5, @Today), DATEADD(DAY, -3, @Today),
        N'標準雙人房', 2, 2500.00, 5000.00, 'Completed',
        DATEADD(MINUTE, 14 * 60 + 20, CAST(DATEADD(DAY, -12, @Today) AS datetime2(0))),
        NULL, NULL, NULL, NULL
    ),
    (
        'BK202608070005', 1, 2, N'吳家豪', '0912345005', 'guest005@example.com',
        DATEADD(DAY, 10, @Today), DATEADD(DAY, 12, @Today),
        N'家庭四人房', 4, 4200.00, 8400.00, 'Cancelled',
        DATEADD(MINUTE, 9 * 60 + 40, CAST(DATEADD(DAY, -4, @Today) AS datetime2(0))),
        'GuestRequest',
        N'顧客行程變更，於取消期限內提出取消。',
        DATEADD(MINUTE, 10 * 60 + 30, CAST(DATEADD(DAY, -1, @Today) AS datetime2(0))),
        'E20260807002'
    ),
    (
        'BK202608070006', 2, 5, N'林書妍', '0912345006', 'guest006@example.com',
        DATEADD(DAY, 1, @Today), DATEADD(DAY, 3, @Today),
        N'三人房', 3, 3300.00, 6600.00, 'Cancelled',
        DATEADD(MINUTE, 15 * 60, CAST(DATEADD(DAY, -6, @Today) AS datetime2(0))),
        'HotelUnableToFulfill',
        N'原房型設備故障且已確認無其他同房型合格房間可提供。',
        DATEADD(MINUTE, 11 * 60 + 20, CAST(@Today AS datetime2(0))),
        'E20260807004'
    ),
    (
        'BK202608070007', 1, 1, N'許博翔', '0912345007', 'guest007@example.com',
        DATEADD(DAY, -4, @Today), DATEADD(DAY, -2, @Today),
        N'標準雙人房', 2, 2800.00, 5600.00, 'NoShow',
        DATEADD(MINUTE, 8 * 60 + 50, CAST(DATEADD(DAY, -10, @Today) AS datetime2(0))),
        NULL, NULL, NULL, NULL
    ),
    (
        'BK202608070008', 2, 4, N'楊宗翰', '0912345008', 'guest008@example.com',
        @Today, DATEADD(DAY, 1, @Today),
        N'標準雙人房', 2, 2500.00, 2500.00, 'Paid',
        DATEADD(MINUTE, 12 * 60 + 5, CAST(DATEADD(DAY, -2, @Today) AS datetime2(0))),
        NULL, NULL, NULL, NULL
    ),
    (
        'BK202608070009', 2, 5, N'謝宜庭', '0912345009', 'guest009@example.com',
        DATEADD(DAY, -1, @Today), DATEADD(DAY, 1, @Today),
        N'三人房', 3, 3300.00, 6600.00, 'CheckedIn',
        DATEADD(MINUTE, 16 * 60 + 30, CAST(DATEADD(DAY, -5, @Today) AS datetime2(0))),
        NULL, NULL, NULL, NULL
    ),
    (
        'BK202608070010', 3, 7, N'張凱翔', '0912345010', 'guest010@example.com',
        DATEADD(DAY, 3, @Today), DATEADD(DAY, 5, @Today),
        N'標準雙人房', 2, 2300.00, 4600.00, 'Paid',
        DATEADD(MINUTE, 13 * 60 + 15, CAST(DATEADD(DAY, -8, @Today) AS datetime2(0))),
        NULL, NULL, NULL, NULL
    ),
    (
        'BK202608070011', 1, 2, N'劉思妤', '0912345011', 'guest011@example.com',
        DATEADD(DAY, -8, @Today), DATEADD(DAY, -6, @Today),
        N'家庭四人房', 4, 4200.00, 8400.00, 'Completed',
        DATEADD(MINUTE, 9 * 60 + 25, CAST(DATEADD(DAY, -14, @Today) AS datetime2(0))),
        NULL, NULL, NULL, NULL
    ),
    (
        'BK202608070012', 1, 1, N'王柏鈞', '0912345012', 'guest012@example.com',
        DATEADD(DAY, 1, @Today), DATEADD(DAY, 4, @Today),
        N'標準雙人房', 2, 2800.00, 8400.00, 'Paid',
        DATEADD(MINUTE, 11 * 60 + 45, CAST(DATEADD(DAY, -2, @Today) AS datetime2(0))),
        NULL, NULL, NULL, NULL
    ),
    (
        'BK202608070013', 2, 6, N'陳怡安', '0912345013', 'guest013@example.com',
        DATEADD(DAY, 4, @Today), DATEADD(DAY, 6, @Today),
        N'家庭四人房', 4, 4700.00, 9400.00, 'Paid',
        DATEADD(MINUTE, 14 * 60 + 5, CAST(DATEADD(DAY, -1, @Today) AS datetime2(0))),
        NULL, NULL, NULL, NULL
    );

    /* =========================================================
       6. StayRecords

       1：台北入住中，今日待退房
       2：台中已完成，房間後續已清潔
       3：台中入住中
       4：台北已完成，但房間仍待清潔
       ========================================================= */
    SET @IdentityInsertTable = N'dbo.StayRecords';
    SET IDENTITY_INSERT [dbo].[StayRecords] ON;

    INSERT INTO [dbo].[StayRecords]
    (
        [StayRecordId],
        [BookingNumber],
        [RoomId],
        [RoomNumberSnapshot],
        [ActualCheckInAt],
        [ActualCheckOutAt],
        [PrimaryGuestName],
        [ActualGuestCount],
        [CheckedInByEmployeeNumber],
        [CheckedOutByEmployeeNumber]
    )
    VALUES
    (
        1,
        'BK202608070003',
        1,
        N'201',
        DATEADD(MINUTE, 16 * 60 + 25, CAST(DATEADD(DAY, -2, @Today) AS datetime2(0))),
        NULL,
        N'周子晴',
        2,
        'E20260807002',
        NULL
    ),
    (
        2,
        'BK202608070004',
        13,
        N'201',
        DATEADD(MINUTE, 16 * 60 + 10, CAST(DATEADD(DAY, -5, @Today) AS datetime2(0))),
        DATEADD(MINUTE, 10 * 60 + 35, CAST(DATEADD(DAY, -3, @Today) AS datetime2(0))),
        N'黃詩涵',
        2,
        'E20260807004',
        'E20260807004'
    ),
    (
        3,
        'BK202608070009',
        19,
        N'302',
        DATEADD(MINUTE, 16 * 60 + 40, CAST(DATEADD(DAY, -1, @Today) AS datetime2(0))),
        NULL,
        N'謝宜庭',
        3,
        'E20260807004',
        NULL
    ),
    (
        4,
        'BK202608070011',
        9,
        N'302',
        DATEADD(MINUTE, 16 * 60 + 5, CAST(DATEADD(DAY, -8, @Today) AS datetime2(0))),
        DATEADD(MINUTE, 11 * 60 + 5, CAST(DATEADD(DAY, -6, @Today) AS datetime2(0))),
        N'劉思妤',
        4,
        'E20260807006',
        'E20260807006'
    );

    SET IDENTITY_INSERT [dbo].[StayRecords] OFF;
    SET @IdentityInsertTable = NULL;

    /* =========================================================
       7. OperationTypes
       固定 ID 與代碼，避免 OperationLogs 依賴自動編號
       ========================================================= */
    SET @IdentityInsertTable = N'dbo.OperationTypes';
    SET IDENTITY_INSERT [dbo].[OperationTypes] ON;

    INSERT INTO [dbo].[OperationTypes]
    (
        [OperationTypeId],
        [OperationTypeCode],
        [OperationTypeName]
    )
    VALUES
    ( 1, 'BranchCreated',               N'建立分館'),
    ( 2, 'BranchUpdated',               N'修改分館'),
    ( 3, 'BranchBookingOpened',         N'開放新訂房'),
    ( 4, 'BranchBookingStopped',        N'停止新訂房'),

    ( 5, 'RoomTypeCreated',             N'建立房型'),
    ( 6, 'RoomTypeUpdated',             N'修改房型'),
    ( 7, 'RoomTypeDisabled',            N'停用房型'),
    ( 8, 'RoomTypeEnabled',             N'啟用房型'),

    ( 9, 'RoomCreated',                 N'建立房間'),
    (10, 'RoomUpdated',                 N'修改房間'),
    (11, 'RoomDisabled',                N'停用房間'),
    (12, 'RoomEnabled',                 N'啟用房間'),

    (13, 'EmployeeCreated',             N'建立帳號'),
    (14, 'EmployeeUpdated',             N'修改帳號'),
    (15, 'EmployeeDisabled',            N'停用帳號'),
    (16, 'EmployeeEnabled',             N'啟用帳號'),
    (17, 'EmployeePasswordReset',       N'重設密碼'),

    (18, 'RoomReserved',                N'設為保留'),
    (19, 'RoomReservationReleased',     N'解除保留'),
    (20, 'RoomCleaningStatusChanged',   N'更新清潔狀態'),

    (21, 'BookingCancelled',            N'取消訂單'),
    (22, 'CheckIn',                     N'Check-in'),
    (23, 'CheckOut',                    N'Check-out');

    SET IDENTITY_INSERT [dbo].[OperationTypes] OFF;
    SET @IdentityInsertTable = NULL;

    /* =========================================================
       8. OperationLogs
       ========================================================= */
    SET @IdentityInsertTable = N'dbo.OperationLogs';
    SET IDENTITY_INSERT [dbo].[OperationLogs] ON;

    INSERT INTO [dbo].[OperationLogs]
    (
        [OperationLogId],
        [TargetBranchId],
        [OperatedAt],
        [OperatorEmployeeNumber],
        [OperationTypeId],
        [TargetType],
        [TargetIdentifier],
        [Description]
    )
    VALUES
    ( 1, 1,
        DATEADD(MINUTE, 9 * 60, CAST(DATEADD(DAY, -30, @Today) AS datetime2(0))),
        'E20260807001', 1, 'Branch', N'1',
        N'建立台北商旅分館。'
    ),
    ( 2, 2,
        DATEADD(MINUTE, 9 * 60 + 15, CAST(DATEADD(DAY, -30, @Today) AS datetime2(0))),
        'E20260807001', 1, 'Branch', N'2',
        N'建立台中商旅分館。'
    ),
    ( 3, 3,
        DATEADD(MINUTE, 9 * 60 + 30, CAST(DATEADD(DAY, -30, @Today) AS datetime2(0))),
        'E20260807001', 1, 'Branch', N'3',
        N'建立高雄商旅分館。'
    ),
    ( 4, 3,
        DATEADD(MINUTE, 13 * 60 + 20, CAST(DATEADD(DAY, -5, @Today) AS datetime2(0))),
        'E20260807001', 4, 'Branch', N'3',
        N'將高雄商旅設定為停止接受新訂房。'
    ),
    ( 5, 1,
        DATEADD(MINUTE, 14 * 60 + 10, CAST(DATEADD(DAY, -4, @Today) AS datetime2(0))),
        'E20260807002', 18, 'Room', N'202',
        N'將台北商旅房間 202 設為保留。'
    ),
    ( 6, 1,
        DATEADD(MINUTE, 13 * 60 + 40, CAST(DATEADD(DAY, -4, @Today) AS datetime2(0))),
        'E20260807001', 11, 'Room', N'204',
        N'將台北商旅房間 204 設為停用，原因：冷氣故障待修。'
    ),
    ( 7, 1,
        DATEADD(MINUTE, 15 * 60 + 10, CAST(DATEADD(DAY, -3, @Today) AS datetime2(0))),
        'E20260807002', 20, 'Room', N'203',
        N'將台北商旅房間 203 清潔狀態由已清潔改為待清潔。'
    ),
    ( 8, 1,
        DATEADD(MINUTE, 16 * 60 + 25, CAST(DATEADD(DAY, -2, @Today) AS datetime2(0))),
        'E20260807002', 22, 'Booking', N'BK202608070003',
        N'完成訂單 BK202608070003 的 Check-in，指派房間 201。'
    ),
    ( 9, 2,
        DATEADD(MINUTE, 16 * 60 + 10, CAST(DATEADD(DAY, -5, @Today) AS datetime2(0))),
        'E20260807004', 22, 'Booking', N'BK202608070004',
        N'完成訂單 BK202608070004 的 Check-in，指派房間 201。'
    ),
    (10, 2,
        DATEADD(MINUTE, 10 * 60 + 35, CAST(DATEADD(DAY, -3, @Today) AS datetime2(0))),
        'E20260807004', 23, 'Booking', N'BK202608070004',
        N'完成訂單 BK202608070004 的 Check-out，房間 201 轉為待清潔。'
    ),
    (11, 2,
        DATEADD(MINUTE, 13 * 60 + 10, CAST(DATEADD(DAY, -3, @Today) AS datetime2(0))),
        'E20260807004', 20, 'Room', N'201',
        N'收到清潔完成通知，將台中商旅房間 201 由待清潔改為已清潔。'
    ),
    (12, 1,
        DATEADD(MINUTE, 10 * 60 + 30, CAST(DATEADD(DAY, -1, @Today) AS datetime2(0))),
        'E20260807002', 21, 'Booking', N'BK202608070005',
        N'因顧客因素取消訂單 BK202608070005。'
    ),
    (13, 2,
        DATEADD(MINUTE, 11 * 60 + 20, CAST(@Today AS datetime2(0))),
        'E20260807004', 21, 'Booking', N'BK202608070006',
        N'因飯店確認無法依原訂單提供住宿，取消訂單 BK202608070006。'
    ),
    (14, 1,
        DATEADD(MINUTE, 15 * 60 + 5, CAST(DATEADD(DAY, -1, @Today) AS datetime2(0))),
        'E20260807001', 15, 'Employee', N'E20260807003',
        N'停用台北商旅員工帳號 E20260807003。'
    ),
    (15, 2,
        DATEADD(MINUTE, 16 * 60 + 40, CAST(DATEADD(DAY, -1, @Today) AS datetime2(0))),
        'E20260807004', 22, 'Booking', N'BK202608070009',
        N'完成訂單 BK202608070009 的 Check-in，指派房間 302。'
    ),
    (16, 1,
        DATEADD(MINUTE, 16 * 60 + 5, CAST(DATEADD(DAY, -8, @Today) AS datetime2(0))),
        'E20260807006', 22, 'Booking', N'BK202608070011',
        N'完成訂單 BK202608070011 的 Check-in，指派房間 302。'
    ),
    (17, 1,
        DATEADD(MINUTE, 11 * 60 + 5, CAST(DATEADD(DAY, -6, @Today) AS datetime2(0))),
        'E20260807006', 23, 'Booking', N'BK202608070011',
        N'完成訂單 BK202608070011 的 Check-out，房間 302 轉為待清潔。'
    ),
    (18, 1,
        DATEADD(MINUTE, 9 * 60 + 50, CAST(DATEADD(DAY, -2, @Today) AS datetime2(0))),
        'E20260807002', 18, 'Room', N'206',
        N'將台北商旅房間 206 設為保留；房間清潔狀態仍獨立為待清潔。'
    ),
    (19, 3,
        DATEADD(MINUTE, 14 * 60 + 25, CAST(DATEADD(DAY, -4, @Today) AS datetime2(0))),
        'E20260807005', 11, 'Room', N'202',
        N'將高雄商旅房間 202 設為停用，原因：浴室漏水維修中。'
    ),
    (20, 2,
        DATEADD(MINUTE, 10 * 60 + 15, CAST(DATEADD(DAY, -2, @Today) AS datetime2(0))),
        'E20260807007', 20, 'Room', N'203',
        N'將台中商旅房間 203 清潔狀態由已清潔改為待清潔；供應狀態仍維持保留。'
    );

    SET IDENTITY_INSERT [dbo].[OperationLogs] OFF;
    SET @IdentityInsertTable = NULL;

    /* =========================================================
       9. 校正 Identity seed

       由於上面使用固定 ID 寫入，這裡在資料已存在的情況下
       將 seed 設為最大固定 ID，下一筆自動編號會從 MAX + 1 開始。
       ========================================================= */
    DBCC CHECKIDENT ('dbo.Branches',       RESEED, 3)  WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.RoomTypes',      RESEED, 8)  WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.Rooms',          RESEED, 27) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.StayRecords',    RESEED, 4)  WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.OperationTypes', RESEED, 23) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.OperationLogs',  RESEED, 20) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    /* =========================================================
       建立完成摘要
       ========================================================= */
    SELECT N'Branches' AS [TableName], COUNT(*) AS [RowCount]
    FROM [dbo].[Branches]
    UNION ALL
    SELECT N'RoomTypes', COUNT(*) FROM [dbo].[RoomTypes]
    UNION ALL
    SELECT N'Rooms', COUNT(*) FROM [dbo].[Rooms]
    UNION ALL
    SELECT N'Employees', COUNT(*) FROM [dbo].[Employees]
    UNION ALL
    SELECT N'Bookings', COUNT(*) FROM [dbo].[Bookings]
    UNION ALL
    SELECT N'StayRecords', COUNT(*) FROM [dbo].[StayRecords]
    UNION ALL
    SELECT N'OperationTypes', COUNT(*) FROM [dbo].[OperationTypes]
    UNION ALL
    SELECT N'OperationLogs', COUNT(*) FROM [dbo].[OperationLogs];

    /* 房間狀態快速檢查 */
    SELECT
        [RoomId],
        [BranchId],
        [RoomTypeId],
        [RoomNumber],
        [SupplyStatus],
        [CleaningStatus],
        [DisabledReason]
    FROM [dbo].[Rooms]
    ORDER BY [BranchId], [RoomTypeId], [RoomId];

END TRY
BEGIN CATCH
    /* 若錯誤發生在 IDENTITY_INSERT 開啟期間，先還原目前資料表的 Session 狀態 */
    IF @IdentityInsertTable = N'dbo.Branches'
        SET IDENTITY_INSERT [dbo].[Branches] OFF;
    ELSE IF @IdentityInsertTable = N'dbo.RoomTypes'
        SET IDENTITY_INSERT [dbo].[RoomTypes] OFF;
    ELSE IF @IdentityInsertTable = N'dbo.Rooms'
        SET IDENTITY_INSERT [dbo].[Rooms] OFF;
    ELSE IF @IdentityInsertTable = N'dbo.StayRecords'
        SET IDENTITY_INSERT [dbo].[StayRecords] OFF;
    ELSE IF @IdentityInsertTable = N'dbo.OperationTypes'
        SET IDENTITY_INSERT [dbo].[OperationTypes] OFF;
    ELSE IF @IdentityInsertTable = N'dbo.OperationLogs'
        SET IDENTITY_INSERT [dbo].[OperationLogs] OFF;

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
