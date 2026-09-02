namespace HotelManagementSystem.Tests;

using HotelManagementSystem.Services;

public class RoomAvailabilityServiceTests
{
    [Fact]
    public void CalculateDailyRemainingRooms_EndDateEqualsStartDate_ThrowsArgumentException()
    {
        var service = new RoomAvailabilityService(null!, null!);
        var date = new DateOnly(2026, 9, 10);

        Assert.Throws<ArgumentException>(() =>
            service.CalculateDailyRemainingRooms(1, date, date));

    }

}

