using HotelManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Services
{
    public class NoShowService
    {
        private readonly TaipeiClock _clock;
        private readonly HotelManagementContext _context;

        public NoShowService(TaipeiClock clock, HotelManagementContext context)
        {
            _clock = clock;
            _context = context;

        }

        private static readonly TimeOnly NoShowCutoffTime = new(12, 0);

        public async Task UpdateNoShowsAsync()
        {
            // 1. 取得 now
            var now = _clock.Now;
            // 2. 取得 today
            var today = _clock.Today;
            // 3. 判斷現在有沒有到今天 12:00
            bool isTodayOverNoShowCutoffTime = TimeOnly.FromDateTime(now) >= NoShowCutoffTime;
            // 4. 從 _context.Bookings 篩出候選訂單
            var query = _context.Bookings.Where(b => b.BookingStatus == "Paid" && b.StayRecord == null);
            //現在 >= 今日 12:00？
            //    │
            //    ├─ 是
            //    │   → CheckOutDate <= Today
            //    │
            //    └─ 否
            //        → CheckOutDate < Today
            if (isTodayOverNoShowCutoffTime)
            {
                //已經超過 12:00，CheckOutDate <= Today 的 Paid + 無 StayRecord 訂單都該 NoShow
                query = query.Where(b => b.CheckOutDate <= today);
            }
            else
            {
                //只有CheckOutDate < 2026 / 08 / 14 的才該 NoShow
                query = query.Where(b => b.CheckOutDate < today);
            }
            // 5. ToListAsync()
            var bookingsToNoShow = await query.ToListAsync();
            // 6. foreach 改 BookingStatus
            foreach (var booking in bookingsToNoShow)
            {
                booking.BookingStatus = "NoShow";
            }
            // 7. SaveChangesAsync()
            await _context.SaveChangesAsync();
        }

    }
}