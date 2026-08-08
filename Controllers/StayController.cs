using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class StayController : Controller
    {
        public IActionResult CheckIn()
        {
            return View();
        }
        public IActionResult CheckOut()
        {
            return View();
        }
    }
}
