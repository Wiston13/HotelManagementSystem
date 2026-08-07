/*
    HotelManagementSystem - 第一版固定測試資料
    SQL Server

    使用方式：
    1. 先執行 01_create_hotel_management_schema.sql
    2. 再執行本檔案
    3. 本檔案每次執行都會先清除既有測試資料，再重新建立固定情境
    4. 訂房／住房日期會以「執行當天」為基準動態產生，避免測資隨時間失效

    測試帳號說明：
    - PasswordHash 目前以 SQL Server SHA2_256 產生測試雜湊，
      只用於資料庫測試資料。
    - 未來 ASP.NET 登入若採用 PasswordHasher，請由應用程式重新產生相容雜湊。
*/

USE [HotelManagementSystem];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /* =========================================================
       0. 清除既有測試資料
       ========================================================= */
    DELETE FROM [dbo].[OperationLogs];
    DELETE FROM [dbo].[StayRecords];
    DELETE FROM [dbo].[Bookings];
    DELETE FROM [dbo].[Rooms];
    DELETE FROM [dbo].[Employees];
    DELETE FROM [dbo].[OperationTypes];
    DELETE FROM [dbo].[RoomTypes];
    DELETE FROM [dbo].[Branches];

    DBCC CHECKIDENT ('dbo.OperationLogs', RESEED, 0);
    DBCC CHECKIDENT ('dbo.StayRecords', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Rooms', RESEED, 0);
    DBCC CHECKIDENT ('dbo.OperationTypes', RESEED, 0);
    DBCC CHECKIDENT ('dbo.RoomTypes', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Branches', RESEED, 0);

    /* =========================================================
       共用日期
       ========================================================= */
    DECLARE @Today date = CAST(GETDATE() AS date);

    DECLARE @Yesterday date = DATEADD(DAY, -1, @Today);
    DECLARE @Tomorrow date = DATEADD(DAY, 1, @Today);

    DECLARE @SamplePasswordHash varchar(255) =
        CONVERT(varchar(64), HASHBYTES('SHA2_256', 'Hotel@123'), 2);

    /* =========================================================
       1. Branches
       ========================================================= */
    INSERT INTO [dbo].[Branches]
    (
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
        N'台北商旅',
        '02-2555-0101',
        N'台北市中山區中山北路一段100號',
        N'位於台北市中心，鄰近捷運與主要商圈。',
        1,
        N'北部',
        N'https://example.com/images/branches/taipei.jpg'
    ),
    (
        N'台中商旅',
        '04-2222-0202',
        N'台中市西區台灣大道二段200號',
        N'位於台中市區，適合商務與短期住宿。',
        1,
        N'中部',
        N'https://example.com/images/branches/taichung.jpg'
    ),
    (
        N'高雄商旅',
        '07-3333-0303',
        N'高雄市前金區中華三路300號',
        N'目前停止接受新訂房，但既有訂單仍可繼續處理。',
        0,
        N'南部',
        N'https://example.com/images/branches/kaohsiung.jpg'
    );

    /* =========================================================
       2. RoomTypes
       ========================================================= */
    INSERT INTO [dbo].[RoomTypes]
    (
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
        1,
        N'標準雙人房',
        2,
        N'一大床',
        2800.00,
        1,
        N'適合兩人入住的基本雙人房型。',
        N'https://example.com/images/room-types/taipei-standard-double.jpg'
    ),
    (
        1,
        N'家庭四人房',
        4,
        N'兩大床',
        4200.00,
        1,
        N'適合家庭或四人同行入住。',
        N'https://example.com/images/room-types/taipei-family-quad.jpg'
    ),
    (
        1,
        N'經濟雙人房',
        2,
        N'一大床',
        2200.00,
        0,
        N'測試停用房型，不應出現在新訂房結果。',
        N'https://example.com/images/room-types/taipei-economy-double.jpg'
    ),
    (
        2,
        N'標準雙人房',
        2,
        N'一大床',
        2500.00,
        1,
        N'台中館標準雙人房。',
        N'https://example.com/images/room-types/taichung-standard-double.jpg'
    ),
    (
        2,
        N'三人房',
        3,
        N'一大床＋一單人床',
        3300.00,
        1,
        N'適合三人同行入住。',
        N'https://example.com/images/room-types/taichung-triple.jpg'
    ),
    (
        3,
        N'標準雙人房',
        2,
        N'一大床',
        2300.00,
        1,
        N'高雄館房型；分館目前停止接受新訂房。',
        N'https://example.com/images/room-types/kaohsiung-standard-double.jpg'
    );

    /* =========================================================
       3. Rooms

       房間情境：
       - Open / Reserved / Disabled
       - Clean / NeedsCleaning
       - RoomId 1：目前入住中
       - RoomId 9：已退房，待清潔
       ========================================================= */
    INSERT INTO [dbo].[Rooms]
    (
        [BranchId],
        [RoomTypeId],
        [RoomNumber],
        [Floor],
        [SupplyStatus],
        [CleaningStatus]
    )
    VALUES
    (1, 1, N'201', 2, 'Open',     'Clean'),
    (1, 1, N'202', 2, 'Reserved', 'Clean'),
    (1, 1, N'203', 2, 'Open',     'NeedsCleaning'),
    (1, 1, N'204', 2, 'Disabled', 'Clean'),
    (1, 1, N'205', 2, 'Open',     'Clean'),

    (1, 2, N'301', 3, 'Open',     'Clean'),
    (1, 2, N'302', 3, 'Open',     'Clean'),

    (1, 3, N'101', 1, 'Open',     'Clean'),

    (2, 4, N'201', 2, 'Open',     'NeedsCleaning'),
    (2, 4, N'202', 2, 'Open',     'Clean'),
    (2, 4, N'203', 2, 'Reserved', 'Clean'),

    (2, 5, N'301', 3, 'Open',     'Clean'),
    (2, 5, N'302', 3, 'Open',     'NeedsCleaning'),

    (3, 6, N'201', 2, 'Open',     'Clean'),
    (3, 6, N'202', 2, 'Disabled', 'Clean');

    /* =========================================================
       4. Employees

       測試登入帳號：
       - E20260807001：總系統管理員
       - E20260807002：台北館員工 林怡君
       - E20260807003：台北館停用員工 陳柏宇
       - E20260807004：台中館員工 張雅婷
       - E20260807005：高雄館員工 王志豪
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
    ('E20260807005', N'王志豪',     1, 3,    @SamplePasswordHash, 'BranchEmployee');

    /* =========================================================
       5. Bookings

       BK202608070001：今日待入住 Paid
       BK202608070002：未來 Paid
       BK202608070003：目前入住中 CheckedIn
       BK202608070004：已完成 Completed
       BK202608070005：顧客因素取消 Cancelled
       BK202608070006：飯店因素取消 Cancelled
       BK202608070007：逾期未入住 NoShow
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
        'BK202608070001',
        1,
        1,
        N'陳冠宇',
        '0912-345-001',
        'guest001@example.com',
        @Today,
        DATEADD(DAY, 2, @Today),
        N'標準雙人房',
        2800.00,
        5600.00,
        'Paid',
        DATEADD(MINUTE, 9 * 60 + 15, CAST(DATEADD(DAY, -3, @Today) AS datetime2(0))),
        NULL,
        NULL,
        NULL,
        NULL
    ),
    (
        'BK202608070002',
        1,
        2,
        N'李佳穎',
        '0912-345-002',
        'guest002@example.com',
        DATEADD(DAY, 5, @Today),
        DATEADD(DAY, 7, @Today),
        N'家庭四人房',
        4200.00,
        8400.00,
        'Paid',
        DATEADD(MINUTE, 10 * 60 + 10, CAST(DATEADD(DAY, -1, @Today) AS datetime2(0))),
        NULL,
        NULL,
        NULL,
        NULL
    ),
    (
        'BK202608070003',
        1,
        1,
        N'周子晴',
        '0912-345-003',
        'guest003@example.com',
        DATEADD(DAY, -2, @Today),
        @Today,
        N'標準雙人房',
        2800.00,
        5600.00,
        'CheckedIn',
        DATEADD(MINUTE, 11 * 60, CAST(DATEADD(DAY, -7, @Today) AS datetime2(0))),
        NULL,
        NULL,
        NULL,
        NULL
    ),
    (
        'BK202608070004',
        2,
        4,
        N'黃詩涵',
        '0912-345-004',
        'guest004@example.com',
        DATEADD(DAY, -5, @Today),
        DATEADD(DAY, -3, @Today),
        N'標準雙人房',
        2500.00,
        5000.00,
        'Completed',
        DATEADD(MINUTE, 14 * 60 + 20, CAST(DATEADD(DAY, -12, @Today) AS datetime2(0))),
        NULL,
        NULL,
        NULL,
        NULL
    ),
    (
        'BK202608070005',
        1,
        2,
        N'吳家豪',
        '0912-345-005',
        'guest005@example.com',
        DATEADD(DAY, 10, @Today),
        DATEADD(DAY, 12, @Today),
        N'家庭四人房',
        4200.00,
        8400.00,
        'Cancelled',
        DATEADD(MINUTE, 9 * 60 + 40, CAST(DATEADD(DAY, -4, @Today) AS datetime2(0))),
        'GuestRequest',
        N'顧客行程變更，於取消期限內提出取消。',
        DATEADD(MINUTE, 10 * 60 + 30, CAST(DATEADD(DAY, -1, @Today) AS datetime2(0))),
        'E20260807002'
    ),
    (
        'BK202608070006',
        2,
        5,
        N'林書妍',
        '0912-345-006',
        'guest006@example.com',
        DATEADD(DAY, 1, @Today),
        DATEADD(DAY, 3, @Today),
        N'三人房',
        3300.00,
        6600.00,
        'Cancelled',
        DATEADD(MINUTE, 15 * 60, CAST(DATEADD(DAY, -6, @Today) AS datetime2(0))),
        'HotelUnableToFulfill',
        N'原房型設備故障且已確認無其他同房型合格房間可提供。',
        DATEADD(MINUTE, 11 * 60 + 20, CAST(@Today AS datetime2(0))),
        'E20260807004'
    ),
    (
        'BK202608070007',
        1,
        1,
        N'許博翔',
        '0912-345-007',
        'guest007@example.com',
        DATEADD(DAY, -4, @Today),
        DATEADD(DAY, -2, @Today),
        N'標準雙人房',
        2800.00,
        5600.00,
        'NoShow',
        DATEADD(MINUTE, 8 * 60 + 50, CAST(DATEADD(DAY, -10, @Today) AS datetime2(0))),
        NULL,
        NULL,
        NULL,
        NULL
    );

    /* =========================================================
       6. StayRecords

       一筆入住中、一筆已退房
       ========================================================= */
    INSERT INTO [dbo].[StayRecords]
    (
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
        'BK202608070004',
        9,
        N'201',
        DATEADD(MINUTE, 16 * 60 + 10, CAST(DATEADD(DAY, -5, @Today) AS datetime2(0))),
        DATEADD(MINUTE, 10 * 60 + 35, CAST(DATEADD(DAY, -3, @Today) AS datetime2(0))),
        N'黃詩涵',
        2,
        'E20260807004',
        'E20260807004'
    );

    /* =========================================================
       7. OperationTypes
       固定操作類型，供系統寫入與查詢頁下拉選單使用
       ========================================================= */
    INSERT INTO [dbo].[OperationTypes]
    (
        [OperationTypeCode],
        [OperationTypeName]
    )
    VALUES
    ('BranchCreated',               N'建立分館'),
    ('BranchUpdated',               N'修改分館'),
    ('BranchBookingOpened',         N'開放新訂房'),
    ('BranchBookingStopped',        N'停止新訂房'),

    ('RoomTypeCreated',             N'建立房型'),
    ('RoomTypeUpdated',             N'修改房型'),
    ('RoomTypeDisabled',            N'停用房型'),
    ('RoomTypeEnabled',             N'啟用房型'),

    ('RoomCreated',                 N'建立房間'),
    ('RoomUpdated',                 N'修改房間'),
    ('RoomDisabled',                N'停用房間'),
    ('RoomEnabled',                 N'啟用房間'),

    ('EmployeeCreated',             N'建立帳號'),
    ('EmployeeUpdated',             N'修改帳號'),
    ('EmployeeDisabled',            N'停用帳號'),
    ('EmployeeEnabled',             N'啟用帳號'),
    ('EmployeePasswordReset',       N'重設密碼'),

    ('RoomReserved',                N'設為保留'),
    ('RoomReservationReleased',     N'解除保留'),
    ('RoomCleaningStatusChanged',   N'更新清潔狀態'),

    ('BookingCancelled',            N'取消訂單'),
    ('CheckIn',                     N'Check-in'),
    ('CheckOut',                    N'Check-out');

    /* =========================================================
       8. OperationLogs

       注意：
       - TargetBranchId 是「操作對象的分館」
       - NoShow 為系統自動判定，不建立員工操作紀錄
       ========================================================= */
    INSERT INTO [dbo].[OperationLogs]
    (
        [TargetBranchId],
        [OperatedAt],
        [OperatorEmployeeNumber],
        [OperationTypeId],
        [TargetType],
        [TargetIdentifier],
        [Description]
    )
    VALUES
    (
        1,
        DATEADD(MINUTE, 9 * 60, CAST(DATEADD(DAY, -20, @Today) AS datetime2(0))),
        'E20260807001',
        1,
        'Branch',
        N'1',
        N'建立台北商旅分館。'
    ),
    (
        2,
        DATEADD(MINUTE, 9 * 60 + 15, CAST(DATEADD(DAY, -20, @Today) AS datetime2(0))),
        'E20260807001',
        1,
        'Branch',
        N'2',
        N'建立台中商旅分館。'
    ),
    (
        3,
        DATEADD(MINUTE, 9 * 60 + 30, CAST(DATEADD(DAY, -20, @Today) AS datetime2(0))),
        'E20260807001',
        1,
        'Branch',
        N'3',
        N'建立高雄商旅分館。'
    ),
    (
        3,
        DATEADD(MINUTE, 13 * 60 + 20, CAST(DATEADD(DAY, -5, @Today) AS datetime2(0))),
        'E20260807001',
        4,
        'Branch',
        N'3',
        N'將高雄商旅設定為停止接受新訂房。'
    ),
    (
        1,
        DATEADD(MINUTE, 14 * 60 + 10, CAST(DATEADD(DAY, -3, @Today) AS datetime2(0))),
        'E20260807002',
        18,
        'Room',
        N'202',
        N'將台北商旅房間 202 設為保留。'
    ),
    (
        1,
        DATEADD(MINUTE, 16 * 60 + 25, CAST(DATEADD(DAY, -2, @Today) AS datetime2(0))),
        'E20260807002',
        22,
        'Booking',
        N'BK202608070003',
        N'完成訂單 BK202608070003 的 Check-in，指派房間 201。'
    ),
    (
        2,
        DATEADD(MINUTE, 10 * 60 + 35, CAST(DATEADD(DAY, -3, @Today) AS datetime2(0))),
        'E20260807004',
        23,
        'Booking',
        N'BK202608070004',
        N'完成訂單 BK202608070004 的 Check-out，房間 201 轉為待清潔。'
    ),
    (
        1,
        DATEADD(MINUTE, 10 * 60 + 30, CAST(DATEADD(DAY, -1, @Today) AS datetime2(0))),
        'E20260807002',
        21,
        'Booking',
        N'BK202608070005',
        N'因顧客因素取消訂單 BK202608070005。'
    ),
    (
        2,
        DATEADD(MINUTE, 11 * 60 + 20, CAST(@Today AS datetime2(0))),
        'E20260807004',
        21,
        'Booking',
        N'BK202608070006',
        N'因飯店確認無法依原訂單提供住宿，取消訂單 BK202608070006。'
    ),
    (
        1,
        DATEADD(MINUTE, 15 * 60 + 5, CAST(DATEADD(DAY, -1, @Today) AS datetime2(0))),
        'E20260807001',
        15,
        'Employee',
        N'E20260807003',
        N'停用台北商旅員工帳號 E20260807003。'
    );

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

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
