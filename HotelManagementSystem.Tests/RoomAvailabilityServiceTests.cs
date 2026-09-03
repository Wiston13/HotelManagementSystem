namespace HotelManagementSystem.Tests;

using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

public class RoomAvailabilityServiceTests
{
    #region CalculateDailyRemainingRooms Method
    // 規則：查詢區間的結束日必須晚於開始日。
    // 原因：相同日期不構成有效的 [startDate, endDate) 區間，應拒絕計算。
    [Fact]
    public void CalculateDailyRemainingRooms_EndDateEqualsStartDate_ThrowsArgumentException()
    {
        var service = new RoomAvailabilityService(null!, null!);
        var startDate = new DateOnly(2099, 1, 1);

        Assert.Throws<ArgumentException>(() =>
            service.CalculateDailyRemainingRooms(1, startDate, startDate));
    }

    // 規則：沒有訂單時，每個查詢日的可用房數等於 Open 房間數。
    // 原因：查詢區間 [startDate, endDate) 內沒有任何占用來源扣減三間 Open 房。
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

    // 規則：Paid 與 CheckedIn 訂單會在住宿日扣減同房型的可用房數。
    // 原因：訂單的入住日計入占用、退房日不計入，因此只有第一個查詢日剩兩間房。
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

    // 規則：Cancelled、Completed 與 NoShow 訂單不應占用房間供應。
    // 原因：這些狀態不是有效住宿，占用區間內兩日都維持三間 Open 房。
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

    // 規則：只有 SupplyStatus 為 Open 的房間可納入供應量。
    // 原因：Reserved 與 Disabled 房間不可售，兩個查詢日皆只剩一間 Open 房。
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

    // 規則：其他房型的 Booking 不得扣減目前查詢房型的可用房數。
    // 原因：兩筆有效訂單皆屬其他房型，因此房型 1 的三間 Open 房不受影響。
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

    // 規則：查詢區間外、剛好貼齊邊界的 Booking 不應占用查詢日。
    // 原因：[startDate, endDate) 採左閉右開，前筆退房日與後筆入住日都不落在區間內。
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

    // 規則：Booking 只扣減與查詢區間重疊的住宿日。
    // 原因：訂單與查詢區間只在第一日重疊；退房日不計入原訂單住宿占用。
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

    // 規則：相鄰 Booking 應各扣減自己的住宿日，不能在交界日重複扣減。
    // 原因：前筆的退房日正是後筆入住日，查詢區間內每一天都只被一筆訂單占用。
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

    // 規則：同房型的重疊 Booking 應依同日實際占用筆數扣減供應量。
    // 原因：三個查詢日分別被一、兩、一筆訂單占用，所以剩餘房數為 2、1、2。
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

    // 規則：12:00:00 起，尚未退房的 CheckedIn 住房視為逾期未退房。
    // 原因：原訂單占用入住日，逾期住房再額外影響退房當日，兩個查詢日皆只剩兩間房。
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

