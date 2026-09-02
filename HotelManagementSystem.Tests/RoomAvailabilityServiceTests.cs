namespace HotelManagementSystem.Tests;

using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

public class RoomAvailabilityServiceTests
{
    [Fact]
    public void CalculateDailyRemainingRooms_EndDateEqualsStartDate_ThrowsArgumentException()
    {
        var service = new RoomAvailabilityService(null!, null!);
        var startDate = new DateOnly(2099, 1, 1);

        Assert.Throws<ArgumentException>(() =>
            service.CalculateDailyRemainingRooms(1, startDate, startDate));
    }

    [Fact]
    public void CalculateDailyRemainingRooms_NoBookings_ReturnsOpenRoomCount()
    {
        using var context = CreateContext();

        var startDate = new DateOnly(2099, 1, 1);
        var endDate = startDate.AddDays(2);

        context.Rooms.AddRange(
            CreateRoom(1, "Open"),
            CreateRoom(2, "Open"),
            CreateRoom(3, "Open")
        );

        context.SaveChanges();

        var taipeiClock = new TaipeiClock();
        var service = new RoomAvailabilityService(taipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Equal(2, result.Count);

        Assert.Equal(3, result[startDate]);
        Assert.Equal(3, result[startDate.AddDays(1)]);
    }

    [Theory]
    [InlineData("Paid")]
    [InlineData("CheckedIn")]
    public void CalculateDailyRemainingRooms_ActiveBooking_ReducesRemainingRoomCount(string bookingStatus)
    {
        using var context = CreateContext();

        var startDate = new DateOnly(2099, 1, 1);
        var endDate = startDate.AddDays(2);
        var bookingCheckInDate = startDate;
        var bookingCheckOutDate = bookingCheckInDate.AddDays(1);

        context.Rooms.AddRange(
            CreateRoom(1, "Open"),
            CreateRoom(2, "Open"),
            CreateRoom(3, "Open")
        );

        context.Bookings.Add(
            CreateBooking(bookingStatus, bookingCheckInDate, bookingCheckOutDate)
        );

        context.SaveChanges();

        var taipeiClock = new TaipeiClock();
        var service = new RoomAvailabilityService(taipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Equal(2, result.Count);

        Assert.Equal(2, result[startDate]);
        Assert.Equal(3, result[startDate.AddDays(1)]);
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("Completed")]
    [InlineData("NoShow")]
    public void CalculateDailyRemainingRooms_InactiveBooking_DoesNotReduceRemainingRoomCount(string bookingStatus)
    {
        using var context = CreateContext();

        var startDate = new DateOnly(2099, 1, 1);
        var endDate = startDate.AddDays(2);
        var bookingCheckInDate = startDate;
        var bookingCheckOutDate = bookingCheckInDate.AddDays(1);

        context.Rooms.AddRange(
            CreateRoom(1, "Open"),
            CreateRoom(2, "Open"),
            CreateRoom(3, "Open")
        );

        context.Bookings.Add(
            CreateBooking(bookingStatus, bookingCheckInDate, bookingCheckOutDate)
        );

        context.SaveChanges();

        var taipeiClock = new TaipeiClock();
        var service = new RoomAvailabilityService(taipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Equal(2, result.Count);

        Assert.Equal(3, result[startDate]);
        Assert.Equal(3, result[startDate.AddDays(1)]);
    }

    [Fact]
    public void CalculateDailyRemainingRooms_NonOpenRooms_ReturnsOpenRoomCount()
    {
        using var context = CreateContext();

        var startDate = new DateOnly(2099, 1, 1);
        var endDate = startDate.AddDays(2);

        context.Rooms.AddRange(
            CreateRoom(1, "Open"),
            CreateRoom(2, "Reserved"),
            CreateRoom(3, "Disabled")
        );

        context.SaveChanges();

        var taipeiClock = new TaipeiClock();
        var service = new RoomAvailabilityService(taipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Equal(2, result.Count);

        Assert.Equal(1, result[startDate]);
        Assert.Equal(1, result[startDate.AddDays(1)]);
    }

    [Fact]
    public void CalculateDailyRemainingRooms_OtherRoomTypeBooking_DoesNotReduceRemainingRoomCount()
    {
        using var context = CreateContext();

        var startDate = new DateOnly(2099, 1, 1);
        var endDate = startDate.AddDays(2);
        var bookingCheckInDate = startDate;
        var bookingCheckOutDate = bookingCheckInDate.AddDays(1);

        context.Rooms.AddRange(
            CreateRoom(1, "Open"),
            CreateRoom(2, "Open"),
            CreateRoom(3, "Open"),
            CreateRoom(4, "Open", roomTypeId: 2),
            CreateRoom(5, "Open", roomTypeId: 3)
        );

        context.Bookings.AddRange(
            CreateBooking("Paid", bookingCheckInDate, bookingCheckOutDate, roomTypeId: 2, bookingNumber: 1),
            CreateBooking("Paid", bookingCheckInDate, bookingCheckOutDate, roomTypeId: 3, bookingNumber: 2)
        );

        context.SaveChanges();

        var taipeiClock = new TaipeiClock();
        var service = new RoomAvailabilityService(taipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Equal(2, result.Count);

        Assert.Equal(3, result[startDate]);
        Assert.Equal(3, result[startDate.AddDays(1)]);
    }

    [Fact]
    public void CalculateDailyRemainingRooms_BookingsAdjacentToDateRange_DoNotReduceRemainingRoomCount()
    {
        using var context = CreateContext();

        var bookingCheckInDate = new DateOnly(2099, 1, 1);
        var bookingCheckOutDate = bookingCheckInDate.AddDays(1);
        var startDate = bookingCheckOutDate;
        var endDate = startDate.AddDays(2);

        context.Rooms.AddRange(
            CreateRoom(1, "Open"),
            CreateRoom(2, "Open"),
            CreateRoom(3, "Open")
        );

        context.Bookings.AddRange(
            CreateBooking("Paid", bookingCheckInDate, bookingCheckOutDate, bookingNumber: 1),
            CreateBooking("Paid", endDate, endDate.AddDays(1), bookingNumber: 2)
        );

        context.SaveChanges();

        var taipeiClock = new TaipeiClock();
        var service = new RoomAvailabilityService(taipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Equal(2, result.Count);

        Assert.Equal(3, result[startDate]);
        Assert.Equal(3, result[startDate.AddDays(1)]);
    }

    [Fact]
    public void CalculateDailyRemainingRooms_PartiallyOverlappingBooking_ReducesOnlyOverlappingDates()
    {
        using var context = CreateContext();

        var bookingCheckInDate = new DateOnly(2099, 1, 1);
        var bookingCheckOutDate = bookingCheckInDate.AddDays(2);
        var startDate = bookingCheckInDate.AddDays(1);
        var endDate = startDate.AddDays(2);

        context.Rooms.AddRange(
            CreateRoom(1, "Open"),
            CreateRoom(2, "Open"),
            CreateRoom(3, "Open")
        );

        context.Bookings.Add(
            CreateBooking("Paid", bookingCheckInDate, bookingCheckOutDate)
        );

        context.SaveChanges();

        var taipeiClock = new TaipeiClock();
        var service = new RoomAvailabilityService(taipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Equal(2, result.Count);

        Assert.Equal(2, result[startDate]);
        Assert.Equal(3, result[startDate.AddDays(1)]);
    }

    [Fact]
    public void CalculateDailyRemainingRooms_AdjacentBookings_ReduceEachOccupiedDateWithoutDoubleCounting()
    {
        using var context = CreateContext();

        var firstBookingCheckInDate = new DateOnly(2099, 1, 1);
        var firstBookingCheckOutDate = firstBookingCheckInDate.AddDays(2);
        var secondBookingCheckInDate = firstBookingCheckOutDate;
        var secondBookingCheckOutDate = secondBookingCheckInDate.AddDays(2);
        var startDate = firstBookingCheckInDate.AddDays(1);
        var endDate = startDate.AddDays(2);

        context.Rooms.AddRange(
            CreateRoom(1, "Open"),
            CreateRoom(2, "Open"),
            CreateRoom(3, "Open")
        );

        context.Bookings.AddRange(
            CreateBooking("Paid", firstBookingCheckInDate, firstBookingCheckOutDate, bookingNumber: 1),
            CreateBooking("Paid", secondBookingCheckInDate, secondBookingCheckOutDate, bookingNumber: 2)
        );

        context.SaveChanges();

        var taipeiClock = new TaipeiClock();
        var service = new RoomAvailabilityService(taipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Equal(2, result.Count);

        Assert.Equal(2, result[startDate]);
        Assert.Equal(2, result[startDate.AddDays(1)]);
    }

    [Fact]
    public void CalculateDailyRemainingRooms_MultipleOverlappingBookings_ReducesByBookingCount()
    {
        using var context = CreateContext();

        var firstBookingCheckInDate = new DateOnly(2099, 1, 1);
        var firstBookingCheckOutDate = firstBookingCheckInDate.AddDays(2);
        var secondBookingCheckInDate = firstBookingCheckInDate.AddDays(1);
        var secondBookingCheckOutDate = secondBookingCheckInDate.AddDays(2);
        var startDate = firstBookingCheckInDate;
        var endDate = startDate.AddDays(3);

        context.Rooms.AddRange(
            CreateRoom(1, "Open"),
            CreateRoom(2, "Open"),
            CreateRoom(3, "Open")
        );

        context.Bookings.AddRange(
            CreateBooking("Paid", firstBookingCheckInDate, firstBookingCheckOutDate, bookingNumber: 1),
            CreateBooking("Paid", secondBookingCheckInDate, secondBookingCheckOutDate, bookingNumber: 2)
        );

        context.SaveChanges();

        var taipeiClock = new TaipeiClock();
        var service = new RoomAvailabilityService(taipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Equal(3, result.Count);

        Assert.Equal(2, result[startDate]);
        Assert.Equal(1, result[startDate.AddDays(1)]);
        Assert.Equal(2, result[startDate.AddDays(2)]);
    }

    [Fact]
    public void CalculateDailyRemainingRooms_OverdueStayAfterCheckoutTime_ReducesTodayAvailability()
    {
        using var context = CreateContext();

        var bookingCheckInDate = new DateOnly(2099, 1, 1);
        var bookingCheckOutDate = bookingCheckInDate.AddDays(2);
        var startDate = bookingCheckInDate.AddDays(1);
        var endDate = startDate.AddDays(2);

        var room = CreateRoom(1, "Open");
        context.Rooms.AddRange(
            room,
            CreateRoom(2, "Open"),
            CreateRoom(3, "Open")
        );

        var booking = CreateBooking(
            "CheckedIn",
            bookingCheckInDate,
            bookingCheckOutDate);

        context.Bookings.Add(booking);

        context.StayRecords.Add(
            CreateStayRecord(booking, room)
        );

        context.SaveChanges();

        var fakeTaipeiClock = new FakeTaipeiClock(new DateTime(2099, 1, 3, 13, 0, 0));
        var service = new RoomAvailabilityService(fakeTaipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Equal(2, result.Count);

        Assert.Equal(2, result[startDate]);
        Assert.Equal(2, result[startDate.AddDays(1)]);
    }

    [Fact]
    public void CalculateDailyRemainingRooms_StayBeforeCheckoutTime_DoesNotReduceTodayAvailability()
    {
        // TODO:
        // 1. 建立測試 Context
        // 2. 建立 3 間 RoomType 1 的 Open 房
        // 3. 建立一筆 CheckedIn Booking：
        //    - 2099/01/01 入住
        //    - 2099/01/03 退房
        // 4. 建立對應 StayRecord：
        //    - ActualCheckOutAt = null
        // 5. 使用 FakeTaipeiClock 固定現在時間：
        //    - 2099/01/03 11:00
        // 6. 查詢 2099/01/03 ～ 2099/01/04
        // 7. 驗證今天剩餘房量仍為 3
        //
        // 原因：
        // Booking 在退房日已不算正常占房，
        // 而現在 11:00 尚未超過 12:00 退房時間，
        // 所以也不應被算進 overdueCount。
    }

    #region helper
    private static HotelManagementContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HotelManagementContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

        return new HotelManagementContext(options);
    }

    private static Room CreateRoom(int roomId, string supplyStatus, int roomTypeId = 1)
    {
        return new Room
        {
            RoomId = roomId,
            BranchId = 1,
            RoomTypeId = roomTypeId,
            RoomNumber = $"{100 + roomId}",
            Floor = 1,
            SupplyStatus = supplyStatus,
            CleaningStatus = "Clean"
        };
    }

    private static Booking CreateBooking(
        string bookingStatus,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int roomTypeId = 1,
        int bookingNumber = 1)
    {
        const decimal nightlyPrice = 2000m;

        return new Booking
        {
            BookingNumber = $"TEST{bookingNumber:D3}",
            BranchId = 1,
            RoomTypeId = roomTypeId,

            BookerName = "測試顧客",
            ContactPhone = "0912345678",
            Email = "test@example.com",

            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,

            RoomTypeNameSnapshot = "測試房型",
            MaxOccupancySnapshot = 2,
            NightlyPriceSnapshot = nightlyPrice,
            TotalAmount =
                nightlyPrice * (checkOutDate.DayNumber - checkInDate.DayNumber),

            BookingStatus = bookingStatus,
            CreatedAt = new DateTime(2098, 12, 1)
        };
    }

    private static StayRecord CreateStayRecord(
        Booking booking,
        Room room,
        int stayRecordId = 1,
        DateTime? actualCheckOutAt = null)
    {
        return new StayRecord
        {
            StayRecordId = stayRecordId,
            BookingNumber = booking.BookingNumber,
            RoomId = room.RoomId,

            BookingNumberNavigation = booking,
            Room = room,

            RoomNumberSnapshot = room.RoomNumber,
            ActualCheckInAt = booking.CheckInDate.ToDateTime(new TimeOnly(16, 0)),
            ActualCheckOutAt = actualCheckOutAt,

            PrimaryGuestName = "TEST",
            ActualGuestCount = 2,
            CheckedInByEmployeeNumber = "TESTE001",
            CheckedOutByEmployeeNumber = actualCheckOutAt == null ? null : "TESTE001"
        };
    }

    private class FakeTaipeiClock : TaipeiClock
    {
        private readonly DateTime _now;

        public FakeTaipeiClock(DateTime now)
        {
            _now = now;
        }

        public override DateTime Now => _now;
    }
    #endregion

}

