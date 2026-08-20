using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class BookingExportController : AdminBaseController
    {
       
        public IActionResult Export()
        {
           
            return View();
        }
    }
}