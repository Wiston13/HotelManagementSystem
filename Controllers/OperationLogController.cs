using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class OperationLogController : AdminBaseController
    {
       
        public IActionResult Search()
        {
            return View();
        }
    }
}
