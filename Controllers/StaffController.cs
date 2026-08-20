using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class StaffController : AdminBaseController
    {
       
        public IActionResult Employees()
        {
            Console.WriteLine(HttpContext.Session.GetString("UserRole"));
            Console.WriteLine(HttpContext.Session.GetString("UserName"));
            Console.WriteLine(HttpContext.Session.GetString("BranchId"));
            return View();
        }
    }
}
