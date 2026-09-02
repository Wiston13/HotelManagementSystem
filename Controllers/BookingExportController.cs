using HotelManagementSystem.Models;
using HotelManagementSystem.Services;
using HotelManagementSystem.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class BookingExportController : SystemAdminControllerBase
    {
        private readonly HotelManagementContext _context;
        private readonly NoShowService _noShowService;

        public BookingExportController(HotelManagementContext context, NoShowService noShowService)
            : base(context)
        {
            _context = context;
            _noShowService = noShowService;
        }

        public async Task<IActionResult> Export()
        {

            ViewBag.Branches = await _context.Branches
                .OrderBy(b => b.BranchId)
                .ToListAsync();

            return View();
        }

        [HttpGet]

        public async Task<IActionResult> GetOrders(int? branchId, string start, string end)
        {
            try
            {
                if (!DateOnly.TryParseExact(start, "yyyy-MM-dd", out var startDate) ||
                    !DateOnly.TryParseExact(end, "yyyy-MM-dd", out var endDate) ||
                    startDate > endDate)
                {
                    return Json(new { success = false, message = "日期區間格式不正確。" });
                }

                await _noShowService.UpdateNoShowsAsync();

                var query = from booking in _context.Bookings
                            join b in _context.Branches on booking.BranchId equals b.BranchId
                            where booking.CheckInDate >= startDate && booking.CheckInDate <= endDate
                            select new { booking, b };

                if (branchId.HasValue)
                {
                    query = query.Where(x => x.booking.BranchId == branchId.Value);
                }

                var dbData = await query.Select(x => new
                {
                    booking_no = x.booking.BookingNumber,
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
                    status = StatusDisplayHelper.GetBookingStatusText(x.booking.BookingStatus),

                    cancel_cause = x.booking.CancellationCause == "GuestRequest"
                        ? "顧客因素"
                        : x.booking.CancellationCause == "HotelUnableToFulfill"
                            ? "飯店因素"
                            : x.booking.CancellationCause ?? "",
                    cancel_reason = x.booking.CancellationReason ?? "",
                    cancel_emp = x.booking.CancelledByEmployeeNumber ?? "",
                    cancel_at = x.booking.CancelledAt.HasValue ? x.booking.CancelledAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",

                    refund_amount = x.booking.BookingStatus == "Cancelled" ? x.booking.TotalAmount : 0,
                    room_no = x.booking.StayRecord != null ? x.booking.StayRecord.RoomNumberSnapshot : "",
                    act_occupancy = x.booking.StayRecord != null ? x.booking.StayRecord.ActualGuestCount : 0,
                    act_checkin_at = x.booking.StayRecord != null ? x.booking.StayRecord.ActualCheckInAt.ToString("yyyy-MM-dd HH:mm:ss") : "",
                    checkin_emp = x.booking.StayRecord != null ? x.booking.StayRecord.CheckedInByEmployeeNumber : "",
                    act_checkout_at = x.booking.StayRecord != null && x.booking.StayRecord.ActualCheckOutAt.HasValue ? x.booking.StayRecord.ActualCheckOutAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                    checkout_emp = x.booking.StayRecord != null ? x.booking.StayRecord.CheckedOutByEmployeeNumber : ""
                }).ToListAsync();

                return Json(new { success = true, data = dbData });
            }
            catch (Exception)
            {

                return Json(new { success = false, message = "查詢失敗，請稍後再試。" });
            }
        }
    }
}