    // 規則：11:59:59 尚未達 12:00，不應視為逾期未退房。
    // 原因：退房日不計入原訂單住宿占用，且尚未逾期，所以查詢當日維持三間房。
    [Fact]
    public void CalculateDailyRemainingRooms_StayBeforeCheckoutTime_DoesNotReduceTodayAvailability()
    {
        using var context = CreateContext();

        var bookingCheckInDate = new DateOnly(2099, 1, 1);
        var bookingCheckOutDate = bookingCheckInDate.AddDays(2);
        var startDate = bookingCheckOutDate;
        var endDate = startDate.AddDays(1);

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

        var fakeTaipeiClock = new FakeTaipeiClock(new DateTime(2099, 1, 3, 11, 59, 59));
        var service = new RoomAvailabilityService(fakeTaipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Single(result);
        Assert.Equal(3, result[startDate]);
    }

    // 規則：12:00:00 是逾期未退房的起算邊界。
    // 原因：退房當日自此刻起仍未退房的 StayRecord 會額外占用一間房，剩餘兩間房。
    [Fact]
    public void CalculateDailyRemainingRooms_StayAtCheckoutTime_ReducesTodayAvailability()
    {
        using var context = CreateContext();

        var bookingCheckInDate = new DateOnly(2099, 1, 1);
        var bookingCheckOutDate = bookingCheckInDate.AddDays(2);
        var startDate = bookingCheckOutDate;
        var endDate = startDate.AddDays(1);

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

        var fakeTaipeiClock = new FakeTaipeiClock(new DateTime(2099, 1, 3, 12, 0, 0));
        var service = new RoomAvailabilityService(fakeTaipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Single(result);
        Assert.Equal(2, result[startDate]);
    }

    // 規則：已完成退房的 StayRecord 不得再視為逾期住房。
    // 原因：退房日不計入原訂單住宿占用，且 ActualCheckOutAt 已存在，查詢當日維持三間房。
    [Fact]
    public void CalculateDailyRemainingRooms_CompletedStay_DoesNotReduceTodayAvailability()
    {
        using var context = CreateContext();

        var bookingCheckInDate = new DateOnly(2099, 1, 1);
        var bookingCheckOutDate = bookingCheckInDate.AddDays(2);
        var startDate = bookingCheckOutDate;
        var endDate = startDate.AddDays(1);

        var room = CreateRoom(1, "Open");
        context.Rooms.AddRange(
            room,
            CreateRoom(2, "Open"),
            CreateRoom(3, "Open")
        );

        var booking = CreateBooking(
            "Completed",
            bookingCheckInDate,
            bookingCheckOutDate);

        context.Bookings.Add(booking);

        context.StayRecords.Add(
            CreateStayRecord(booking, room, actualCheckOutAt: new DateTime(2099, 1, 3, 12, 30, 0))
        );

        context.SaveChanges();

        var fakeTaipeiClock = new FakeTaipeiClock(new DateTime(2099, 1, 3, 13, 0, 0));
        var service = new RoomAvailabilityService(fakeTaipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Single(result);
        Assert.Equal(3, result[startDate]);
    }

    // 規則：逾期住房只額外影響時鐘所指的查詢當日。
    // 原因：前一天退房但仍未退房的住房在今日扣減一間房，次日不應持續扣減。
    [Fact]
    public void CalculateDailyRemainingRooms_OverdueStayFromPreviousDay_ReducesTodayAvailability()
    {
        using var context = CreateContext();

        var bookingCheckInDate = new DateOnly(2099, 1, 1);
        var bookingCheckOutDate = bookingCheckInDate.AddDays(2);
        var startDate = bookingCheckOutDate.AddDays(1);
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

        var fakeTaipeiClock = new FakeTaipeiClock(new DateTime(2099, 1, 4, 10, 0, 0));
        var service = new RoomAvailabilityService(fakeTaipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Equal(2, result.Count);

        Assert.Equal(2, result[startDate]);
        Assert.Equal(3, result[startDate.AddDays(1)]);
    }

    // 規則：其他房型的逾期 StayRecord 不得影響目前查詢房型。
    // 原因：逾期住房屬房型 2，房型 1 的三間 Open 房在查詢當日皆可用。
    [Fact]
    public void CalculateDailyRemainingRooms_OverdueStayInOtherRoomType_DoesNotReduceTodayAvailability()
    {
        using var context = CreateContext();

        var bookingCheckInDate = new DateOnly(2099, 1, 1);
        var bookingCheckOutDate = bookingCheckInDate.AddDays(2);
        var startDate = bookingCheckOutDate.AddDays(1); // 1/4
        var endDate = startDate.AddDays(1);             // 1/5

        var overdueRoom = CreateRoom(4, "Open", roomTypeId: 2);
        context.Rooms.AddRange(
            CreateRoom(1, "Open"),
            CreateRoom(2, "Open"),
            CreateRoom(3, "Open"),
            overdueRoom
        );

        var booking = CreateBooking(
            "CheckedIn",
            bookingCheckInDate,
            bookingCheckOutDate,
            roomTypeId: 2);

        context.Bookings.Add(booking);

        context.StayRecords.Add(
            CreateStayRecord(booking, overdueRoom)
        );

        context.SaveChanges();

        var fakeTaipeiClock = new FakeTaipeiClock(new DateTime(2099, 1, 4, 10, 0, 0));
        var service = new RoomAvailabilityService(fakeTaipeiClock, context);

        var result = service.CalculateDailyRemainingRooms(1, startDate, endDate);

        Assert.Single(result);
        Assert.Equal(3, result[startDate]);
    }

    #endregion

    #region CalculateMinimumRemainingRooms
    // 規則：最小剩餘房量應從多日的每日剩餘房量中取最低值。
    // 原因：三個查詢日的剩餘房量為 2、1、2，因此結果應為 1。
    [Fact]
    public void CalculateMinimumRemainingRooms_MultipleDays_ReturnsLowestRemainingCount()
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

        var result = service.CalculateMinimumRemainingRooms(1, startDate, endDate);

        Assert.Equal(1, result);
    }
    #endregion

    #region FindCapacityShortages
    // 規則：supplyReduction 小於 0 時，應丟出 ArgumentOutOfRangeException。
    // 原因：供應量減少不能為負值，-1 不代表有效的減少數量。
    [Fact]
    public void FindCapacityShortages_NegativeSupplyReduction_ThrowsArgumentOutOfRangeException()
    {
        var service = new RoomAvailabilityService(null!, null!);

        var startDate = new DateOnly(2099, 1, 1);
        var endDate = new DateOnly(2099, 1, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.FindCapacityShortages(1, startDate, endDate, -1));
    }

    // 規則：減少後仍大於 0 的日期不算 shortage。
    // 原因：三間 Open 房減少一間後仍有兩間可用，查詢區間不應回傳不足日期。
    [Fact]
    public void FindCapacityShortages_NoCapacityShortage_ReturnsEmpty()
    {
        using var context = CreateContext();

        context.Rooms.AddRange(
            CreateRoom(1, "Open"),
            CreateRoom(2, "Open"),
            CreateRoom(3, "Open")
        );

        context.SaveChanges();

        var startDate = new DateOnly(2099, 1, 1);
        var endDate = new DateOnly(2099, 1, 5);

        var taipeiClock = new TaipeiClock();
        var service = new RoomAvailabilityService(taipeiClock, context);

        Assert.Empty(service.FindCapacityShortages(1, startDate, endDate, 1));
    }

    // 規則：減少後剛好等於 0 的日期也不算 shortage。
    // 原因：唯一的 Open 房減少一間後雖無剩餘，但未低於 0，因此不應回傳不足日期。
    [Fact]
    public void FindCapacityShortages_ReductionEqualsRemainingCapacity_DoesNotReportShortage()
    {
        using var context = CreateContext();

        context.Rooms.AddRange(
            CreateRoom(1, "Open"),
            CreateRoom(2, "Disabled"),
            CreateRoom(3, "Disabled")
        );

        context.SaveChanges();

        var startDate = new DateOnly(2099, 1, 1);
        var endDate = new DateOnly(2099, 1, 5);

        var taipeiClock = new TaipeiClock();
        var service = new RoomAvailabilityService(taipeiClock, context);

        Assert.Empty(service.FindCapacityShortages(1, startDate, endDate, 1));
    }

    // 規則：只有減少後小於 0 的日期才回傳，且不足量必須正確。
    // 原因：第二日兩筆訂單已用盡兩間 Open 房，再減少一間後不足 1，其餘日期不短缺。
    [Fact]
    public void FindCapacityShortages_CapacityShortage_ReturnsAffectedDatesAndShortageAmounts()
    {
        using var context = CreateContext();

        var startDate = new DateOnly(2099, 1, 1);
        var endDate = startDate.AddDays(3);
        var firstBookingCheckInDate = startDate;
        var firstBookingCheckOutDate = firstBookingCheckInDate.AddDays(2);
        var secondBookingCheckInDate = startDate.AddDays(1);
        var secondBookingCheckOutDate = secondBookingCheckInDate.AddDays(2);

        context.Rooms.AddRange(
            CreateRoom(1, "Open"),
            CreateRoom(2, "Open"),
            CreateRoom(3, "Disabled")
        );

        context.Bookings.AddRange(
            CreateBooking("Paid", firstBookingCheckInDate, firstBookingCheckOutDate, bookingNumber: 1),
            CreateBooking("Paid", secondBookingCheckInDate, secondBookingCheckOutDate, bookingNumber: 2)
        );

        context.SaveChanges();

        var taipeiClock = new TaipeiClock();
        var service = new RoomAvailabilityService(taipeiClock, context);

        var result = service.FindCapacityShortages(1, startDate, endDate, 1);

        Assert.Single(result);
        Assert.Equal(1, result[startDate.AddDays(1)]);
    }
    #endregion

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

