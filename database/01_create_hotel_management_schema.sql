/*
    HotelManagementSystem - 第一版完整資料庫 DDL
    SQL Server

    注意：
    1. 此腳本為開發／測試用可重建版本。
    2. 若資料表已存在，會依相依順序 DROP 後重新建立，因此原資料會被刪除。
    3. OperationTypes 的固定類型與其他測試資料請放在 SampleData 腳本中。
    4. 第一版所有業務日期與時間以台灣時間（Asia/Taipei）為準。
*/

USE [master];
GO

IF DB_ID(N'HotelManagementSystem') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [HotelManagementSystem]');
END;
GO

USE [HotelManagementSystem];
GO

/*
   Filtered index（UX_StayRecords_ActiveRoom）在 sqlcmd／SSMS
   不同連線預設下都需要一致的 SET 選項；這些設定不改變 Schema。
*/
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

/* =========================================================
   重新建立資料表：先依 FK 相依順序刪除
   ========================================================= */
DROP TABLE IF EXISTS [dbo].[OperationLogs];
DROP TABLE IF EXISTS [dbo].[StayRecords];
DROP TABLE IF EXISTS [dbo].[Bookings];
DROP TABLE IF EXISTS [dbo].[Rooms];
DROP TABLE IF EXISTS [dbo].[Employees];
DROP TABLE IF EXISTS [dbo].[OperationTypes];
DROP TABLE IF EXISTS [dbo].[RoomTypes];
DROP TABLE IF EXISTS [dbo].[Branches];
GO

/* =========================================================
   1. Branches 分館
   ========================================================= */
CREATE TABLE [dbo].[Branches]
(
    [BranchId]              int IDENTITY(1,1) NOT NULL,
    [BranchName]            nvarchar(50) NOT NULL,
    [Phone]                 varchar(20) NOT NULL,
    [Address]               nvarchar(200) NOT NULL,
    [Description]           nvarchar(max) NULL,
    [AcceptsNewBookings]    bit NOT NULL
        CONSTRAINT [DF_Branches_AcceptsNewBookings] DEFAULT (1),
    [Region]                nvarchar(50) NULL,
    [ImageUrl]              nvarchar(2048) NULL,

    CONSTRAINT [PK_Branches]
        PRIMARY KEY ([BranchId]),

    CONSTRAINT [UQ_Branches_BranchName]
        UNIQUE ([BranchName])
);
GO

/* =========================================================
   2. RoomTypes 房型
   ========================================================= */
CREATE TABLE [dbo].[RoomTypes]
(
    [RoomTypeId]        int IDENTITY(1,1) NOT NULL,
    [BranchId]          int NOT NULL,
    [RoomTypeName]      nvarchar(50) NOT NULL,
    [MaxOccupancy]      tinyint NOT NULL,
    [BedType]           nvarchar(50) NOT NULL,
    [NightlyPrice]      decimal(10,2) NOT NULL,
    [IsActive]          bit NOT NULL
        CONSTRAINT [DF_RoomTypes_IsActive] DEFAULT (1),
    [Description]       nvarchar(500) NULL,
    [ImageUrl]          nvarchar(2048) NOT NULL,

    CONSTRAINT [PK_RoomTypes]
        PRIMARY KEY ([RoomTypeId]),

    CONSTRAINT [FK_RoomTypes_Branches]
        FOREIGN KEY ([BranchId])
        REFERENCES [dbo].[Branches] ([BranchId]),

    CONSTRAINT [UQ_RoomTypes_BranchId_RoomTypeName]
        UNIQUE ([BranchId], [RoomTypeName]),

    /* 供 Rooms、Bookings 以複合 FK 保證房型與分館一致 */
    CONSTRAINT [UQ_RoomTypes_RoomTypeId_BranchId]
        UNIQUE ([RoomTypeId], [BranchId]),

    CONSTRAINT [CK_RoomTypes_MaxOccupancy]
        CHECK ([MaxOccupancy] > 0),

    CONSTRAINT [CK_RoomTypes_NightlyPrice]
        CHECK ([NightlyPrice] > 0)
);
GO

