using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class BookingExportController : Controller
    {
        [AdminAuthorize]
        public IActionResult Export()
        {
            var  HttpContext.Session.GetString("UserRole"); // 這行是為了觸發 Session 的讀取，確保 Session 可用
            return View();
        }
    }
}