using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class BookingController : Controller
    {
        public IActionResult RoomSelection()
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
            return View();
        }
    }
}
