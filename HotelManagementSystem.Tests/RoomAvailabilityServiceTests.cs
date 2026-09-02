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

    #endregion
}

