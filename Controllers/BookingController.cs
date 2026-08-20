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



        private string StatusToChenese(string input)
        {
            string output = "";
            switch (input)
            {
                case "Completed":
                    output = "已完成";
                    break;
                case "CheckedIn":
                    output = "入住中";
                    break;
                case "Cancelled":
                    output = "已取消";
                    break;
                case "NoShow":
                    output = "逾期未入住";
                    break;
                case "Paid":
                    output = "已付款";
                    break;
            }
            return output;
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

            if (string.IsNullOrWhiteSpace(BookingNum)||string.IsNullOrWhiteSpace(Phone))
            {
                return View(model);
            }

            var booking = _context.Bookings.FirstOrDefault(b => b.BookingNumber == BookingNum &&b.ContactPhone==Phone);

            if (booking == null)
            {
                model.BookingNum = BookingNum;
                model.Phone = Phone;
                ViewBag.NoResult = true;
                return View(model);
            }

            var branch = _context.Branches.FirstOrDefault(b => b.BranchId == booking!.BranchId);

            model.BookingNum = booking.BookingNumber;
            model.Phone = booking.ContactPhone;
            model.BranchName = branch!.BranchName;
            model.Roomtype = booking.RoomTypeNameSnapshot;
            model.StartDate = new DateTime(booking.CheckInDate.Year, booking.CheckInDate.Month, booking.CheckInDate.Day);
            model.EndDate = new DateTime(booking.CheckOutDate.Year, booking.CheckOutDate.Month, booking.CheckOutDate.Day); 
            model.BookingDate = booking.CreatedAt;
            model.Name = booking.BookerName;
            model.Price = booking.NightlyPriceSnapshot.ToString("N0");
            model.BookingStatus = StatusToChenese(booking.BookingStatus);
            return View(model);

        }
    }
}