using HotelManagementSystem.Models;

namespace HotelManagementSystem.Services
{
    public class RoomAvailabilityService
    {
        private readonly HotelManagementContext _context;
        private readonly TaipeiClock _taipeiClock;
        public RoomAvailabilityService(TaipeiClock taipeiClock, HotelManagementContext context)
        {
            _taipeiClock = taipeiClock;
            _context = context;
        }

        public Dictionary<DateOnly, int> CalculateDailyRemainingRooms(int roomTypeId, DateOnly startDate, DateOnly endDate)
        {
            var now = _taipeiClock.Now;
            var today = _taipeiClock.Today;
            var dailyRemainingRooms = new Dictionary<DateOnly, int>();

            // 飯店退房時間為 12:00
            var checkOutTime = new TimeSpan(12, 0, 0);

            // 判斷目前時間是否已超過退房時間
            var isAfterCheckOutTime = now.TimeOfDay >= checkOutTime;

            // 取得可售房量（該房型 供應狀態為 Open 的房間總數）
            var availableCount = _context.Rooms
                .Count(r =>
                r.RoomTypeId == roomTypeId &&
                r.SupplyStatus == "Open");

            // 日期重疊的有效訂單
            var bookings = _context.Bookings
                .Where(b =>
                    b.RoomTypeId == roomTypeId &&
                    b.CheckInDate < endDate && b.CheckOutDate > startDate &&
                    (b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn")
                ).ToList();

            for (var date = startDate; date < endDate; date = date.AddDays(1))
            {
                // 該晚正常有效訂單占用數量
                var bookingCount = bookings
                    .Count(b =>
                        b.CheckInDate <= date &&
                        b.CheckOutDate > date);

                // 該晚剩餘房量
                var remaining = availableCount - bookingCount;

                // 扣除逾期未退房房量
                if (date == today)
                {
                    var overdueCount = _context.StayRecords
                        .Count(s =>
                            s.ActualCheckOutAt == null &&
                            s.Room.RoomTypeId == roomTypeId &&
                            (
                                s.BookingNumberNavigation.CheckOutDate < today ||
                                (s.BookingNumberNavigation.CheckOutDate == today &&
                                 isAfterCheckOutTime)
                            ));

                    remaining -= overdueCount;
                }
                dailyRemainingRooms[date] = remaining;
            }

            return dailyRemainingRooms;
        }

        public int CalculateMinimumRemainingRooms(int roomTypeId, DateOnly startDate, DateOnly endDate)
        {
            var minimumRemainingCount = CalculateDailyRemainingRooms(roomTypeId, startDate, endDate).Values.Min();

            return minimumRemainingCount;
        }

        public Dictionary<DateOnly, int> FindCapacityShortages(int roomTypeId, DateOnly startDate, DateOnly endDate, int supplyReduction)
        {
            var dailyRemainingRooms = CalculateDailyRemainingRooms(roomTypeId, startDate, endDate);

            var shortages = new Dictionary<DateOnly, int>();

            for (var date = startDate; date < endDate; date = date.AddDays(1))
            {
                var remainingAfterReduction =
                    dailyRemainingRooms[date] - supplyReduction;

                if (remainingAfterReduction < 0)
                {
                    shortages[date] = Math.Abs(remainingAfterReduction);
                }
            }

            return shortages;
        }
    }
}
