using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class GroupOrderController : Controller
    {
        public IActionResult Export()
        {
            return View();
        }
    }
}
