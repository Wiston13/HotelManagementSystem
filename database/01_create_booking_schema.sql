/*
    HotelManagementSystem - customer booking flow
    Target: Microsoft SQL Server / T-SQL

    Scope: Branches, RoomTypes, Rooms, Bookings
    This script assumes that the target database has already been created and selected.
*/

CREATE DATABASE HotelManagementSystem;
GO

USE HotelManagementSystem;
GO

SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    CREATE TABLE dbo.Branches
    (
        BranchId                int IDENTITY(1, 1) NOT NULL,
        BranchName              nvarchar(50) NOT NULL,
        Phone                   varchar(20) NOT NULL,
        Address                 nvarchar(200) NOT NULL,
        Description             nvarchar(max) NULL,
        AcceptsNewBookings      bit NOT NULL
            CONSTRAINT DF_Branches_AcceptsNewBookings DEFAULT (1),
        Region                  nvarchar(50) NULL,
        ImageUrl                nvarchar(2048) NULL,

        CONSTRAINT PK_Branches PRIMARY KEY (BranchId),
        CONSTRAINT UQ_Branches_BranchName UNIQUE (BranchName)
    );

    CREATE TABLE dbo.RoomTypes
    (
        RoomTypeId              int IDENTITY(1, 1) NOT NULL,
        BranchId                int NOT NULL,
        RoomTypeName            nvarchar(50) NOT NULL,
        MaxOccupancy            tinyint NOT NULL,
        BedType                 nvarchar(50) NOT NULL,
        NightlyPrice            decimal(10, 2) NOT NULL,
        IsActive                bit NOT NULL
            CONSTRAINT DF_RoomTypes_IsActive DEFAULT (1),
        Description             nvarchar(500) NULL,

        CONSTRAINT PK_RoomTypes PRIMARY KEY (RoomTypeId),
        CONSTRAINT FK_RoomTypes_Branches
            FOREIGN KEY (BranchId) REFERENCES dbo.Branches (BranchId),
        CONSTRAINT UQ_RoomTypes_BranchId_RoomTypeName
            UNIQUE (BranchId, RoomTypeName),

        -- Required so Rooms and Bookings can use a composite FK to enforce
        -- that their BranchId matches the selected room type's BranchId.
        CONSTRAINT UQ_RoomTypes_RoomTypeId_BranchId
            UNIQUE (RoomTypeId, BranchId),

        CONSTRAINT CK_RoomTypes_MaxOccupancy
            CHECK (MaxOccupancy > 0),
        CONSTRAINT CK_RoomTypes_NightlyPrice
            CHECK (NightlyPrice > 0)
    );

    CREATE TABLE dbo.Rooms
    (
        RoomId                  int IDENTITY(1, 1) NOT NULL,
        BranchId                int NOT NULL,
        RoomTypeId              int NOT NULL,
        RoomNumber              nvarchar(10) NOT NULL,
        Floor                   smallint NOT NULL,
        SupplyStatus            varchar(20) NOT NULL
            CONSTRAINT DF_Rooms_SupplyStatus DEFAULT ('Open'),

        CONSTRAINT PK_Rooms PRIMARY KEY (RoomId),

        -- Besides relating the room to a type, this blocks cross-branch data,
        -- such as a Taipei room pointing to a Kaohsiung room type.
        CONSTRAINT FK_Rooms_RoomTypes_Branch
            FOREIGN KEY (RoomTypeId, BranchId)
            REFERENCES dbo.RoomTypes (RoomTypeId, BranchId),

        CONSTRAINT UQ_Rooms_BranchId_RoomNumber
            UNIQUE (BranchId, RoomNumber),
        CONSTRAINT CK_Rooms_SupplyStatus
            CHECK (SupplyStatus IN ('Open', 'Reserved', 'Disabled'))
    );

    CREATE TABLE dbo.Bookings
    (
        BookingNumber           varchar(20) NOT NULL,
        BranchId                int NOT NULL,
        RoomTypeId              int NOT NULL,
        BookerName              nvarchar(50) NOT NULL,
        ContactPhone            varchar(20) NOT NULL,
        Email                   varchar(254) NOT NULL,
        CheckInDate             date NOT NULL,
        CheckOutDate            date NOT NULL,
        RoomTypeNameSnapshot    nvarchar(50) NOT NULL,
        NightlyPriceSnapshot    decimal(10, 2) NOT NULL,
        -- TotalAmount has a wider range than one night's price.
        TotalAmount             decimal(12, 2) NOT NULL,
        BookingStatus           varchar(20) NOT NULL,
        CreatedAt               datetime2(0) NOT NULL
            CONSTRAINT DF_Bookings_CreatedAt DEFAULT (SYSDATETIME()),

        CONSTRAINT PK_Bookings PRIMARY KEY (BookingNumber),

        -- A booking is for a room type, not for a physical RoomId.
        CONSTRAINT FK_Bookings_RoomTypes_Branch
            FOREIGN KEY (RoomTypeId, BranchId)
            REFERENCES dbo.RoomTypes (RoomTypeId, BranchId),

        CONSTRAINT CK_Bookings_DateRange
            CHECK (CheckOutDate > CheckInDate),
        CONSTRAINT CK_Bookings_NightlyPriceSnapshot
            CHECK (NightlyPriceSnapshot > 0),
        CONSTRAINT CK_Bookings_TotalAmountPositive
            CHECK (TotalAmount > 0),
        CONSTRAINT CK_Bookings_TotalAmountMatchesStay
            CHECK
            (
                TotalAmount = CONVERT
                (
                    decimal(12, 2),
                    NightlyPriceSnapshot * DATEDIFF(day, CheckInDate, CheckOutDate)
                )
            ),
        CONSTRAINT CK_Bookings_BookingStatus
            CHECK (BookingStatus IN ('Paid', 'CheckedIn', 'Cancelled', 'Completed', 'NoShow'))
    );

    -- Supports the availability calculation for one room type over a date range.
    CREATE INDEX IX_Bookings_RoomType_Status_Dates
        ON dbo.Bookings (RoomTypeId, BookingStatus, CheckInDate, CheckOutDate);

    -- Supports counting rooms that contribute to a room type's supply.
    CREATE INDEX IX_Rooms_RoomType_SupplyStatus
        ON dbo.Rooms (RoomTypeId, SupplyStatus);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
