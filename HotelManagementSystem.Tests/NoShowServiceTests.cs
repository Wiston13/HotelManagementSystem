using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Services;
using Microsoft.EntityFrameworkCore;


namespace HotelManagementSystem.Tests;

public class NoShowServiceTests
{
    // 規則：今日退房的 Paid 訂單在 12:00 前不得改為 NoShow。
    // 原因：11:59:59 尚未到達 NoShow 判定時間。
    [Fact]
    public async Task UpdateNoShowsAsync_BeforeCutoff_TodayCheckoutPaidBooking_RemainsPaid()
    {
        using var context = CreateContext();

        var booking = CreateBooking("Paid", new DateOnly(2099, 1, 1), new DateOnly(2099, 1, 3));

        context.Bookings.Add(booking);

        await context.SaveChangesAsync();

        var fakeTaipeiClock = new FakeTaipeiClock(new DateTime(2099, 1, 3, 11, 59, 59));
        var service = new NoShowService(fakeTaipeiClock, context);

        await service.UpdateNoShowsAsync();

        Assert.Equal("Paid", booking.BookingStatus);
    }

    // 規則：今日退房的 Paid 訂單自 12:00 起應改為 NoShow。
    // 原因：12:00:00 已達 NoShow 判定時間。
    [Fact]
    public async Task UpdateNoShowsAsync_AtCutoff_TodayCheckoutPaidBooking_BecomesNoShow()
    {
        using var context = CreateContext();

        var booking = CreateBooking("Paid", new DateOnly(2099, 1, 1), new DateOnly(2099, 1, 3));

        context.Bookings.Add(booking);

        await context.SaveChangesAsync();

        var fakeTaipeiClock = new FakeTaipeiClock(new DateTime(2099, 1, 3, 12, 0, 0));
        var service = new NoShowService(fakeTaipeiClock, context);

        await service.UpdateNoShowsAsync();

        Assert.Equal("NoShow", booking.BookingStatus);
    }

    // 規則：前一天已逾期的 Paid 訂單應改為 NoShow。
    // 原因：退房日早於今日，即使今日未到 12:00 仍已逾期。
    [Fact]
    public async Task UpdateNoShowsAsync_BeforeCutoff_PreviousDayCheckoutPaidBooking_BecomesNoShow()
    {
        using var context = CreateContext();

        var booking = CreateBooking("Paid", new DateOnly(2099, 1, 1), new DateOnly(2099, 1, 3));

        context.Bookings.Add(booking);

        await context.SaveChangesAsync();

        var fakeTaipeiClock = new FakeTaipeiClock(new DateTime(2099, 1, 4, 10, 0, 0));
        var service = new NoShowService(fakeTaipeiClock, context);

        await service.UpdateNoShowsAsync();

        Assert.Equal("NoShow", booking.BookingStatus);
    }

    // 規則：非 Paid 訂單不得由 NoShowService 修改。
    // 原因：CheckedIn 並非 NoShow 的可處理狀態。
    [Fact]
    public async Task UpdateNoShowsAsync_NonPaidBooking_DoesNotBecomeNoShow()
    {
        using var context = CreateContext();

        var booking = CreateBooking("CheckedIn", new DateOnly(2099, 1, 1), new DateOnly(2099, 1, 3));

        context.Bookings.Add(booking);

        await context.SaveChangesAsync();

        var fakeTaipeiClock = new FakeTaipeiClock(new DateTime(2099, 1, 4, 10, 0, 0));
        var service = new NoShowService(fakeTaipeiClock, context);

        await service.UpdateNoShowsAsync();

        Assert.Equal("CheckedIn", booking.BookingStatus);
    }

    // 規則：已有 StayRecord 的 Paid 訂單不得改為 NoShow。
    // 原因：已有入住紀錄，表示訂單並非未入住。
    [Fact]
    public async Task UpdateNoShowsAsync_PaidBookingWithStayRecord_DoesNotBecomeNoShow()
    {
        using var context = CreateContext();

        var booking = CreateBooking("Paid", new DateOnly(2099, 1, 1), new DateOnly(2099, 1, 3));

        context.Bookings.Add(booking);

        var room = new Room
        {
            RoomId = 1,
            BranchId = 1,
            RoomTypeId = 1,
            RoomNumber = "101",
            Floor = 1,
            SupplyStatus = "Open",
            CleaningStatus = "Clean"
        };

        context.Rooms.Add(room);

        var stayRecord = new StayRecord
        {
            StayRecordId = 1,
            BookingNumber = booking.BookingNumber,
            RoomId = 1,

            BookingNumberNavigation = booking,
            Room = room,

            RoomNumberSnapshot = room.RoomNumber,
            ActualCheckInAt = booking.CheckInDate.ToDateTime(new TimeOnly(16, 0)),
            ActualCheckOutAt = null,

            PrimaryGuestName = "TEST",
            ActualGuestCount = 2,
            CheckedInByEmployeeNumber = "TESTE001",
            CheckedOutByEmployeeNumber = null
        };

        context.StayRecords.Add(stayRecord);

        await context.SaveChangesAsync();

        var fakeTaipeiClock = new FakeTaipeiClock(new DateTime(2099, 1, 4, 10, 0, 0));
        var service = new NoShowService(fakeTaipeiClock, context);

        await service.UpdateNoShowsAsync();

        Assert.Equal("Paid", booking.BookingStatus);
    }

    #region helper
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
    #endregion
}
