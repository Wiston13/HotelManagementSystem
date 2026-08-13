using HotelManagementSystem.Models;
using HotelManagementSystem.Models.ViewModels.Stay;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class StayController : Controller
    {
        private readonly HotelManagementContext _context;

        public StayController(HotelManagementContext context)
        {
            _context = context;
        }

        public IActionResult CheckOut()
        {
            return View();
        }

        public IActionResult CheckIn(string? bookingNumber)
        {
            var model = new CheckInViewModel
            {
                BookingNumber = bookingNumber
            };

            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                return View(model);
            }

            var booking = _context.Bookings.FirstOrDefault(b => b.BookingNumber == bookingNumber);

            if (booking == null)
            {
                model.ErrorMessage = "查無此訂單";
                return View(model);
            }

            if (booking.BookingStatus != "Paid")
            {
                model.CanCheckIn = false;
                model.CheckInBlockedReason = "此訂單目前無法辦理入住";
                return View(model);
            }

            var checkInStart = booking.CheckInDate.ToDateTime(new TimeOnly(16, 0));
            var checkOutDeadline = booking.CheckOutDate.ToDateTime(new TimeOnly(12, 0));
            var now = DateTime.Now;

            if (now < checkInStart)
            {
                model.CheckInBlockedReason = "尚未到可辦理入住時間";
                return View(model);
            }

            if (now >= checkOutDeadline)
            {
                model.CheckInBlockedReason = "此訂單已超過可辦理入住時間";
                return View(model);
            }

            var hasStayRecord = _context.StayRecords.Any(s => s.BookingNumber == booking.BookingNumber);

            if (hasStayRecord)
            {
                model.CheckInBlockedReason = "此訂單已建立住房紀錄";
                return View(model);
            }

            model.CanCheckIn = true;

            if (model.CanCheckIn)
            {
                model.HasResult = true;
                model.BookerName = booking.BookerName;
                model.ContactPhone = booking.ContactPhone;
                model.RoomTypeName = booking.RoomTypeNameSnapshot;
                model.CheckInDate = booking.CheckInDate;
                model.CheckOutDate = booking.CheckOutDate;
                model.BookingStatus = booking.BookingStatus;
            }

            return View(model);
        }
    }
}
