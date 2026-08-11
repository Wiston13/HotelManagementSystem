using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class BranchBookingController : Controller
    {
        public IActionResult BookingSearch()
        {
            return View();
        }
        public IActionResult BookingResult()
        {
            return View();
        }
    }
}
