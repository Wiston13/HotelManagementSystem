/*
    HotelManagementSystem - 第一版開發營運量體資料
    SQL Server

    前置：先依序執行 01、02、03；本檔是可選的營運量體層。

    本檔責任：
    - 在 03 的 42 筆固定核心情境之外，建立可供查詢、分頁、匯出與統計的營運量體。
    - 固定產生 2,000 筆訂單、1,148 筆住房紀錄與 3,648 筆操作紀錄。
    - 所有資料都以集合式、可重現的公式產生，不使用 RAND() 或不穩定 Identity 編號。

    本檔專用範圍：
    - BookingNumber：BK202608078000 ～ BK202608079999
    - StayRecordId：10001 ～ 11148
    - OperationLogId：100001 ～ 103648

    可重跑方式：
    - 可在 03 之後重複執行本檔，只會刪除並重建上述專用範圍。
    - 03 會清除全部情境資料；若重跑 03，之後必須再執行本檔。
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

    DECLARE @NowTaipei datetime2(0) =
        CONVERT(datetime2(0), SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time');
    DECLARE @Today date = CAST(@NowTaipei AS date);

    /*
       只接受剛完成 03，或已完整執行過本檔的狀態。
       若有人在固定範圍外新增開發資料，先中止，避免誤把其他人的資料當成本檔內容。
    */
    IF (SELECT COUNT(*) FROM [dbo].[Branches]) <> 6
       OR (SELECT COUNT(*) FROM [dbo].[RoomTypes]) <> 24
       OR (SELECT COUNT(*) FROM [dbo].[Rooms]) <> 188
       OR (SELECT COUNT(*) FROM [dbo].[Employees]) <> 19
       OR (SELECT COUNT(*) FROM [dbo].[OperationTypes]) <> 23
       OR
       (
           SELECT COUNT(*)
           FROM [dbo].[Bookings]
           WHERE [BookingNumber] NOT BETWEEN 'BK202608078000' AND 'BK202608079999'
       ) <> 42
       OR
       (
           SELECT COUNT(*)
           FROM [dbo].[StayRecords]
           WHERE [StayRecordId] NOT BETWEEN 10001 AND 11148
       ) <> 9
       OR
       (
           SELECT COUNT(*)
           FROM [dbo].[OperationLogs]
           WHERE [OperationLogId] NOT BETWEEN 100001 AND 103648
       ) <> 37
    BEGIN
        THROW 50005, N'核心情境與 03 預期不一致；請先確認資料或重新依序執行 01、02、03。', 1;
    END;

    /* 只依本檔專用範圍清除，保留 03 的固定核心情境。 */
    DELETE FROM [dbo].[OperationLogs]
    WHERE [OperationLogId] BETWEEN 100001 AND 103648;

    DELETE FROM [dbo].[StayRecords]
    WHERE [StayRecordId] BETWEEN 10001 AND 11148;

    DELETE FROM [dbo].[Bookings]
    WHERE [BookingNumber] BETWEEN 'BK202608078000' AND 'BK202608079999';

    /* 0～1999 的 deterministic number set。 */
    DECLARE @Numbers TABLE
    (
        [SequenceNumber] int NOT NULL PRIMARY KEY
    );

    ;WITH [Digits] AS
    (
        SELECT [Digit]
        FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) AS D([Digit])
    )
    INSERT INTO @Numbers ([SequenceNumber])
    SELECT
        D3.[Digit] * 1000 + D2.[Digit] * 100 + D1.[Digit] * 10 + D0.[Digit]
    FROM [Digits] AS D0
    CROSS JOIN [Digits] AS D1
    CROSS JOIN [Digits] AS D2
    CROSS JOIN [Digits] AS D3
    WHERE D3.[Digit] * 1000 + D2.[Digit] * 100 + D1.[Digit] * 10 + D0.[Digit] <= 1999;

    DECLARE @Surnames TABLE
    (
        [NameId] tinyint NOT NULL PRIMARY KEY,
        [Surname] nvarchar(2) NOT NULL
    );

    INSERT INTO @Surnames ([NameId], [Surname])
    VALUES
    (0,N'陳'),(1,N'林'),(2,N'黃'),(3,N'張'),(4,N'李'),
    (5,N'王'),(6,N'吳'),(7,N'劉'),(8,N'蔡'),(9,N'楊'),
    (10,N'許'),(11,N'鄭'),(12,N'謝'),(13,N'洪'),(14,N'郭'),
    (15,N'邱'),(16,N'曾'),(17,N'廖'),(18,N'賴'),(19,N'徐');

    DECLARE @GivenNames TABLE
    (
        [NameId] tinyint NOT NULL PRIMARY KEY,
        [GivenName] nvarchar(4) NOT NULL
    );

    INSERT INTO @GivenNames ([NameId], [GivenName])
    VALUES
    (0,N'怡君'),(1,N'冠宇'),(2,N'雅婷'),(3,N'承翰'),(4,N'詩涵'),
    (5,N'柏勳'),(6,N'佳穎'),(7,N'志豪'),(8,N'書妍'),(9,N'家宏'),
    (10,N'佩珊'),(11,N'俊傑'),(12,N'子晴'),(13,N'宗翰'),(14,N'怡安'),
    (15,N'映辰'),(16,N'博翔'),(17,N'雅雯'),(18,N'文傑'),(19,N'郁婷');

    DECLARE @People TABLE
    (
        [SequenceNumber] int NOT NULL PRIMARY KEY,
        [BookingNumber] varchar(20) NOT NULL UNIQUE,
        [BookerName] nvarchar(50) NOT NULL,
        [ContactPhone] varchar(20) NOT NULL,
        [Email] varchar(254) NOT NULL
    );

    INSERT INTO @People
        ([SequenceNumber], [BookingNumber], [BookerName], [ContactPhone], [Email])
    SELECT
        N.[SequenceNumber],
        'BK20260807' + RIGHT('0000' + CONVERT(varchar(4), 8000 + N.[SequenceNumber]), 4),
        SN.[Surname] + GN.[GivenName],
        '09' + RIGHT('00000000' + CONVERT(varchar(8), 70000000 + N.[SequenceNumber]), 8),
        'volume' + CONVERT(varchar(4), 8000 + N.[SequenceNumber]) + '@seed.example'
    FROM @Numbers AS N
    INNER JOIN @Surnames AS SN ON SN.[NameId] = N.[SequenceNumber] % 20
    INNER JOIN @GivenNames AS GN ON GN.[NameId] = (N.[SequenceNumber] / 20) % 20;

    DECLARE @RoomCatalog TABLE
    (
        [RoomSequence] int NOT NULL PRIMARY KEY,
        [RoomId] int NOT NULL UNIQUE,
        [BranchId] int NOT NULL,
        [RoomTypeId] int NOT NULL,
        [RoomNumber] nvarchar(10) NOT NULL,
        [RoomTypeName] nvarchar(50) NOT NULL,
        [MaxOccupancy] tinyint NOT NULL,
        [NightlyPrice] decimal(10,2) NOT NULL
    );

    INSERT INTO @RoomCatalog
        ([RoomSequence], [RoomId], [BranchId], [RoomTypeId], [RoomNumber],
         [RoomTypeName], [MaxOccupancy], [NightlyPrice])
    SELECT
        ROW_NUMBER() OVER (ORDER BY R.[RoomId]),
        R.[RoomId], R.[BranchId], R.[RoomTypeId], R.[RoomNumber],
        RT.[RoomTypeName], RT.[MaxOccupancy], RT.[NightlyPrice]
    FROM [dbo].[Rooms] AS R
    INNER JOIN [dbo].[RoomTypes] AS RT
        ON RT.[RoomTypeId] = R.[RoomTypeId] AND RT.[BranchId] = R.[BranchId];

    DECLARE @RoomTypeCatalog TABLE
    (
        [RoomTypeSequence] int NOT NULL PRIMARY KEY,
        [BranchId] int NOT NULL,
        [RoomTypeId] int NOT NULL,
        [RoomTypeName] nvarchar(50) NOT NULL,
        [MaxOccupancy] tinyint NOT NULL,
        [NightlyPrice] decimal(10,2) NOT NULL
    );

    INSERT INTO @RoomTypeCatalog
        ([RoomTypeSequence], [BranchId], [RoomTypeId], [RoomTypeName], [MaxOccupancy], [NightlyPrice])
    SELECT
        ROW_NUMBER() OVER (ORDER BY RT.[BranchId], RT.[RoomTypeId]),
        RT.[BranchId], RT.[RoomTypeId], RT.[RoomTypeName], RT.[MaxOccupancy], RT.[NightlyPrice]
    FROM [dbo].[RoomTypes] AS RT;

    DECLARE @FutureRoomTypeCatalog TABLE
    (
        [RoomTypeSequence] int NOT NULL PRIMARY KEY,
        [BranchId] int NOT NULL,
        [RoomTypeId] int NOT NULL,
        [RoomTypeName] nvarchar(50) NOT NULL,
        [MaxOccupancy] tinyint NOT NULL,
        [NightlyPrice] decimal(10,2) NOT NULL
    );

    INSERT INTO @FutureRoomTypeCatalog
        ([RoomTypeSequence], [BranchId], [RoomTypeId], [RoomTypeName], [MaxOccupancy], [NightlyPrice])
    SELECT
        ROW_NUMBER() OVER (ORDER BY RT.[BranchId], RT.[RoomTypeId]),
        RT.[BranchId], RT.[RoomTypeId], RT.[RoomTypeName], RT.[MaxOccupancy], RT.[NightlyPrice]
    FROM [dbo].[RoomTypes] AS RT
    INNER JOIN [dbo].[Branches] AS B ON B.[BranchId] = RT.[BranchId]
    WHERE RT.[IsActive] = 1 AND B.[AcceptsNewBookings] = 1;

    DECLARE @FutureRoomTypeCount int = (SELECT COUNT(*) FROM @FutureRoomTypeCatalog);

    DECLARE @BranchEmployees TABLE
    (
        [BranchId] int NOT NULL,
        [EmployeeSlot] int NOT NULL,
        [EmployeeCount] int NOT NULL,
        [EmployeeNumber] varchar(20) NOT NULL,
        PRIMARY KEY ([BranchId], [EmployeeSlot])
    );

    INSERT INTO @BranchEmployees
        ([BranchId], [EmployeeSlot], [EmployeeCount], [EmployeeNumber])
    SELECT
        E.[BranchId],
        ROW_NUMBER() OVER (PARTITION BY E.[BranchId] ORDER BY E.[EmployeeNumber]),
        COUNT(*) OVER (PARTITION BY E.[BranchId]),
        E.[EmployeeNumber]
    FROM [dbo].[Employees] AS E
    WHERE E.[Role] = 'BranchEmployee' AND E.[IsActive] = 1;

    DECLARE @ActiveRooms TABLE
    (
        [ActiveSequence] int NOT NULL PRIMARY KEY,
        [RoomId] int NOT NULL UNIQUE,
        [BranchId] int NOT NULL,
        [RoomTypeId] int NOT NULL,
        [RoomNumber] nvarchar(10) NOT NULL,
        [RoomTypeName] nvarchar(50) NOT NULL,
        [MaxOccupancy] tinyint NOT NULL,
        [NightlyPrice] decimal(10,2) NOT NULL
    );

    ;WITH [Eligible] AS
    (
        SELECT
            R.[RoomId], R.[BranchId], R.[RoomTypeId], R.[RoomNumber],
            RT.[RoomTypeName], RT.[MaxOccupancy], RT.[NightlyPrice],
            ROW_NUMBER() OVER
            (
                PARTITION BY R.[BranchId], R.[RoomTypeId]
                ORDER BY R.[RoomId]
            ) AS [RoomTypeRoomSequence]
        FROM [dbo].[Rooms] AS R
        INNER JOIN [dbo].[RoomTypes] AS RT
            ON RT.[RoomTypeId] = R.[RoomTypeId] AND RT.[BranchId] = R.[BranchId]
        WHERE R.[SupplyStatus] = 'Open'
          AND R.[CleaningStatus] = 'Clean'
          AND NOT EXISTS
          (
              SELECT 1
              FROM [dbo].[StayRecords] AS SR
              WHERE SR.[RoomId] = R.[RoomId] AND SR.[ActualCheckOutAt] IS NULL
          )
          /* 保留 03 的固定 Check-in 與無房可指派案例。 */
          AND NOT (R.[BranchId] = 1 AND R.[RoomTypeId] IN (2,4,5))
    ),
    [RankedByBranch] AS
    (
        SELECT
            E.*,
            ROW_NUMBER() OVER
            (
                PARTITION BY E.[BranchId]
                ORDER BY E.[RoomTypeRoomSequence], E.[RoomTypeId], E.[RoomId]
            ) AS [BranchRoomSequence]
        FROM [Eligible] AS E
    )
    INSERT INTO @ActiveRooms
        ([ActiveSequence], [RoomId], [BranchId], [RoomTypeId], [RoomNumber],
         [RoomTypeName], [MaxOccupancy], [NightlyPrice])
    SELECT
        ROW_NUMBER() OVER (ORDER BY RB.[BranchId], RB.[BranchRoomSequence]),
        RB.[RoomId], RB.[BranchId], RB.[RoomTypeId], RB.[RoomNumber],
        RB.[RoomTypeName], RB.[MaxOccupancy], RB.[NightlyPrice]
    FROM [RankedByBranch] AS RB
    WHERE RB.[BranchRoomSequence] <= 8;

    IF (SELECT COUNT(*) FROM @Numbers) <> 2000
       OR (SELECT COUNT(*) FROM @RoomCatalog) <> 188
       OR (SELECT COUNT(*) FROM @RoomTypeCatalog) <> 24
       OR @FutureRoomTypeCount <> 20
       OR (SELECT COUNT(*) FROM @ActiveRooms) <> 48
    BEGIN
        THROW 50006, N'量體資料的 number set、房型或可指派入住中房間數不符合預期。', 1;
    END;

    DECLARE @VolumeBookings TABLE
    (
        [SequenceNumber] int NOT NULL PRIMARY KEY,
        [BookingNumber] varchar(20) NOT NULL UNIQUE,
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
        [CancelledByEmployeeNumber] varchar(20) NULL,
        [RoomId] int NULL,
        [RoomNumberSnapshot] nvarchar(10) NULL,
        [ActualCheckInAt] datetime2(0) NULL,
        [ActualCheckOutAt] datetime2(0) NULL,
        [ActualGuestCount] tinyint NULL,
        [CheckedInByEmployeeNumber] varchar(20) NULL,
        [CheckedOutByEmployeeNumber] varchar(20) NULL
    );

    /* 1,100 筆已完成：分布於過去約 19 個月，同房間各批次相隔 90 天。 */
    INSERT INTO @VolumeBookings
    (
        [SequenceNumber], [BookingNumber], [BranchId], [RoomTypeId], [BookerName],
        [ContactPhone], [Email], [CheckInDate], [CheckOutDate], [RoomTypeNameSnapshot],
        [MaxOccupancySnapshot], [NightlyPriceSnapshot], [TotalAmount], [BookingStatus],
        [CreatedAt], [CancellationCause], [CancellationReason], [CancelledAt],
        [CancelledByEmployeeNumber], [RoomId], [RoomNumberSnapshot], [ActualCheckInAt],
        [ActualCheckOutAt], [ActualGuestCount], [CheckedInByEmployeeNumber],
        [CheckedOutByEmployeeNumber]
    )
    SELECT
        N.[SequenceNumber], P.[BookingNumber], RC.[BranchId], RC.[RoomTypeId], P.[BookerName],
        P.[ContactPhone], P.[Email], D.[CheckInDate], O.[CheckOutDate], RC.[RoomTypeName],
        RC.[MaxOccupancy], RC.[NightlyPrice],
        RC.[NightlyPrice] * (1 + N.[SequenceNumber] % 4), 'Completed',
        DATEADD(MINUTE, 480 + N.[SequenceNumber] % 541,
            CAST(DATEADD(DAY, -(7 + N.[SequenceNumber] % 45), D.[CheckInDate]) AS datetime2(0))),
        NULL, NULL, NULL, NULL,
        RC.[RoomId], RC.[RoomNumber],
        DATEADD(MINUTE, 960 + N.[SequenceNumber] % 91, CAST(D.[CheckInDate] AS datetime2(0))),
        DATEADD(MINUTE, 570 + N.[SequenceNumber] % 121, CAST(O.[CheckOutDate] AS datetime2(0))),
        1 + N.[SequenceNumber] % RC.[MaxOccupancy],
        EIN.[EmployeeNumber], EOUT.[EmployeeNumber]
    FROM @Numbers AS N
    INNER JOIN @People AS P ON P.[SequenceNumber] = N.[SequenceNumber]
    INNER JOIN @RoomCatalog AS RC ON RC.[RoomSequence] = 1 + N.[SequenceNumber] % 188
    CROSS APPLY
    (
        VALUES
        (
            DATEADD
            (
                DAY,
                -570 + (N.[SequenceNumber] / 188) * 90 + (RC.[RoomSequence] * 37) % 90,
                @Today
            )
        )
    ) AS D([CheckInDate])
    CROSS APPLY
    (
        VALUES (DATEADD(DAY, 1 + N.[SequenceNumber] % 4, D.[CheckInDate]))
    ) AS O([CheckOutDate])
    INNER JOIN @BranchEmployees AS EIN
        ON EIN.[BranchId] = RC.[BranchId]
       AND EIN.[EmployeeSlot] = 1 + N.[SequenceNumber] % EIN.[EmployeeCount]
    INNER JOIN @BranchEmployees AS EOUT
        ON EOUT.[BranchId] = RC.[BranchId]
       AND EOUT.[EmployeeSlot] = 1 + (N.[SequenceNumber] + 1) % EOUT.[EmployeeCount]
    WHERE N.[SequenceNumber] BETWEEN 0 AND 1099;

    /*
       300 筆已取消：顧客因素與飯店因素各半。
       全部使用已發生的歷史入住日；飯店因素同時覆蓋入住日前與同日 16:00 後取消。
    */
    INSERT INTO @VolumeBookings
    (
        [SequenceNumber], [BookingNumber], [BranchId], [RoomTypeId], [BookerName],
        [ContactPhone], [Email], [CheckInDate], [CheckOutDate], [RoomTypeNameSnapshot],
        [MaxOccupancySnapshot], [NightlyPriceSnapshot], [TotalAmount], [BookingStatus],
        [CreatedAt], [CancellationCause], [CancellationReason], [CancelledAt],
        [CancelledByEmployeeNumber], [RoomId], [RoomNumberSnapshot], [ActualCheckInAt],
        [ActualCheckOutAt], [ActualGuestCount], [CheckedInByEmployeeNumber],
        [CheckedOutByEmployeeNumber]
    )
    SELECT
        N.[SequenceNumber], P.[BookingNumber], RT.[BranchId], RT.[RoomTypeId], P.[BookerName],
        P.[ContactPhone], P.[Email], D.[CheckInDate], O.[CheckOutDate], RT.[RoomTypeName],
        RT.[MaxOccupancy], RT.[NightlyPrice],
        RT.[NightlyPrice] * (1 + N.[SequenceNumber] % 4), 'Cancelled',
        DATEADD(MINUTE, 510 + N.[SequenceNumber] % 481,
            CAST(DATEADD(DAY, -(8 + N.[SequenceNumber] % 45), D.[CheckInDate]) AS datetime2(0))),
        CASE WHEN N.[SequenceNumber] % 2 = 0 THEN 'GuestRequest' ELSE 'HotelUnableToFulfill' END,
        CASE
            WHEN N.[SequenceNumber] % 2 = 0
                THEN N'顧客行程調整，分館完成身分與訂單資料核對後於期限內取消。'
            WHEN N.[SequenceNumber] % 4 = 1
                THEN N'合法入住時間確認原房型無法履約，以飯店因素取消。'
            ELSE N'分館於入住日前確認原房型無法履約，完成聯絡後取消。'
        END,
        CASE
            WHEN N.[SequenceNumber] % 2 = 0
                THEN DATEADD(MINUTE, 600 + N.[SequenceNumber] % 361,
                    CAST(DATEADD(DAY, -(1 + N.[SequenceNumber] % 5), D.[CheckInDate]) AS datetime2(0)))
            WHEN N.[SequenceNumber] % 4 = 1
                THEN DATEADD(MINUTE, 960 + N.[SequenceNumber] % 120,
                    CAST(D.[CheckInDate] AS datetime2(0)))
            ELSE DATEADD(MINUTE, 720 + N.[SequenceNumber] % 241,
                    CAST(DATEADD(DAY, -(1 + N.[SequenceNumber] % 3), D.[CheckInDate]) AS datetime2(0)))
        END,
        ECANCEL.[EmployeeNumber],
        NULL, NULL, NULL, NULL, NULL, NULL, NULL
    FROM @Numbers AS N
    INNER JOIN @People AS P ON P.[SequenceNumber] = N.[SequenceNumber]
    INNER JOIN @RoomTypeCatalog AS RT ON RT.[RoomTypeSequence] = 1 + N.[SequenceNumber] % 24
    CROSS APPLY
    (
        VALUES (DATEADD(DAY, -(1 + ((N.[SequenceNumber] - 1100) * 7) % 360), @Today))
    ) AS D([CheckInDate])
    CROSS APPLY
    (
        VALUES (DATEADD(DAY, 1 + N.[SequenceNumber] % 4, D.[CheckInDate]))
    ) AS O([CheckOutDate])
    INNER JOIN @BranchEmployees AS ECANCEL
        ON ECANCEL.[BranchId] = RT.[BranchId]
       AND ECANCEL.[EmployeeSlot] = 1 + N.[SequenceNumber] % ECANCEL.[EmployeeCount]
    WHERE N.[SequenceNumber] BETWEEN 1100 AND 1399;

    /* 120 筆歷史 NoShow：沒有 StayRecord，也不建立員工操作紀錄。 */
    INSERT INTO @VolumeBookings
    (
        [SequenceNumber], [BookingNumber], [BranchId], [RoomTypeId], [BookerName],
        [ContactPhone], [Email], [CheckInDate], [CheckOutDate], [RoomTypeNameSnapshot],
        [MaxOccupancySnapshot], [NightlyPriceSnapshot], [TotalAmount], [BookingStatus],
        [CreatedAt], [CancellationCause], [CancellationReason], [CancelledAt],
        [CancelledByEmployeeNumber], [RoomId], [RoomNumberSnapshot], [ActualCheckInAt],
        [ActualCheckOutAt], [ActualGuestCount], [CheckedInByEmployeeNumber],
        [CheckedOutByEmployeeNumber]
    )
    SELECT
        N.[SequenceNumber], P.[BookingNumber], RT.[BranchId], RT.[RoomTypeId], P.[BookerName],
        P.[ContactPhone], P.[Email], D.[CheckInDate], O.[CheckOutDate], RT.[RoomTypeName],
        RT.[MaxOccupancy], RT.[NightlyPrice],
        RT.[NightlyPrice] * (1 + N.[SequenceNumber] % 4), 'NoShow',
        DATEADD(MINUTE, 480 + N.[SequenceNumber] % 541,
            CAST(DATEADD(DAY, -(5 + N.[SequenceNumber] % 31), D.[CheckInDate]) AS datetime2(0))),
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, NULL, NULL, NULL
    FROM @Numbers AS N
    INNER JOIN @People AS P ON P.[SequenceNumber] = N.[SequenceNumber]
    INNER JOIN @RoomTypeCatalog AS RT ON RT.[RoomTypeSequence] = 1 + N.[SequenceNumber] % 24
    CROSS APPLY
    (
        VALUES (DATEADD(DAY, -540 + (N.[SequenceNumber] * 11) % 450, @Today))
    ) AS D([CheckInDate])
    CROSS APPLY
    (
        VALUES (DATEADD(DAY, 1 + N.[SequenceNumber] % 4, D.[CheckInDate]))
    ) AS O([CheckOutDate])
    WHERE N.[SequenceNumber] BETWEEN 1400 AND 1519;

    /* 48 筆目前入住中：每館 8 間，房間皆為 Open + Clean 且沒有其他有效住房。 */
    INSERT INTO @VolumeBookings
    (
        [SequenceNumber], [BookingNumber], [BranchId], [RoomTypeId], [BookerName],
        [ContactPhone], [Email], [CheckInDate], [CheckOutDate], [RoomTypeNameSnapshot],
        [MaxOccupancySnapshot], [NightlyPriceSnapshot], [TotalAmount], [BookingStatus],
        [CreatedAt], [CancellationCause], [CancellationReason], [CancelledAt],
        [CancelledByEmployeeNumber], [RoomId], [RoomNumberSnapshot], [ActualCheckInAt],
        [ActualCheckOutAt], [ActualGuestCount], [CheckedInByEmployeeNumber],
        [CheckedOutByEmployeeNumber]
    )
    SELECT
        N.[SequenceNumber], P.[BookingNumber], AR.[BranchId], AR.[RoomTypeId], P.[BookerName],
        P.[ContactPhone], P.[Email], D.[CheckInDate], O.[CheckOutDate], AR.[RoomTypeName],
        AR.[MaxOccupancy], AR.[NightlyPrice],
        AR.[NightlyPrice] * DATEDIFF(DAY, D.[CheckInDate], O.[CheckOutDate]), 'CheckedIn',
        DATEADD(MINUTE, 525 + AR.[ActiveSequence] % 451,
            CAST(DATEADD(DAY, -(7 + AR.[ActiveSequence] % 14), D.[CheckInDate]) AS datetime2(0))),
        NULL, NULL, NULL, NULL,
        AR.[RoomId], AR.[RoomNumber],
        DATEADD(MINUTE, 960 + AR.[ActiveSequence] % 91, CAST(D.[CheckInDate] AS datetime2(0))),
        NULL,
        1 + AR.[ActiveSequence] % AR.[MaxOccupancy],
        EIN.[EmployeeNumber], NULL
    FROM @Numbers AS N
    INNER JOIN @People AS P ON P.[SequenceNumber] = N.[SequenceNumber]
    INNER JOIN @ActiveRooms AS AR ON AR.[ActiveSequence] = N.[SequenceNumber] - 1519
    CROSS APPLY
    (
        VALUES
        (
            CASE WHEN (AR.[ActiveSequence] - 1) % 8 < 2
                 THEN DATEADD(DAY, -(4 + AR.[ActiveSequence] % 3), @Today)
                 ELSE DATEADD(DAY, -(1 + AR.[ActiveSequence] % 3), @Today) END
        )
    ) AS D([CheckInDate])
    CROSS APPLY
    (
        VALUES
        (
            CASE WHEN (AR.[ActiveSequence] - 1) % 8 < 2
                 THEN DATEADD(DAY, -1, @Today)
                 ELSE DATEADD(DAY, 1 + AR.[ActiveSequence] % 3, @Today) END
        )
    ) AS O([CheckOutDate])
    INNER JOIN @BranchEmployees AS EIN
        ON EIN.[BranchId] = AR.[BranchId]
       AND EIN.[EmployeeSlot] = 1 + AR.[ActiveSequence] % EIN.[EmployeeCount]
    WHERE N.[SequenceNumber] BETWEEN 1520 AND 1567;

    /*
       432 筆 Paid：只使用目前開放新訂房分館的啟用房型。
       入住與退房日均落在 Today～Today + 60，並保留 1～4 晚與跨月分布。
    */
    INSERT INTO @VolumeBookings
    (
        [SequenceNumber], [BookingNumber], [BranchId], [RoomTypeId], [BookerName],
        [ContactPhone], [Email], [CheckInDate], [CheckOutDate], [RoomTypeNameSnapshot],
        [MaxOccupancySnapshot], [NightlyPriceSnapshot], [TotalAmount], [BookingStatus],
        [CreatedAt], [CancellationCause], [CancellationReason], [CancelledAt],
        [CancelledByEmployeeNumber], [RoomId], [RoomNumberSnapshot], [ActualCheckInAt],
        [ActualCheckOutAt], [ActualGuestCount], [CheckedInByEmployeeNumber],
        [CheckedOutByEmployeeNumber]
    )
    SELECT
        N.[SequenceNumber], P.[BookingNumber], RT.[BranchId], RT.[RoomTypeId], P.[BookerName],
        P.[ContactPhone], P.[Email], D.[CheckInDate], O.[CheckOutDate], RT.[RoomTypeName],
        RT.[MaxOccupancy], RT.[NightlyPrice],
        RT.[NightlyPrice] * (1 + N.[SequenceNumber] % 4), 'Paid',
        DATEADD(MINUTE, 480 + N.[SequenceNumber] % 541,
            CAST(DATEADD(DAY, -(1 + N.[SequenceNumber] % 30), @Today) AS datetime2(0))),
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, NULL, NULL, NULL
    FROM @Numbers AS N
    INNER JOIN @People AS P ON P.[SequenceNumber] = N.[SequenceNumber]
    INNER JOIN @FutureRoomTypeCatalog AS RT
        ON RT.[RoomTypeSequence] = 1 + N.[SequenceNumber] % @FutureRoomTypeCount
    CROSS APPLY
    (
        VALUES (1 + N.[SequenceNumber] % 4)
    ) AS S([StayNights])
    CROSS APPLY
    (
        VALUES
        (
            DATEADD
            (
                DAY,
                CASE
                    /*
                       古都雙人房有 4 間房被逾期未退房住房繼續占用，
                       剩餘 1 間可用容量；其 Paid 訂單以不重疊的 2 晚區間分布。
                    */
                    WHEN RT.[RoomTypeId] = 14
                        THEN 5 + ((N.[SequenceNumber] - 1568) / @FutureRoomTypeCount) * 2
                    ELSE
                        (
                            ((N.[SequenceNumber] - 1568) / @FutureRoomTypeCount) * 13
                            + RT.[RoomTypeSequence] * 7
                        ) % (61 - S.[StayNights])
                END,
                @Today
            )
        )
    ) AS D([CheckInDate])
    CROSS APPLY
    (
        VALUES (DATEADD(DAY, S.[StayNights], D.[CheckInDate]))
    ) AS O([CheckOutDate])
    WHERE N.[SequenceNumber] BETWEEN 1568 AND 1999;

    IF (SELECT COUNT(*) FROM @VolumeBookings) <> 2000
       OR (SELECT COUNT(*) FROM @VolumeBookings WHERE [BookingStatus] = 'Completed') <> 1100
       OR (SELECT COUNT(*) FROM @VolumeBookings WHERE [BookingStatus] = 'Cancelled') <> 300
       OR (SELECT COUNT(*) FROM @VolumeBookings WHERE [BookingStatus] = 'NoShow') <> 120
       OR (SELECT COUNT(*) FROM @VolumeBookings WHERE [BookingStatus] = 'CheckedIn') <> 48
       OR (SELECT COUNT(*) FROM @VolumeBookings WHERE [BookingStatus] = 'Paid') <> 432
    BEGIN
        THROW 50007, N'量體訂單 staging 筆數或狀態比例不符合預期。', 1;
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
    FROM @VolumeBookings;

    SET @IdentityInsertTable = N'dbo.StayRecords';
    SET IDENTITY_INSERT [dbo].[StayRecords] ON;

    INSERT INTO [dbo].[StayRecords]
    (
        [StayRecordId], [BookingNumber], [RoomId], [RoomNumberSnapshot],
        [ActualCheckInAt], [ActualCheckOutAt], [PrimaryGuestName], [ActualGuestCount],
        [CheckedInByEmployeeNumber], [CheckedOutByEmployeeNumber]
    )
    SELECT
        CASE WHEN VB.[BookingStatus] = 'Completed'
             THEN 10001 + VB.[SequenceNumber]
             ELSE 11101 + (VB.[SequenceNumber] - 1520) END,
        VB.[BookingNumber], VB.[RoomId], VB.[RoomNumberSnapshot],
        VB.[ActualCheckInAt], VB.[ActualCheckOutAt], VB.[BookerName], VB.[ActualGuestCount],
        VB.[CheckedInByEmployeeNumber], VB.[CheckedOutByEmployeeNumber]
    FROM @VolumeBookings AS VB
    WHERE VB.[BookingStatus] IN ('Completed', 'CheckedIn');

    SET IDENTITY_INSERT [dbo].[StayRecords] OFF;
    SET @IdentityInsertTable = NULL;

    SET @IdentityInsertTable = N'dbo.OperationLogs';
    SET IDENTITY_INSERT [dbo].[OperationLogs] ON;

    /* Completed：Check-in、Check-out、清潔完成各一筆。 */
    INSERT INTO [dbo].[OperationLogs]
    (
        [OperationLogId], [TargetBranchId], [OperatedAt], [OperatorEmployeeNumber],
        [OperationTypeId], [TargetType], [TargetIdentifier], [Description]
    )
    SELECT
        100001 + VB.[SequenceNumber], VB.[BranchId], VB.[ActualCheckInAt],
        VB.[CheckedInByEmployeeNumber], 22, 'Booking', VB.[BookingNumber],
        N'完成 Check-in，指派房間 ' + VB.[RoomNumberSnapshot] + N'。'
    FROM @VolumeBookings AS VB
    WHERE VB.[BookingStatus] = 'Completed'
    UNION ALL
    SELECT
        101101 + VB.[SequenceNumber], VB.[BranchId], VB.[ActualCheckOutAt],
        VB.[CheckedOutByEmployeeNumber], 23, 'Booking', VB.[BookingNumber],
        N'完成 Check-out，房間 ' + VB.[RoomNumberSnapshot] + N' 轉為待清潔。'
    FROM @VolumeBookings AS VB
    WHERE VB.[BookingStatus] = 'Completed'
    UNION ALL
    SELECT
        102201 + VB.[SequenceNumber], VB.[BranchId], DATEADD(HOUR, 2, VB.[ActualCheckOutAt]),
        VB.[CheckedOutByEmployeeNumber], 20, 'Room', VB.[RoomNumberSnapshot],
        N'歷史住房退房後完成清潔，房間 ' + VB.[RoomNumberSnapshot] + N' 改為已清潔。'
    FROM @VolumeBookings AS VB
    WHERE VB.[BookingStatus] = 'Completed';

    /* CheckedIn：目前只有成功 Check-in 紀錄。 */
    INSERT INTO [dbo].[OperationLogs]
    (
        [OperationLogId], [TargetBranchId], [OperatedAt], [OperatorEmployeeNumber],
        [OperationTypeId], [TargetType], [TargetIdentifier], [Description]
    )
    SELECT
        103301 + (VB.[SequenceNumber] - 1520), VB.[BranchId], VB.[ActualCheckInAt],
        VB.[CheckedInByEmployeeNumber], 22, 'Booking', VB.[BookingNumber],
        N'完成 Check-in，指派房間 ' + VB.[RoomNumberSnapshot] + N'。'
    FROM @VolumeBookings AS VB
    WHERE VB.[BookingStatus] = 'CheckedIn';

    /* Cancelled：每張由同分館啟用員工建立一筆成功取消操作。 */
    INSERT INTO [dbo].[OperationLogs]
    (
        [OperationLogId], [TargetBranchId], [OperatedAt], [OperatorEmployeeNumber],
        [OperationTypeId], [TargetType], [TargetIdentifier], [Description]
    )
    SELECT
        103349 + (VB.[SequenceNumber] - 1100), VB.[BranchId], VB.[CancelledAt],
        VB.[CancelledByEmployeeNumber], 21, 'Booking', VB.[BookingNumber],
        CASE WHEN VB.[CancellationCause] = 'GuestRequest'
             THEN N'因顧客因素取消訂單 ' + VB.[BookingNumber] + N'。'
             ELSE N'因飯店因素取消訂單 ' + VB.[BookingNumber] + N'。' END
    FROM @VolumeBookings AS VB
    WHERE VB.[BookingStatus] = 'Cancelled';

    SET IDENTITY_INSERT [dbo].[OperationLogs] OFF;
    SET @IdentityInsertTable = NULL;

    DBCC CHECKIDENT ('dbo.StayRecords',   RESEED, 11148)  WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.OperationLogs', RESEED, 103648) WITH NO_INFOMSGS;

    IF
    (
        SELECT COUNT(*)
        FROM [dbo].[Bookings]
        WHERE [BookingNumber] BETWEEN 'BK202608078000' AND 'BK202608079999'
    ) <> 2000
       OR (SELECT COUNT(*) FROM [dbo].[StayRecords] WHERE [StayRecordId] BETWEEN 10001 AND 11148) <> 1148
       OR (SELECT COUNT(*) FROM [dbo].[OperationLogs] WHERE [OperationLogId] BETWEEN 100001 AND 103648) <> 3648
    BEGIN
        THROW 50008, N'量體資料寫入筆數不符合預期。', 1;
    END;

    COMMIT TRANSACTION;

    SELECT
        N'營運量體資料完成' AS [Result],
        @NowTaipei AS [TaipeiNow],
        2000 AS [VolumeBookings],
        1148 AS [VolumeStayRecords],
        3648 AS [VolumeOperationLogs];

    SELECT [BookingStatus], COUNT(*) AS [BookingCount]
    FROM [dbo].[Bookings]
    WHERE [BookingNumber] BETWEEN 'BK202608078000' AND 'BK202608079999'
    GROUP BY [BookingStatus]
    ORDER BY [BookingStatus];
END TRY
BEGIN CATCH
    IF @IdentityInsertTable = N'dbo.StayRecords'
        SET IDENTITY_INSERT [dbo].[StayRecords] OFF;
    ELSE IF @IdentityInsertTable = N'dbo.OperationLogs'
        SET IDENTITY_INSERT [dbo].[OperationLogs] OFF;

    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