/* =========================================================
   3. Rooms 實體房間
   ========================================================= */
CREATE TABLE [dbo].[Rooms]
(
    [RoomId]            int IDENTITY(1,1) NOT NULL,
    [BranchId]          int NOT NULL,
    [RoomTypeId]        int NOT NULL,
    [RoomNumber]        nvarchar(10) NOT NULL,
    [Floor]             smallint NOT NULL,
    [SupplyStatus]      varchar(20) NOT NULL
        CONSTRAINT [DF_Rooms_SupplyStatus] DEFAULT ('Open'),
    [CleaningStatus]    varchar(20) NOT NULL
        CONSTRAINT [DF_Rooms_CleaningStatus] DEFAULT ('Clean'),
    [DisabledReason]    nvarchar(200) NULL,

    CONSTRAINT [PK_Rooms]
        PRIMARY KEY ([RoomId]),

    CONSTRAINT [FK_Rooms_RoomTypes]
        FOREIGN KEY ([RoomTypeId], [BranchId])
        REFERENCES [dbo].[RoomTypes] ([RoomTypeId], [BranchId]),

    CONSTRAINT [UQ_Rooms_BranchId_RoomNumber]
        UNIQUE ([BranchId], [RoomNumber]),

    CONSTRAINT [CK_Rooms_SupplyStatus]
        CHECK ([SupplyStatus] IN ('Open', 'Reserved', 'Disabled')),

    CONSTRAINT [CK_Rooms_CleaningStatus]
        CHECK ([CleaningStatus] IN ('Clean', 'NeedsCleaning')),

    /*
       只有停用房間可保存停用原因。
       停用時原因必填且不能只有空白；
       恢復開放販售後必須清除停用原因。
    */
    CONSTRAINT [CK_Rooms_DisabledReason]
        CHECK
        (
            (
                [SupplyStatus] = 'Disabled'
                AND [DisabledReason] IS NOT NULL
                AND LEN(LTRIM(RTRIM([DisabledReason]))) > 0
            )
            OR
            (
                [SupplyStatus] <> 'Disabled'
                AND [DisabledReason] IS NULL
            )
        )
    );
GO

/* =========================================================
   4. Employees 員工帳號
   EmployeeNumber 同時作為登入帳號
   ========================================================= */
CREATE TABLE [dbo].[Employees]
(
    [EmployeeNumber]    varchar(20) NOT NULL,
    [EmployeeName]      nvarchar(50) NOT NULL,
    [IsActive]          bit NOT NULL
        CONSTRAINT [DF_Employees_IsActive] DEFAULT (1),
    [BranchId]          int NULL,
    [PasswordHash]      varchar(255) NOT NULL,
    [Role]              varchar(20) NOT NULL,

    CONSTRAINT [PK_Employees]
        PRIMARY KEY ([EmployeeNumber]),

    CONSTRAINT [FK_Employees_Branches]
        FOREIGN KEY ([BranchId])
        REFERENCES [dbo].[Branches] ([BranchId]),

    CONSTRAINT [CK_Employees_Role]
        CHECK ([Role] IN ('SystemAdmin', 'BranchEmployee')),

    CONSTRAINT [CK_Employees_Role_Branch]
        CHECK
        (
            ([Role] = 'SystemAdmin' AND [BranchId] IS NULL)
            OR
            ([Role] = 'BranchEmployee' AND [BranchId] IS NOT NULL)
        )
);
GO

/* =========================================================
   5. Bookings 訂單

   第一版簡化：
   - 付款方式固定信用卡，不另存欄位
   - 付款金額 = TotalAmount
   - 付款時間 = CreatedAt
   - Email 只執行一次寄送動作，不保存寄送結果／時間
   - NoShow 不另存 NoShowAt，由 CheckOutDate 當日 12:00 推導
   ========================================================= */
