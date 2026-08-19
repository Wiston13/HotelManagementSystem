using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class StaffController : Controller
    {
        [AdminAuthorize]
        public IActionResult Employees()
        {
            return View();
        }
    }
}
