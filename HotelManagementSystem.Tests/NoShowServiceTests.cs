using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Services;
using Microsoft.EntityFrameworkCore;


namespace HotelManagementSystem.Tests;

public class NoShowServiceTests
{
    [Fact]
    public async Task UpdateNoShowsAsync_BeforeCutoff_TodayCheckoutPaidBooking_RemainsPaid()
    {
        using var context = CreateContext();

        Booking booking = new Booking
        {
            BookingNumber = $"TEST001",
            BranchId = 1,
            RoomTypeId = 1,

            BookerName = "測試顧客",
            ContactPhone = "0912345678",
            Email = "test@example.com",

            CheckInDate = new DateOnly(2099, 1, 1),
            CheckOutDate = new DateOnly(2099, 1, 3),

            RoomTypeNameSnapshot = "測試房型",
            MaxOccupancySnapshot = 2,
            NightlyPriceSnapshot = 2000,
            TotalAmount = 4000,

            BookingStatus = "Paid",
            CreatedAt = new DateTime(2098, 12, 1)
        };

        context.Bookings.Add(
            booking
        );

        await context.SaveChangesAsync();

        var fakeTaipeiClock = new FakeTaipeiClock(new DateTime(2099, 1, 3, 11, 59, 59));
        var service = new NoShowService(fakeTaipeiClock, context);

        await service.UpdateNoShowsAsync();

        Assert.Equal("Paid", booking.BookingStatus);
    }

    [Fact]
    public async Task UpdateNoShowsAsync_AtCutoff_TodayCheckoutPaidBooking_BecomesNoShow()
    {
        using var context = CreateContext();

        Booking booking = new Booking
        {
            BookingNumber = $"TEST001",
            BranchId = 1,
            RoomTypeId = 1,

            BookerName = "測試顧客",
            ContactPhone = "0912345678",
            Email = "test@example.com",

            CheckInDate = new DateOnly(2099, 1, 1),
            CheckOutDate = new DateOnly(2099, 1, 3),

            RoomTypeNameSnapshot = "測試房型",
            MaxOccupancySnapshot = 2,
            NightlyPriceSnapshot = 2000,
            TotalAmount = 4000,

            BookingStatus = "Paid",
            CreatedAt = new DateTime(2098, 12, 1)
        };

        context.Bookings.Add(
            booking
        );

        await context.SaveChangesAsync();

        var fakeTaipeiClock = new FakeTaipeiClock(new DateTime(2099, 1, 3, 12, 0, 0));
        var service = new NoShowService(fakeTaipeiClock, context);

        await service.UpdateNoShowsAsync();

        Assert.Equal("NoShow", booking.BookingStatus);
    }

    private static HotelManagementContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HotelManagementContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

        return new HotelManagementContext(options);
    }

    private class FakeTaipeiClock : TaipeiClock
    {
        private readonly DateTime _now;

        public FakeTaipeiClock(DateTime now)
        {
            _now = now;
        }
        public override DateTime Now
        {
            get { return _now; }
        }
    }
}