CREATE TABLE [dbo].[Bookings]
(
    [BookingNumber]                 varchar(20) NOT NULL,
    [BranchId]                      int NOT NULL,
    [RoomTypeId]                    int NOT NULL,
    [BookerName]                    nvarchar(50) NOT NULL,
    [ContactPhone]                  varchar(20) NOT NULL,
    [Email]                         varchar(254) NOT NULL,
    [CheckInDate]                   date NOT NULL,
    [CheckOutDate]                  date NOT NULL,
    [RoomTypeNameSnapshot]          nvarchar(50) NOT NULL,
    [MaxOccupancySnapshot]          tinyint NOT NULL,
    [NightlyPriceSnapshot]          decimal(10,2) NOT NULL,
    [TotalAmount]                   decimal(12,2) NOT NULL,
    [BookingStatus]                 varchar(20) NOT NULL,
    [CreatedAt]                     datetime2(0) NOT NULL
        CONSTRAINT [DF_Bookings_CreatedAt]
        DEFAULT (CONVERT(datetime2(0), SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time')),
    [CancellationCause]             varchar(30) NULL,
    [CancellationReason]            nvarchar(500) NULL,
    [CancelledAt]                   datetime2(0) NULL,
    [CancelledByEmployeeNumber]     varchar(20) NULL,

    CONSTRAINT [PK_Bookings]
        PRIMARY KEY ([BookingNumber]),

    CONSTRAINT [FK_Bookings_RoomTypes]
        FOREIGN KEY ([RoomTypeId], [BranchId])
        REFERENCES [dbo].[RoomTypes] ([RoomTypeId], [BranchId]),

    CONSTRAINT [FK_Bookings_CancelledByEmployee]
        FOREIGN KEY ([CancelledByEmployeeNumber])
        REFERENCES [dbo].[Employees] ([EmployeeNumber]),

    CONSTRAINT [CK_Bookings_DateRange]
        CHECK ([CheckOutDate] > [CheckInDate]),

    CONSTRAINT [CK_Bookings_MaxOccupancySnapshot]
        CHECK ([MaxOccupancySnapshot] > 0),

    CONSTRAINT [CK_Bookings_NightlyPriceSnapshot]
        CHECK ([NightlyPriceSnapshot] > 0),

    CONSTRAINT [CK_Bookings_TotalAmount]
        CHECK ([TotalAmount] > 0),

    CONSTRAINT [CK_Bookings_TotalAmount_Calculation]
        CHECK
        (
            [TotalAmount]
            = [NightlyPriceSnapshot] * DATEDIFF(DAY, [CheckInDate], [CheckOutDate])
        ),

    CONSTRAINT [CK_Bookings_Status]
        CHECK
        (
            [BookingStatus] IN
            ('Paid', 'CheckedIn', 'Cancelled', 'Completed', 'NoShow')
        ),

    CONSTRAINT [CK_Bookings_CancellationCause]
        CHECK
        (
            [CancellationCause] IS NULL
            OR [CancellationCause] IN ('GuestRequest', 'HotelUnableToFulfill')
        ),

    /*
       Cancelled 時四個取消欄位必須都有值；
       其他狀態則四欄都必須為 NULL。
       CancellationReason 是否為空白字串仍由後端驗證。
    */
    CONSTRAINT [CK_Bookings_CancellationFields]
        CHECK
        (
            (
                [BookingStatus] = 'Cancelled'
                AND [CancellationCause] IS NOT NULL
                AND [CancellationReason] IS NOT NULL
                AND [CancelledAt] IS NOT NULL
                AND [CancelledByEmployeeNumber] IS NOT NULL
            )
            OR
            (
                [BookingStatus] <> 'Cancelled'
                AND [CancellationCause] IS NULL
                AND [CancellationReason] IS NULL
                AND [CancelledAt] IS NULL
                AND [CancelledByEmployeeNumber] IS NULL
            )
        )
);
GO

/* =========================================================
   6. StayRecords 住房紀錄
   一張訂單第一版最多一筆住房紀錄
   ========================================================= */
CREATE TABLE [dbo].[StayRecords]
(
    [StayRecordId]                  int IDENTITY(1,1) NOT NULL,
    [BookingNumber]                 varchar(20) NOT NULL,
    [RoomId]                        int NOT NULL,
    [RoomNumberSnapshot]            nvarchar(10) NOT NULL,
    [ActualCheckInAt]               datetime2(0) NOT NULL,
    [ActualCheckOutAt]              datetime2(0) NULL,
    [PrimaryGuestName]              nvarchar(50) NOT NULL,
    [ActualGuestCount]              tinyint NOT NULL,
    [CheckedInByEmployeeNumber]     varchar(20) NOT NULL,
    [CheckedOutByEmployeeNumber]    varchar(20) NULL,

    CONSTRAINT [PK_StayRecords]
        PRIMARY KEY ([StayRecordId]),

    CONSTRAINT [UQ_StayRecords_BookingNumber]
        UNIQUE ([BookingNumber]),

    CONSTRAINT [FK_StayRecords_Bookings]
        FOREIGN KEY ([BookingNumber])
        REFERENCES [dbo].[Bookings] ([BookingNumber]),

    CONSTRAINT [FK_StayRecords_Rooms]
        FOREIGN KEY ([RoomId])
        REFERENCES [dbo].[Rooms] ([RoomId]),

    CONSTRAINT [FK_StayRecords_CheckedInByEmployee]
        FOREIGN KEY ([CheckedInByEmployeeNumber])
        REFERENCES [dbo].[Employees] ([EmployeeNumber]),

    CONSTRAINT [FK_StayRecords_CheckedOutByEmployee]
        FOREIGN KEY ([CheckedOutByEmployeeNumber])
        REFERENCES [dbo].[Employees] ([EmployeeNumber]),

    CONSTRAINT [CK_StayRecords_ActualGuestCount]
        CHECK ([ActualGuestCount] > 0),

    CONSTRAINT [CK_StayRecords_CheckOutTime]
        CHECK
        (
            [ActualCheckOutAt] IS NULL
            OR [ActualCheckOutAt] >= [ActualCheckInAt]
        ),

    /* 退房時間與退房辦理員工必須同時為 NULL 或同時有值 */
    CONSTRAINT [CK_StayRecords_CheckOutFields]
        CHECK
        (
            ([ActualCheckOutAt] IS NULL AND [CheckedOutByEmployeeNumber] IS NULL)
            OR
            ([ActualCheckOutAt] IS NOT NULL AND [CheckedOutByEmployeeNumber] IS NOT NULL)
        )
);
GO

/*
   保證同一間實體房間同一時間最多一筆「尚未退房」住房紀錄。
   歷史已退房紀錄不受此限制。
*/
CREATE UNIQUE INDEX [UX_StayRecords_ActiveRoom]
ON [dbo].[StayRecords] ([RoomId], [ActualCheckOutAt])
WHERE [ActualCheckOutAt] IS NULL;
GO

/* =========================================================
   7. OperationTypes 操作類型
   固定參考資料；下拉選單由此表讀取，而非從 OperationLogs DISTINCT
   ========================================================= */
CREATE TABLE [dbo].[OperationTypes]
(
    [OperationTypeId]       int IDENTITY(1,1) NOT NULL,
    [OperationTypeCode]     varchar(50) NOT NULL,
    [OperationTypeName]     nvarchar(50) NOT NULL,

    CONSTRAINT [PK_OperationTypes]
        PRIMARY KEY ([OperationTypeId]),

    CONSTRAINT [UQ_OperationTypes_OperationTypeCode]
        UNIQUE ([OperationTypeCode]),

    CONSTRAINT [UQ_OperationTypes_OperationTypeName]
        UNIQUE ([OperationTypeName])
);
GO

/* =========================================================
   8. OperationLogs 基本操作紀錄
   - 僅由系統在關鍵寫入成功後新增
   - 一般功能不提供修改／刪除
   - TargetBranchId = 操作對象所屬／受影響分館
   ========================================================= */
CREATE TABLE [dbo].[OperationLogs]
(
    [OperationLogId]            int IDENTITY(1,1) NOT NULL,
    [TargetBranchId]            int NOT NULL,
    [OperatedAt]                datetime2(0) NOT NULL
        CONSTRAINT [DF_OperationLogs_OperatedAt]
        DEFAULT (CONVERT(datetime2(0), SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time')),
    [OperatorEmployeeNumber]    varchar(20) NOT NULL,
    [OperationTypeId]           int NOT NULL,
    [TargetType]                varchar(30) NOT NULL,
    [TargetIdentifier]          nvarchar(100) NOT NULL,
    [Description]               nvarchar(500) NOT NULL,

    CONSTRAINT [PK_OperationLogs]
        PRIMARY KEY ([OperationLogId]),

    CONSTRAINT [FK_OperationLogs_TargetBranch]
        FOREIGN KEY ([TargetBranchId])
        REFERENCES [dbo].[Branches] ([BranchId]),

    CONSTRAINT [FK_OperationLogs_OperatorEmployee]
        FOREIGN KEY ([OperatorEmployeeNumber])
        REFERENCES [dbo].[Employees] ([EmployeeNumber]),

    CONSTRAINT [FK_OperationLogs_OperationTypes]
        FOREIGN KEY ([OperationTypeId])
        REFERENCES [dbo].[OperationTypes] ([OperationTypeId])
);
GO

/* =========================================================
   第一版常用查詢索引
   ========================================================= */

/* 查房／房量計算 */
CREATE INDEX [IX_Bookings_Availability]
ON [dbo].[Bookings]
(
    [BranchId],
    [RoomTypeId],
    [BookingStatus],
    [CheckInDate],
    [CheckOutDate]
);
GO

/* 訂單相關功能執行前，查找並補判已達退房日 12:00 的 NoShow */
CREATE INDEX [IX_Bookings_NoShow]
ON [dbo].[Bookings] ([BookingStatus], [CheckOutDate])
INCLUDE ([BookingNumber]);
GO

/* 管理員依分館＋成立日期區間匯出 CSV */
CREATE INDEX [IX_Bookings_Branch_CreatedAt]
ON [dbo].[Bookings] ([BranchId], [CreatedAt]);
GO

/* Check-in 候選房間 */
CREATE INDEX [IX_Rooms_CheckInCandidate]
ON [dbo].[Rooms]
(
    [BranchId],
    [RoomTypeId],
    [SupplyStatus],
    [CleaningStatus]
)
INCLUDE ([RoomNumber], [Floor]);
GO

/* 分館員工清單 */
CREATE INDEX [IX_Employees_BranchId]
ON [dbo].[Employees] ([BranchId]);
GO

/* 操作紀錄：依操作對象分館與日期查詢 */
CREATE INDEX [IX_OperationLogs_TargetBranch_OperatedAt]
ON [dbo].[OperationLogs] ([TargetBranchId], [OperatedAt] DESC)
INCLUDE
(
    [OperationTypeId],
    [OperatorEmployeeNumber],
    [TargetType],
    [TargetIdentifier]
);
GO

/* 操作紀錄：依操作者帳號查詢 */
CREATE INDEX [IX_OperationLogs_Operator_OperatedAt]
ON [dbo].[OperationLogs] ([OperatorEmployeeNumber], [OperatedAt] DESC);
GO

/* =========================================================
   Schema 建立完成

   以下跨表／流程規則由後端在交易中驗證，不用額外資料表：
   - Check-in 的 Booking、Room 必須同分館且 Room 為原訂房型
   - ActualGuestCount 不得超過 Bookings.MaxOccupancySnapshot
   - Check-in：建立 StayRecord + Booking -> CheckedIn + OperationLog 同交易
   - Check-out：住房退房欄位 + Booking -> Completed
                + Room -> NeedsCleaning + OperationLog 同交易
   - 取消：Bookings 取消欄位 + Booking -> Cancelled
           + 房量釋放效果 + OperationLog 同交易
   - 可售房量：原退房日已過但尚未實際 Check-out 的有效住房，
               自原退房日起至完成 Check-out 前，仍須額外占用該房型供應，且不得與原訂單重複扣除
   - 房間停用：Room -> Disabled 時必須同時保存 DisabledReason；
             Disabled -> Open 時清除 DisabledReason
   - 清潔狀態：無住客房間可在 Clean / NeedsCleaning 間雙向切換
             + OperationLog 同交易
   - NoShow 不使用背景排程；訂單相關功能執行前依台灣時間補判，且不建立員工 OperationLog
   ========================================================= */

