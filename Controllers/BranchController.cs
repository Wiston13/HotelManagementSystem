using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class BranchController : Controller
    {
        // 1. 分館管理
        public IActionResult Branch()
        {
            return View(); 
        }

        // 2. 房型與固定價格管理
        public IActionResult Price()
        {
            return View(); 
        }

        // 3. 房間管理
        public IActionResult Rooms()
        {
            return View(); 
        }
    }
}
