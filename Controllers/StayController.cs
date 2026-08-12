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

            model.HasResult = true;
            model.BookerName = booking.BookerName;
            model.ContactPhone = booking.ContactPhone;
            model.RoomTypeName = booking.RoomTypeNameSnapshot;
            model.CheckInDate = booking.CheckInDate;
            model.CheckOutDate = booking.CheckOutDate;
            model.BookingStatus = booking.BookingStatus;

            return View(model);
        }
    }
}
