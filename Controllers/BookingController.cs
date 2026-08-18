using Microsoft.AspNetCore.Mvc;
using HotelManagementSystem.Models;
using HotelManagementSystem.Models.BookingSearchModel;


namespace HotelManagementSystem.Controllers
{
    public class BookingController : Controller
    {
        private readonly HotelManagementContext _context;

        public BookingController(HotelManagementContext context)
        {
            _context = context;
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

        [HttpPost]
        public async Task<IActionResult> Lookup(string BookingNum,string Phone)
        {


            return View();
        }
    }
}
