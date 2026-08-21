using HotelManagementSystem.Models;  // 引入你剛剛找到的 HotelManagementContext
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // 👈 就是少了這行，補上它 Include 就不會報錯了！
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;



namespace HotelManagementSystem.Controllers
{
    public class BookingExportController : AdminBaseController
    {
        private readonly HotelManagementContext _context;

        public BookingExportController(HotelManagementContext context)
        {
            _context = context;
        }

        public IActionResult Export()
        {
            return View();
        }

        // 3. 後端 API 查詢與 JSON 回傳端點
        [AllowAnonymous] // 👈 暫時加上這行測試，如果加上就能撈到資料，說明是登入 Session 的問題


        [HttpGet]
        public async Task<IActionResult> GetOrders(string branch, string start, string end)
        {
            try
            {
                DateOnly startDate = DateOnly.Parse(start);
                DateOnly endDate = DateOnly.Parse(end);

                // 🚀 1. 核心實作：手動使用 LINQ Join 將訂單表與分館表串聯起來！
                // 假設你們分館表的屬性叫做 _context.Branches (請根據實體名稱修改，例如 Branch 或 HotelBranch)
                var query = from booking in _context.Bookings
                            join b in _context.Branches on booking.BranchId equals b.BranchId
                            where booking.CheckInDate >= startDate && booking.CheckInDate <= endDate
                            select new { booking, b };

                // 🚀 2. 分館篩選：直接拿資料庫回傳的真實 BranchName 進行字串過濾
                if (!string.IsNullOrEmpty(branch) && branch != "全部分館")
                {
                    query = query.Where(x => x.b.BranchName == branch);
                }

                // 🚀 3. 投射出前端需要的欄位格式
                var dbData = await query.Select(x => new
                {
                    booking_no = x.booking.BookingNumber,
                    // 👉 這裡直接抓取分館資料表最真實、原始的欄位名稱！
                    branch_name = x.b.BranchName,

                    created_at = x.booking.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    guest_name = x.booking.BookerName,
                    phone = x.booking.ContactPhone,
                    email = x.booking.Email,
                    check_in = x.booking.CheckInDate.ToString("yyyy-MM-dd"),
                    check_out = x.booking.CheckOutDate.ToString("yyyy-MM-dd"),
                    nights = (x.booking.CheckOutDate.DayNumber - x.booking.CheckInDate.DayNumber),
                    room_type_snapshot = x.booking.RoomTypeNameSnapshot,
                    price_snapshot = x.booking.NightlyPriceSnapshot,
                    total_amount = x.booking.TotalAmount,
                    status = x.booking.BookingStatus,

                    cancel_cause = x.booking.CancellationCause ?? "",
                    cancel_reason = x.booking.CancellationReason ?? "",
                    cancel_emp = x.booking.CancelledByEmployeeNumber ?? "",
                    cancel_at = x.booking.CancelledAt.HasValue ? x.booking.CancelledAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",

                    refund_amount = 0,
                    room_no = "",
                    act_occupancy = 0,
                    act_checkin_at = "",
                    checkin_emp = "",
                    act_checkout_at = "",
                    checkout_emp = ""
                }).ToListAsync();

                return Json(new { success = true, data = dbData }, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}