using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }
    }
}
