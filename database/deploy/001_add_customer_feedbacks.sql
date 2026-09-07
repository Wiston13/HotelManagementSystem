/*
    001_add_customer_feedbacks.sql
    第一次部署後的增量更新；請連線至欲升級的既有資料庫執行。
    只新增 dbo.CustomerFeedbacks，定義與 01_create_hotel_management_schema.sql 一致。
    正式套用後不得修改歷史內容。
*/
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.CustomerFeedbacks') IS NOT NULL
        THROW 51000, N'dbo.CustomerFeedbacks 已存在；本支不可重複套用，請確認部署紀錄與目前 Schema。', 1;
    IF OBJECT_ID(N'dbo.Branches', N'U') IS NULL
        THROW 51001, N'001：找不到 dbo.Branches，請確認目前連線為既有 baseline 資料庫。', 1;

    CREATE TABLE [dbo].[CustomerFeedbacks]
    (
        [Id]              int IDENTITY(1,1) NOT NULL,
        [BranchId]        int NOT NULL,
        [CustomerName]    nvarchar(50) NOT NULL,
        [Email]           varchar(254) NOT NULL,
        [Phone]           varchar(20) NULL,
        [Content]         nvarchar(500) NOT NULL,
        [CreatedAt]       datetime2(0) NOT NULL
            CONSTRAINT [DF_CustomerFeedbacks_CreatedAt]
            DEFAULT (CONVERT(datetime2(0), SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time')),

        CONSTRAINT [PK_CustomerFeedbacks]
            PRIMARY KEY ([Id]),

        CONSTRAINT [FK_CustomerFeedbacks_Branches]
            FOREIGN KEY ([BranchId])
            REFERENCES [dbo].[Branches] ([BranchId])
            ON DELETE NO ACTION,

        CONSTRAINT [CK_CustomerFeedbacks_CustomerName]
            CHECK (LEN(LTRIM(RTRIM([CustomerName]))) > 0),

        CONSTRAINT [CK_CustomerFeedbacks_Email]
            CHECK (LEN(LTRIM(RTRIM([Email]))) > 0),

        CONSTRAINT [CK_CustomerFeedbacks_Content]
            CHECK (LEN(LTRIM(RTRIM([Content]))) > 0),

        /* 後端先移除空白與半形連字號；未填保存 NULL，有值只接受 ASCII 數字。 */
        CONSTRAINT [CK_CustomerFeedbacks_Phone]
            CHECK ([Phone] IS NULL OR (DATALENGTH([Phone]) > 0 AND [Phone] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'))
    );

    CREATE INDEX [IX_CustomerFeedbacks_Branch_CreatedAt]
    ON [dbo].[CustomerFeedbacks] ([BranchId], [CreatedAt] DESC);

    CREATE INDEX [IX_CustomerFeedbacks_CreatedAt]
    ON [dbo].[CustomerFeedbacks] ([CreatedAt] DESC);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
