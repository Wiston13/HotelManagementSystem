using Microsoft.AspNetCore.Mvc;
using HotelManagementSystem.Services;
using HotelManagementSystem.Models;
using HotelManagementSystem.Models.BookingSearchModel;


namespace HotelManagementSystem.Controllers
{
    public class BookingController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly TaipeiClock _Clock;

        public BookingController(HotelManagementContext context,TaipeiClock clock)
        {
            _context = context;
            _Clock = clock;
        }
        

        [HttpGet]
        public IActionResult RoomSelection(string branchName, DateTime checkIn, DateTime checkOut, int guests)
        {            
            return View();
        }

        public IActionResult Payment()
        {
            return View();
        }

        public IActionResult Success()
        {
            return View();
        }

        
        public IActionResult Lookup()
        {
            List<BookingData> _bookingData = new List<BookingData>();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Lookup(string BookingNum,string Phone)
        {
            var model = new BookingData();

            if (string.IsNullOrWhiteSpace(BookingNum))
            {
                return View();
            }

            var booking = _context.Bookings.FirstOrDefault(b => b.BookingNumber == BookingNum &&b.ContactPhone==Phone);

            if (booking == null)
            {               
                return View();
            }
            model.BookingNum = booking.BookingNumber;
            model.Phone = booking.ContactPhone;
            //分館
            model.Roomtype = booking.RoomTypeNameSnapshot;
            model.StartDate = new DateTime(booking.CheckInDate.Year, booking.CheckInDate.Month, booking.CheckInDate.Day);
            model.EndDate = new DateTime(booking.CheckOutDate.Year, booking.CheckOutDate.Month, booking.CheckOutDate.Day); 
            model.BookingDate = booking.CreatedAt;
            model.Name = booking.BookerName;
            model.Price = booking.NightlyPriceSnapshot.ToString("N0");
            model.BookingStatus = booking.BookingStatus;
            return View(model);

        }
    }
}