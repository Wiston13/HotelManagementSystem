using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class RoomTypeController : Controller
    {
        // GET: /RoomType/ 或 /RoomType/Index
        // 顯示房型及固定價格管理主頁面
        [HttpGet]
        public IActionResult Index()
        {
            return View(); // 對應 Views/RoomType/Index.cshtml
        }

        // POST: /RoomType/Save
        // 處理新增或編輯房型與價格資料
        [HttpPost]
        public IActionResult Save(int roomTypeId, string typeName, int capacity, string bedType, decimal basePrice, bool isActive)
        {
            if (roomTypeId == 0)
            {
                // TODO: 執行「新增房型」資料庫邏輯
            }
            else
            {
                // TODO: 執行「修改房型與價格」資料庫邏輯
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
