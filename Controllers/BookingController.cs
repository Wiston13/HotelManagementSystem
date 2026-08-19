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
            var model = new BookingData
            {
                BookingNum = BookingNum
            };

            if (string.IsNullOrWhiteSpace(BookingNum))
            {
                return View(model);
            }

            var booking = _context.Bookings.FirstOrDefault(b => b.BookingNumber == BookingNum);

            if (booking == null)
            {               
                return View();
            }

            if (booking.BookingStatus != "Paid")
            {                
                return View();
            }

            var checkInStart = booking.CheckInDate.ToDateTime(new TimeOnly(16, 0));
            var checkOutDeadline = booking.CheckOutDate.ToDateTime(new TimeOnly(12, 0));
            var now = _Clock.Now;

            if (now < checkInStart)
            {
                return View();
            }

            if (now >= checkOutDeadline)
            {
                return View();
            }

            var hasStayRecord = _context.StayRecords.Any(s => s.BookingNumber == booking.BookingNumber);

            if (hasStayRecord)
            {
                return View();
            }          

            return View(model);

        }
    }
}