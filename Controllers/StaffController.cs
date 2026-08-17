using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class StaffController : Controller
    {
        public IActionResult Employees()
        {
            return View();
        }
    }
}
