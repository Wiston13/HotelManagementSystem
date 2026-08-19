using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class EmployeeHomeController : Controller
    {
        [BranchAuthorize]
        public IActionResult Index()
        {
            return View();
        }
    }
}
