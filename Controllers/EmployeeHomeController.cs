using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class EmployeeHomeController : Controller
    {
   
        public IActionResult Index()
        {
            return View();
        }
    }
}
