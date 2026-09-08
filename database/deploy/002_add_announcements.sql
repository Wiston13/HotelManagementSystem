/*
    002_add_announcements.sql
    第一次部署後的增量更新；請連線至欲升級的既有資料庫執行。
    新增 dbo.Announcements、將 OperationLogs.TargetBranchId 改為 nullable，
    並加入公告操作類型 26～30；最終定義與 fresh database baseline 一致。
    正式套用後不得修改歷史內容。
*/
SET XACT_ABORT ON;

DECLARE @IdentityInsertTable nvarchar(128) = NULL;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Announcements', N'U') IS NOT NULL
        THROW 51000, N'dbo.Announcements 已存在；本支不可重複套用，請確認部署紀錄與目前 Schema。', 1;
    IF OBJECT_ID(N'dbo.OperationLogs', N'U') IS NULL
        THROW 51001, N'002：找不到 dbo.OperationLogs，請確認目前連線為既有 baseline 資料庫。', 1;
    IF OBJECT_ID(N'dbo.OperationTypes', N'U') IS NULL
        THROW 51002, N'002：找不到 dbo.OperationTypes，請確認目前連線為既有 baseline 資料庫。', 1;
    IF OBJECT_ID(N'dbo.Branches', N'U') IS NULL
        THROW 51003, N'002：找不到 dbo.Branches，請確認目前連線為既有 baseline 資料庫。', 1;
    IF COL_LENGTH(N'dbo.OperationLogs', N'TargetBranchId') IS NULL
        THROW 51004, N'002：找不到 dbo.OperationLogs.TargetBranchId，請確認目前 Schema。', 1;
    IF EXISTS
    (
        SELECT 1
        FROM [dbo].[OperationTypes]
        WHERE [OperationTypeId] BETWEEN 26 AND 30
           OR [OperationTypeCode] IN
              ('AnnouncementCreated', 'AnnouncementUpdated', 'AnnouncementDeleted',
               'AnnouncementDisabled', 'AnnouncementEnabled')
           OR [OperationTypeName] IN
              (N'新增公告', N'修改公告', N'刪除公告', N'停用公告', N'啟用公告')
    )
        THROW 51005, N'002：公告操作類型 26～30 已存在或發生代碼衝突，請確認部署紀錄與 Required Data。', 1;

    CREATE TABLE [dbo].[Announcements]
    (
        [AnnouncementId]    int IDENTITY(1,1) NOT NULL,
        [Title]             nvarchar(100) NOT NULL,
        [Content]           nvarchar(1000) NOT NULL,
        [StartAt]           datetime2(0) NOT NULL,
        [EndAt]             datetime2(0) NOT NULL,
        [IsActive]          bit NOT NULL
            CONSTRAINT [DF_Announcements_IsActive] DEFAULT (1),
        [ShowToGuest]       bit NOT NULL
            CONSTRAINT [DF_Announcements_ShowToGuest] DEFAULT (0),
        [CreatedAt]         datetime2(0) NOT NULL
            CONSTRAINT [DF_Announcements_CreatedAt]
            DEFAULT (CONVERT(datetime2(0), SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time')),

        CONSTRAINT [PK_Announcements]
            PRIMARY KEY ([AnnouncementId]),

        CONSTRAINT [CK_Announcements_DateRange]
            CHECK ([EndAt] > [StartAt]),

        /* 排除空字串與全由半形空格組成的值；完整空白與輸入驗證仍由後端負責。 */
        CONSTRAINT [CK_Announcements_Title]
            CHECK (LEN(LTRIM(RTRIM([Title]))) > 0),

        CONSTRAINT [CK_Announcements_Content]
            CHECK (LEN(LTRIM(RTRIM([Content]))) > 0)
    );

    /*
       NULL 代表全系統層級操作；有值時仍由 FK 保證是真實分館。
       先移除依賴物件，再保留資料原地修改欄位，最後還原 baseline 的 FK 與索引。
    */
    IF OBJECT_ID(N'dbo.FK_OperationLogs_TargetBranch', N'F') IS NOT NULL
        ALTER TABLE [dbo].[OperationLogs]
            DROP CONSTRAINT [FK_OperationLogs_TargetBranch];

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'dbo.OperationLogs')
          AND [name] = N'IX_OperationLogs_TargetBranch_OperatedAt'
    )
        DROP INDEX [IX_OperationLogs_TargetBranch_OperatedAt]
            ON [dbo].[OperationLogs];

    ALTER TABLE [dbo].[OperationLogs]
        ALTER COLUMN [TargetBranchId] int NULL;

    ALTER TABLE [dbo].[OperationLogs]
        ADD CONSTRAINT [FK_OperationLogs_TargetBranch]
            FOREIGN KEY ([TargetBranchId])
            REFERENCES [dbo].[Branches] ([BranchId]);

    CREATE INDEX [IX_OperationLogs_TargetBranch_OperatedAt]
    ON [dbo].[OperationLogs] ([TargetBranchId], [OperatedAt] DESC)
    INCLUDE
    (
        [OperationTypeId],
        [OperatorEmployeeNumber],
        [TargetType],
        [TargetIdentifier]
    );

    SET @IdentityInsertTable = N'dbo.OperationTypes';
    SET IDENTITY_INSERT [dbo].[OperationTypes] ON;

    INSERT INTO [dbo].[OperationTypes]
        ([OperationTypeId], [OperationTypeCode], [OperationTypeName])
    VALUES
    (26, 'AnnouncementCreated',  N'新增公告'),
    (27, 'AnnouncementUpdated',  N'修改公告'),
    (28, 'AnnouncementDeleted',  N'刪除公告'),
    (29, 'AnnouncementDisabled', N'停用公告'),
    (30, 'AnnouncementEnabled',  N'啟用公告');

    SET IDENTITY_INSERT [dbo].[OperationTypes] OFF;
    SET @IdentityInsertTable = NULL;

    DBCC CHECKIDENT ('dbo.OperationTypes', RESEED, 30) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @IdentityInsertTable = N'dbo.OperationTypes'
        SET IDENTITY_INSERT [dbo].[OperationTypes] OFF;

    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
