using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class RoomController : Controller
    {
        // GET: /Room/ 或 /Room/Index
        // 顯示房間管理主頁面
        [HttpGet]
        public IActionResult Index()
        {
            return View(); // 對應 Views/Room/Index.cshtml
        }

        // POST: /Room/Save
        // 處理新增或編輯房間資料（樓層為純數字 int）
        [HttpPost]
        public IActionResult Save(int roomId, string roomNumber, string roomType, int floor, bool isActive)
        {
            if (roomId == 0)
            {
                // TODO: 執行「新增房間」資料庫邏輯
            }
            else
            {
                // TODO: 執行「修改房間」資料庫邏輯
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
