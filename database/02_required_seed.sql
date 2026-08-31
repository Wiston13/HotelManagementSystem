/*
    HotelManagementSystem - 系統必要初始化資料
    SQL Server

    建議執行順序：
    1. 01_create_hotel_management_schema.sql
    2. 02_required_seed.sql（本檔：系統必要初始化資料）
    3. 03_demo_data.sql（展示用基礎資料）

    本檔責任：
    - OperationTypes 固定操作類型
    - 初始總系統管理員帳號

    注意：
    - 本檔應在 01 重建 Schema 後執行。
    - 初始帳號密碼固定為 Hotel@123。
    - 固定 PasswordHash 僅供目前開發／展示設定，不得用於正式環境。
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

    DECLARE @SamplePasswordHash varchar(255) =
        'AQAAAAIAAYagAAAAEAARIjNEVWZ3iJmqu8zd7v+PeRFk6r5bp/etR1cXSVRJ3jQ7XCpEip30m5ie+Qu5vg==';

    /* =========================================================
       1. Employees：初始總系統管理員
       ========================================================= */
    INSERT INTO [dbo].[Employees]
    (
        [EmployeeNumber], [EmployeeName], [IsActive], [BranchId], [PasswordHash], [Role]
    )
    VALUES
    ('E20260807001', N'系統管理員', 1, NULL, @SamplePasswordHash, 'SystemAdmin');

    /* =========================================================
       2. OperationTypes：固定 ID 1～25

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
    DBCC CHECKIDENT ('dbo.OperationTypes', RESEED, 25) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;

    SELECT N'必要初始化資料完成' AS [Result],
           (SELECT COUNT(*) FROM [dbo].[OperationTypes]) AS [OperationTypes],
           (SELECT COUNT(*) FROM [dbo].[Employees] WHERE [Role] = 'SystemAdmin') AS [SystemAdmins];
END TRY
BEGIN CATCH
    IF @IdentityInsertTable = N'dbo.OperationTypes'
        SET IDENTITY_INSERT [dbo].[OperationTypes] OFF;

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
