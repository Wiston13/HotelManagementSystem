using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class BookingExportController : Controller
    {
        public IActionResult Export()
        {
            return View();
        }
    }
}