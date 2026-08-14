using Microsoft.AspNetCore.Mvc;


namespace HotelManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

    }
}
