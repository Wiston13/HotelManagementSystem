/*
    HotelManagementSystem - customer booking flow sample data
    Target: Microsoft SQL Server / T-SQL

    Run this script only after 01_create_booking_schema.sql.
    It is intentionally for an empty database, so it cannot accidentally
    mix its fixed test data with data that your team has entered manually.
*/

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Branches', N'U') IS NULL
   OR OBJECT_ID(N'dbo.RoomTypes', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Rooms', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Bookings', N'U') IS NULL
BEGIN
    THROW 50001, N'Please run 01_create_booking_schema.sql before inserting sample data.', 1;
END;
GO

IF EXISTS (SELECT 1 FROM dbo.Branches)
   OR EXISTS (SELECT 1 FROM dbo.RoomTypes)
   OR EXISTS (SELECT 1 FROM dbo.Rooms)
   OR EXISTS (SELECT 1 FROM dbo.Bookings)
BEGIN
    THROW 50002, N'Sample data can only be inserted into an empty booking database.', 1;
END;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /* Branches: Kaohsiung is included to test a branch that stops new bookings. */
    INSERT INTO dbo.Branches
    (
        BranchName, Phone, Address, Description,
        AcceptsNewBookings, Region, ImageUrl
    )
    VALUES
        (N'台北館', '02-2345-6789', N'台北市中山區南京東路三段100號',
         N'鄰近捷運站，適合商務與短期旅客。', 1, N'台北市',
         N'https://images.example.com/branches/taipei.jpg'),
        (N'台中館', '04-2234-5678', N'台中市西區公益路200號',
         N'位於市中心，交通便利。', 1, N'台中市',
         N'https://images.example.com/branches/taichung.jpg'),
        (N'高雄館', '07-3456-7890', N'高雄市前金區中山二路88號',
         N'目前整修中，暫停接受新的網路訂房。', 0, N'高雄市',
         N'https://images.example.com/branches/kaohsiung.jpg');

    DECLARE @TaipeiBranchId int =
        (SELECT BranchId FROM dbo.Branches WHERE BranchName = N'台北館');
    DECLARE @TaichungBranchId int =
        (SELECT BranchId FROM dbo.Branches WHERE BranchName = N'台中館');
    DECLARE @KaohsiungBranchId int =
        (SELECT BranchId FROM dbo.Branches WHERE BranchName = N'高雄館');

    /* Room types: one inactive type is included to test filtering. */
    INSERT INTO dbo.RoomTypes
    (
        BranchId, RoomTypeName, MaxOccupancy, BedType,
        NightlyPrice, IsActive, Description
    )
    VALUES
        (@TaipeiBranchId, N'標準雙人房', 2, N'一張雙人床', 1800.00, 1,
         N'簡約舒適的雙人房，適合商務住宿。'),
        (@TaipeiBranchId, N'豪華雙人房', 2, N'一張加大雙人床', 2500.00, 1,
         N'空間更寬敞，附設沙發休憩區。'),
        (@TaipeiBranchId, N'家庭四人房', 4, N'兩張雙人床', 3600.00, 1,
         N'適合家庭或小型團體入住。'),
        (@TaipeiBranchId, N'景觀套房', 2, N'一張加大雙人床', 4200.00, 0,
         N'目前暫停銷售的房型，用於測試房型停用篩選。'),
        (@TaichungBranchId, N'標準雙人房', 2, N'兩張單人床', 1600.00, 1,
         N'可彈性安排成雙床房。'),
        (@TaichungBranchId, N'家庭四人房', 4, N'兩張雙人床', 3200.00, 1,
         N'採光良好的家庭房。'),
        (@KaohsiungBranchId, N'標準雙人房', 2, N'一張雙人床', 1500.00, 1,
         N'高雄館整修完成後預計開放的基本房型。');

    DECLARE @TaipeiStandardId int =
        (SELECT RoomTypeId FROM dbo.RoomTypes
         WHERE BranchId = @TaipeiBranchId AND RoomTypeName = N'標準雙人房');
    DECLARE @TaipeiDeluxeId int =
        (SELECT RoomTypeId FROM dbo.RoomTypes
         WHERE BranchId = @TaipeiBranchId AND RoomTypeName = N'豪華雙人房');
    DECLARE @TaipeiFamilyId int =
        (SELECT RoomTypeId FROM dbo.RoomTypes
         WHERE BranchId = @TaipeiBranchId AND RoomTypeName = N'家庭四人房');
    DECLARE @TaipeiSuiteId int =
        (SELECT RoomTypeId FROM dbo.RoomTypes
         WHERE BranchId = @TaipeiBranchId AND RoomTypeName = N'景觀套房');
    DECLARE @TaichungStandardId int =
        (SELECT RoomTypeId FROM dbo.RoomTypes
         WHERE BranchId = @TaichungBranchId AND RoomTypeName = N'標準雙人房');
    DECLARE @TaichungFamilyId int =
        (SELECT RoomTypeId FROM dbo.RoomTypes
         WHERE BranchId = @TaichungBranchId AND RoomTypeName = N'家庭四人房');
    DECLARE @KaohsiungStandardId int =
        (SELECT RoomTypeId FROM dbo.RoomTypes
         WHERE BranchId = @KaohsiungBranchId AND RoomTypeName = N'標準雙人房');

    /* Only Open rooms count toward a room type's sellable supply. */
    INSERT INTO dbo.Rooms
    (BranchId, RoomTypeId, RoomNumber, Floor, SupplyStatus)
    VALUES
        (@TaipeiBranchId, @TaipeiStandardId, N'101', 1, 'Open'),
        (@TaipeiBranchId, @TaipeiStandardId, N'102', 1, 'Open'),
        (@TaipeiBranchId, @TaipeiStandardId, N'103', 1, 'Open'),
        (@TaipeiBranchId, @TaipeiStandardId, N'104', 1, 'Open'),
        (@TaipeiBranchId, @TaipeiStandardId, N'105', 1, 'Reserved'),
        (@TaipeiBranchId, @TaipeiStandardId, N'106', 1, 'Disabled'),
        (@TaipeiBranchId, @TaipeiDeluxeId, N'201', 2, 'Open'),
        (@TaipeiBranchId, @TaipeiDeluxeId, N'202', 2, 'Open'),
        (@TaipeiBranchId, @TaipeiFamilyId, N'301', 3, 'Open'),
        (@TaipeiBranchId, @TaipeiFamilyId, N'302', 3, 'Open'),
        (@TaipeiBranchId, @TaipeiSuiteId, N'401', 4, 'Open'),
        (@TaichungBranchId, @TaichungStandardId, N'101', 1, 'Open'),
        (@TaichungBranchId, @TaichungStandardId, N'102', 1, 'Open'),
        (@TaichungBranchId, @TaichungStandardId, N'103', 1, 'Reserved'),
        (@TaichungBranchId, @TaichungFamilyId, N'201', 2, 'Open'),
        (@TaichungBranchId, @TaichungFamilyId, N'202', 2, 'Disabled'),
        (@KaohsiungBranchId, @KaohsiungStandardId, N'101', 1, 'Open'),
        (@KaohsiungBranchId, @KaohsiungStandardId, N'102', 1, 'Open');

    /*
        Current demo booking: BK20260813018 is intentionally the data shown
        on the booking-success page (Taipei / Standard Double / 8/10--8/12).
        Paid and CheckedIn bookings occupy supply; Cancelled, Completed, and
        NoShow records remain for order-history and status-filter tests.
    */
    INSERT INTO dbo.Bookings
    (
        BookingNumber, BranchId, RoomTypeId, BookerName, ContactPhone, Email,
        CheckInDate, CheckOutDate, RoomTypeNameSnapshot,
        NightlyPriceSnapshot, TotalAmount, BookingStatus, CreatedAt
    )
    VALUES
        ('BK20260813018', @TaipeiBranchId, @TaipeiStandardId,
         N'王小明', '0912-345-678', 'name@example.com',
         '2026-08-10', '2026-08-12', N'標準雙人房', 1800.00, 3600.00,
         'Paid', '2026-08-03T20:15:00'),
        ('BK20260809001', @TaipeiBranchId, @TaipeiStandardId,
         N'陳怡君', '0922-111-222', 'yijun.chen@example.com',
         '2026-08-09', '2026-08-11', N'標準雙人房', 1800.00, 3600.00,
         'Paid', '2026-08-02T14:30:00'),
        ('BK20260815001', @TaipeiBranchId, @TaipeiStandardId,
         N'林志豪', '0933-222-333', 'zhihao.lin@example.com',
         '2026-08-10', '2026-08-13', N'標準雙人房', 1800.00, 5400.00,
         'Cancelled', '2026-08-01T11:20:00'),
        ('BK20260802001', @TaipeiBranchId, @TaipeiDeluxeId,
         N'黃雅婷', '0944-333-444', 'yating.huang@example.com',
         '2026-08-02', '2026-08-05', N'豪華雙人房', 2500.00, 7500.00,
         'CheckedIn', '2026-07-25T09:10:00'),
        ('BK20260720001', @TaipeiBranchId, @TaipeiFamilyId,
         N'張家豪', '0955-444-555', 'jiahao.zhang@example.com',
         '2026-07-20', '2026-07-22', N'家庭四人房', 3600.00, 7200.00,
         'Completed', '2026-07-10T16:40:00'),
        ('BK20260728001', @TaichungBranchId, @TaichungStandardId,
         N'李佩珊', '0966-555-666', 'peishan.lee@example.com',
         '2026-07-28', '2026-07-30', N'標準雙人房', 1600.00, 3200.00,
         'NoShow', '2026-07-18T13:05:00'),
        ('BK20260818001', @TaichungBranchId, @TaichungFamilyId,
         N'吳冠宇', '0977-666-777', 'kuanyu.wu@example.com',
         '2026-08-18', '2026-08-20', N'家庭四人房', 3200.00, 6400.00,
         'Paid', '2026-08-03T18:45:00');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
