/*
    002_add_announcements.sql
    第一次部署後的增量更新；請連線至欲升級的既有資料庫執行。
    只新增 dbo.Announcements，定義與 01_create_hotel_management_schema.sql 一致。
    正式套用後不得修改歷史內容。
*/
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Announcements') IS NOT NULL
        THROW 51000, N'dbo.Announcements 已存在；本支不可重複套用，請確認部署紀錄與目前 Schema。', 1;

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

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
