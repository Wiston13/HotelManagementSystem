/*
    HotelManagementSystem - 第一版開發基準資料驗證
    SQL Server（唯讀）

    前置：依序執行 01、02、03。
    本檔不修改資料，可在開發操作前後重跑比較結果。
*/

USE [HotelManagementSystem];
GO

/* sqlcmd 執行本檔時請加 -f 65001，確保中文摘要正確顯示。 */
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

SET NOCOUNT ON;
GO

DECLARE @NowTaipei datetime2(0) =
    CONVERT(datetime2(0), SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time');
DECLARE @Today date = CAST(@NowTaipei AS date);

SELECT @NowTaipei AS [TaipeiNow], @Today AS [TaipeiToday];

/* 1. 八張表資料筆數 */
SELECT N'Branches' AS [TableName], COUNT(*) AS [RowCount] FROM [dbo].[Branches]
UNION ALL SELECT N'RoomTypes', COUNT(*) FROM [dbo].[RoomTypes]
UNION ALL SELECT N'Rooms', COUNT(*) FROM [dbo].[Rooms]
UNION ALL SELECT N'Employees', COUNT(*) FROM [dbo].[Employees]
UNION ALL SELECT N'Bookings', COUNT(*) FROM [dbo].[Bookings]
UNION ALL SELECT N'StayRecords', COUNT(*) FROM [dbo].[StayRecords]
UNION ALL SELECT N'OperationTypes', COUNT(*) FROM [dbo].[OperationTypes]
UNION ALL SELECT N'OperationLogs', COUNT(*) FROM [dbo].[OperationLogs];

/* 2. 每間分館房型數（預期 3～5） */
SELECT
    B.[BranchId],
    B.[BranchName],
    B.[Region],
    B.[AcceptsNewBookings],
    COUNT(RT.[RoomTypeId]) AS [RoomTypeCount]
FROM [dbo].[Branches] AS B
LEFT JOIN [dbo].[RoomTypes] AS RT ON RT.[BranchId] = B.[BranchId]
GROUP BY B.[BranchId], B.[BranchName], B.[Region], B.[AcceptsNewBookings]
ORDER BY B.[BranchId];

/* 3. 每個房型房間數（預期 5～20） */
SELECT
    B.[BranchId],
    B.[BranchName],
    RT.[RoomTypeId],
    RT.[RoomTypeName],
    RT.[MaxOccupancy],
    RT.[NightlyPrice],
    RT.[IsActive],
    COUNT(R.[RoomId]) AS [RoomCount]
FROM [dbo].[RoomTypes] AS RT
INNER JOIN [dbo].[Branches] AS B ON B.[BranchId] = RT.[BranchId]
LEFT JOIN [dbo].[Rooms] AS R ON R.[RoomTypeId] = RT.[RoomTypeId] AND R.[BranchId] = RT.[BranchId]
GROUP BY B.[BranchId], B.[BranchName], RT.[RoomTypeId], RT.[RoomTypeName], RT.[MaxOccupancy], RT.[NightlyPrice], RT.[IsActive]
ORDER BY B.[BranchId], RT.[RoomTypeId];

/* 4. 每間分館員工數（啟用預期 2～3，另列停用） */
SELECT
    B.[BranchId],
    B.[BranchName],
    SUM(CASE WHEN E.[IsActive] = 1 THEN 1 ELSE 0 END) AS [ActiveBranchEmployees],
    SUM(CASE WHEN E.[IsActive] = 0 THEN 1 ELSE 0 END) AS [InactiveBranchEmployees]
FROM [dbo].[Branches] AS B
LEFT JOIN [dbo].[Employees] AS E
    ON E.[BranchId] = B.[BranchId] AND E.[Role] = 'BranchEmployee'
GROUP BY B.[BranchId], B.[BranchName]
ORDER BY B.[BranchId];

/* 5. 各訂單狀態數量 */
SELECT [BookingStatus], COUNT(*) AS [BookingCount]
FROM [dbo].[Bookings]
GROUP BY [BookingStatus]
ORDER BY [BookingStatus];

/* 6. 目前真正可 Check-in 的訂單：時間、狀態、住房與房間條件全部符合 */
SELECT
    B.[BookingNumber],
    BR.[BranchName],
    B.[RoomTypeNameSnapshot],
    B.[CheckInDate],
    B.[CheckOutDate],
    B.[MaxOccupancySnapshot],
    C.[EligibleRoomCount]
FROM [dbo].[Bookings] AS B
INNER JOIN [dbo].[Branches] AS BR ON BR.[BranchId] = B.[BranchId]
CROSS APPLY
(
    SELECT COUNT(*) AS [EligibleRoomCount]
    FROM [dbo].[Rooms] AS R
    WHERE R.[BranchId] = B.[BranchId]
      AND R.[RoomTypeId] = B.[RoomTypeId]
      AND R.[SupplyStatus] = 'Open'
      AND R.[CleaningStatus] = 'Clean'
      AND NOT EXISTS
          (SELECT 1 FROM [dbo].[StayRecords] AS SR WHERE SR.[RoomId] = R.[RoomId] AND SR.[ActualCheckOutAt] IS NULL)
) AS C
WHERE B.[BookingStatus] = 'Paid'
  AND NOT EXISTS (SELECT 1 FROM [dbo].[StayRecords] AS SR WHERE SR.[BookingNumber] = B.[BookingNumber])
  AND @NowTaipei >= DATEADD(HOUR, 16, CAST(B.[CheckInDate] AS datetime2(0)))
  AND @NowTaipei <  DATEADD(HOUR, 12, CAST(B.[CheckOutDate] AS datetime2(0)))
  AND C.[EligibleRoomCount] > 0
ORDER BY B.[BookingNumber];

/* 7. Check-in 邊界／阻擋矩陣 */
SELECT
    B.[BookingNumber],
    B.[BookingStatus],
    B.[CheckInDate],
    B.[CheckOutDate],
    C.[EligibleRoomCount],
    CASE
        WHEN B.[BookingStatus] <> 'Paid' THEN N'狀態不是 Paid'
        WHEN B.[StayRecordCount] > 0 THEN N'已存在 StayRecord'
        WHEN @NowTaipei < DATEADD(HOUR,16,CAST(B.[CheckInDate] AS datetime2(0))) THEN N'尚未到入住日 16:00'
        WHEN @NowTaipei >= DATEADD(HOUR,12,CAST(B.[CheckOutDate] AS datetime2(0))) THEN N'已達退房日 12:00'
        WHEN C.[EligibleRoomCount] = 0 THEN N'同館原房型沒有 Open + Clean + 無住客房間'
        ELSE N'目前可辦理 Check-in'
    END AS [CurrentResult]
FROM
(
    SELECT BK.*, (SELECT COUNT(*) FROM [dbo].[StayRecords] AS S WHERE S.[BookingNumber] = BK.[BookingNumber]) AS [StayRecordCount]
    FROM [dbo].[Bookings] AS BK
    WHERE BK.[BookingNumber] IN
        ('BK202608070001','BK202608070002','BK202608070003','BK202608070004','BK202608070005',
         'BK202608070020','BK202608070026','BK202608070027')
) AS B
CROSS APPLY
(
    SELECT COUNT(*) AS [EligibleRoomCount]
    FROM [dbo].[Rooms] AS R
    WHERE R.[BranchId] = B.[BranchId]
      AND R.[RoomTypeId] = B.[RoomTypeId]
      AND R.[SupplyStatus] = 'Open'
      AND R.[CleaningStatus] = 'Clean'
      AND NOT EXISTS
          (SELECT 1 FROM [dbo].[StayRecords] AS S WHERE S.[RoomId] = R.[RoomId] AND S.[ActualCheckOutAt] IS NULL)
) AS C
ORDER BY B.[BookingNumber];

/* 8. 可 Check-out 住房：一般、今日應退、逾期未退與可提早退房皆列出 */
SELECT
    B.[BookingNumber],
    BR.[BranchName],
    SR.[RoomNumberSnapshot],
    SR.[ActualCheckInAt],
    B.[CheckOutDate],
    CASE
        WHEN B.[CheckOutDate] < @Today THEN N'逾期未退房'
        WHEN B.[CheckOutDate] = @Today THEN N'今日應退房'
        ELSE N'可提早退房'
    END AS [CheckOutScenario]
FROM [dbo].[Bookings] AS B
INNER JOIN [dbo].[StayRecords] AS SR ON SR.[BookingNumber] = B.[BookingNumber]
INNER JOIN [dbo].[Branches] AS BR ON BR.[BranchId] = B.[BranchId]
WHERE B.[BookingStatus] = 'CheckedIn'
  AND SR.[ActualCheckOutAt] IS NULL
ORDER BY B.[CheckOutDate], B.[BookingNumber];

/* 9. 待 No-show 補判訂單：027 永遠符合，028 只在今日 12:00 後符合 */
SELECT
    B.[BookingNumber],
    BR.[BranchName],
    B.[CheckInDate],
    B.[CheckOutDate],
    DATEADD(HOUR,12,CAST(B.[CheckOutDate] AS datetime2(0))) AS [NoShowCutoff]
FROM [dbo].[Bookings] AS B
INNER JOIN [dbo].[Branches] AS BR ON BR.[BranchId] = B.[BranchId]
WHERE B.[BookingStatus] = 'Paid'
  AND NOT EXISTS (SELECT 1 FROM [dbo].[StayRecords] AS SR WHERE SR.[BookingNumber] = B.[BookingNumber])
  AND @NowTaipei >= DATEADD(HOUR,12,CAST(B.[CheckOutDate] AS datetime2(0)))
ORDER BY B.[CheckOutDate], B.[BookingNumber];

/* 10. 房間供應／清潔／推導入住組合 */
SELECT
    X.[SupplyStatus],
    X.[CleaningStatus],
    X.[DerivedOccupancy],
    COUNT(*) AS [RoomCount]
FROM
(
    SELECT
        R.[SupplyStatus],
        R.[CleaningStatus],
        CASE WHEN EXISTS
        (SELECT 1 FROM [dbo].[StayRecords] AS SR WHERE SR.[RoomId] = R.[RoomId] AND SR.[ActualCheckOutAt] IS NULL)
        THEN N'Occupied' ELSE N'Vacant' END AS [DerivedOccupancy]
    FROM [dbo].[Rooms] AS R
) AS X
GROUP BY X.[SupplyStatus], X.[CleaningStatus], X.[DerivedOccupancy]
ORDER BY X.[SupplyStatus], X.[CleaningStatus], X.[DerivedOccupancy];

/* 11. 房量重疊、相鄰與不占量狀態的固定矩陣 */
SELECT
    [BookingNumber], [BookingStatus], [CheckInDate], [CheckOutDate],
    CASE [BookingNumber]
        WHEN 'BK202608070036' THEN N'有效 Paid：與 037 重疊'
        WHEN 'BK202608070037' THEN N'有效 Paid：與 036 重疊，與 038 相鄰'
        WHEN 'BK202608070038' THEN N'有效 Paid：入住日等於 037 退房日，不重疊'
        WHEN 'BK202608070039' THEN N'Cancelled：日期重疊但不占房量'
        WHEN 'BK202608070040' THEN N'NoShow：終止狀態不占房量'
    END AS [InventoryPurpose]
FROM [dbo].[Bookings]
WHERE [BookingNumber] BETWEEN 'BK202608070036' AND 'BK202608070040'
ORDER BY [BookingNumber];

/* 12. 孤立或矛盾資料檢查：正常基準的 IssueCount 應全部為 0 */
SELECT N'訂單與房型分館不一致' AS [CheckName], COUNT(*) AS [IssueCount]
FROM [dbo].[Bookings] AS B
INNER JOIN [dbo].[RoomTypes] AS RT ON RT.[RoomTypeId] = B.[RoomTypeId]
WHERE B.[BranchId] <> RT.[BranchId]
UNION ALL
SELECT N'房間與房型分館不一致', COUNT(*)
FROM [dbo].[Rooms] AS R
INNER JOIN [dbo].[RoomTypes] AS RT ON RT.[RoomTypeId] = R.[RoomTypeId]
WHERE R.[BranchId] <> RT.[BranchId]
UNION ALL
SELECT N'住房房間與訂單原房型或分館不一致', COUNT(*)
FROM [dbo].[StayRecords] AS SR
INNER JOIN [dbo].[Bookings] AS B ON B.[BookingNumber] = SR.[BookingNumber]
INNER JOIN [dbo].[Rooms] AS R ON R.[RoomId] = SR.[RoomId]
WHERE R.[BranchId] <> B.[BranchId] OR R.[RoomTypeId] <> B.[RoomTypeId]
UNION ALL
SELECT N'一張訂單超過一筆住房', COUNT(*)
FROM (SELECT [BookingNumber] FROM [dbo].[StayRecords] GROUP BY [BookingNumber] HAVING COUNT(*) > 1) AS X
UNION ALL
SELECT N'同一房間超過一筆未退房住房', COUNT(*)
FROM (SELECT [RoomId] FROM [dbo].[StayRecords] WHERE [ActualCheckOutAt] IS NULL GROUP BY [RoomId] HAVING COUNT(*) > 1) AS X
UNION ALL
SELECT N'CheckedIn 沒有唯一有效住房', COUNT(*)
FROM [dbo].[Bookings] AS B
WHERE B.[BookingStatus] = 'CheckedIn'
  AND (SELECT COUNT(*) FROM [dbo].[StayRecords] AS SR WHERE SR.[BookingNumber] = B.[BookingNumber] AND SR.[ActualCheckOutAt] IS NULL) <> 1
UNION ALL
SELECT N'Completed 沒有唯一已退房住房', COUNT(*)
FROM [dbo].[Bookings] AS B
WHERE B.[BookingStatus] = 'Completed'
  AND (SELECT COUNT(*) FROM [dbo].[StayRecords] AS SR WHERE SR.[BookingNumber] = B.[BookingNumber] AND SR.[ActualCheckOutAt] IS NOT NULL AND SR.[CheckedOutByEmployeeNumber] IS NOT NULL) <> 1
UNION ALL
SELECT N'Paid Cancelled NoShow 存在住房', COUNT(*)
FROM [dbo].[Bookings] AS B
WHERE B.[BookingStatus] IN ('Paid','Cancelled','NoShow')
  AND EXISTS (SELECT 1 FROM [dbo].[StayRecords] AS SR WHERE SR.[BookingNumber] = B.[BookingNumber])
UNION ALL
SELECT N'取消狀態與取消欄位不一致', COUNT(*)
FROM [dbo].[Bookings] AS B
WHERE (B.[BookingStatus] = 'Cancelled' AND
       (B.[CancellationCause] IS NULL OR B.[CancellationReason] IS NULL OR
        B.[CancelledAt] IS NULL OR B.[CancelledByEmployeeNumber] IS NULL))
   OR (B.[BookingStatus] <> 'Cancelled' AND
       (B.[CancellationCause] IS NOT NULL OR B.[CancellationReason] IS NOT NULL OR
        B.[CancelledAt] IS NOT NULL OR B.[CancelledByEmployeeNumber] IS NOT NULL))
UNION ALL
SELECT N'取消辦理員工與訂單分館不一致', COUNT(*)
FROM [dbo].[Bookings] AS B
INNER JOIN [dbo].[Employees] AS E ON E.[EmployeeNumber] = B.[CancelledByEmployeeNumber]
WHERE E.[BranchId] <> B.[BranchId]
UNION ALL
SELECT N'入住或退房員工與訂單分館不一致', COUNT(*)
FROM [dbo].[StayRecords] AS SR
INNER JOIN [dbo].[Bookings] AS B ON B.[BookingNumber] = SR.[BookingNumber]
INNER JOIN [dbo].[Employees] AS EI ON EI.[EmployeeNumber] = SR.[CheckedInByEmployeeNumber]
LEFT JOIN [dbo].[Employees] AS EO ON EO.[EmployeeNumber] = SR.[CheckedOutByEmployeeNumber]
WHERE EI.[BranchId] <> B.[BranchId] OR (EO.[EmployeeNumber] IS NOT NULL AND EO.[BranchId] <> B.[BranchId])
UNION ALL
SELECT N'總額不等於每晚價格快照乘晚數', COUNT(*)
FROM [dbo].[Bookings]
WHERE [TotalAmount] <> [NightlyPriceSnapshot] * DATEDIFF(DAY,[CheckInDate],[CheckOutDate])
UNION ALL
SELECT N'實際入住人數超過容量快照', COUNT(*)
FROM [dbo].[StayRecords] AS SR
INNER JOIN [dbo].[Bookings] AS B ON B.[BookingNumber] = SR.[BookingNumber]
WHERE SR.[ActualGuestCount] > B.[MaxOccupancySnapshot]
UNION ALL
SELECT N'測資快照與目前房型或房號不一致', COUNT(*)
FROM [dbo].[StayRecords] AS SR
INNER JOIN [dbo].[Bookings] AS B ON B.[BookingNumber] = SR.[BookingNumber]
INNER JOIN [dbo].[RoomTypes] AS RT ON RT.[RoomTypeId] = B.[RoomTypeId]
INNER JOIN [dbo].[Rooms] AS R ON R.[RoomId] = SR.[RoomId]
WHERE B.[RoomTypeNameSnapshot] <> RT.[RoomTypeName]
   OR B.[MaxOccupancySnapshot] <> RT.[MaxOccupancy]
   OR B.[NightlyPriceSnapshot] <> RT.[NightlyPrice]
   OR SR.[RoomNumberSnapshot] <> R.[RoomNumber]
UNION ALL
SELECT N'DisabledReason 與供應狀態不一致', COUNT(*)
FROM [dbo].[Rooms]
WHERE ([SupplyStatus] = 'Disabled' AND ([DisabledReason] IS NULL OR LEN(LTRIM(RTRIM([DisabledReason]))) = 0))
   OR ([SupplyStatus] <> 'Disabled' AND [DisabledReason] IS NOT NULL)
UNION ALL
SELECT N'入住中房間不是 Open', COUNT(*)
FROM [dbo].[StayRecords] AS SR
INNER JOIN [dbo].[Rooms] AS R ON R.[RoomId] = SR.[RoomId]
WHERE SR.[ActualCheckOutAt] IS NULL AND R.[SupplyStatus] <> 'Open'
UNION ALL
SELECT N'分館 ImageUrl 非本機 seed 路徑', COUNT(*)
FROM [dbo].[Branches]
WHERE [ImageUrl] IS NULL OR [ImageUrl] NOT LIKE '/images/seed/branches/%'
UNION ALL
SELECT N'房型 ImageUrl 非本機 seed 路徑', COUNT(*)
FROM [dbo].[RoomTypes]
WHERE [ImageUrl] NOT LIKE '/images/seed/room-types/%';

/* 13. 規模限制檢查：正常基準應回傳 0 列 */
SELECT N'分館房型數不在 3～5' AS [ScaleIssue], B.[BranchName] AS [Target], COUNT(RT.[RoomTypeId]) AS [ActualCount]
FROM [dbo].[Branches] AS B
LEFT JOIN [dbo].[RoomTypes] AS RT ON RT.[BranchId] = B.[BranchId]
GROUP BY B.[BranchName]
HAVING COUNT(RT.[RoomTypeId]) NOT BETWEEN 3 AND 5
UNION ALL
SELECT N'房型房間數不在 5～20', CONCAT(B.[BranchName], N'／', RT.[RoomTypeName]), COUNT(R.[RoomId])
FROM [dbo].[RoomTypes] AS RT
INNER JOIN [dbo].[Branches] AS B ON B.[BranchId] = RT.[BranchId]
LEFT JOIN [dbo].[Rooms] AS R ON R.[RoomTypeId] = RT.[RoomTypeId] AND R.[BranchId] = RT.[BranchId]
GROUP BY B.[BranchName], RT.[RoomTypeName]
HAVING COUNT(R.[RoomId]) NOT BETWEEN 5 AND 20
UNION ALL
SELECT N'分館啟用員工數不在 2～3', B.[BranchName], COUNT(E.[EmployeeNumber])
FROM [dbo].[Branches] AS B
LEFT JOIN [dbo].[Employees] AS E
    ON E.[BranchId] = B.[BranchId] AND E.[Role] = 'BranchEmployee' AND E.[IsActive] = 1
GROUP BY B.[BranchName]
HAVING COUNT(E.[EmployeeNumber]) NOT BETWEEN 2 AND 3;

/* 14. Identity seed 與固定資料最大 ID */
SELECT N'Branches' AS [TableName], IDENT_CURRENT('dbo.Branches') AS [CurrentIdentity], MAX([BranchId]) AS [MaxSeedId] FROM [dbo].[Branches]
UNION ALL SELECT N'RoomTypes', IDENT_CURRENT('dbo.RoomTypes'), MAX([RoomTypeId]) FROM [dbo].[RoomTypes]
UNION ALL SELECT N'Rooms', IDENT_CURRENT('dbo.Rooms'), MAX([RoomId]) FROM [dbo].[Rooms]
UNION ALL SELECT N'StayRecords', IDENT_CURRENT('dbo.StayRecords'), MAX([StayRecordId]) FROM [dbo].[StayRecords]
UNION ALL SELECT N'OperationTypes', IDENT_CURRENT('dbo.OperationTypes'), MAX([OperationTypeId]) FROM [dbo].[OperationTypes]
UNION ALL SELECT N'OperationLogs', IDENT_CURRENT('dbo.OperationLogs'), MAX([OperationLogId]) FROM [dbo].[OperationLogs];
GO
