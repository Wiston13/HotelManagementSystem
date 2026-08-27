using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    [Authorize(Roles = "SystemAdmin")]
    public class BranchController : Controller
    {
        // GET: /Branch/ 或 /Branch/Index
        // 顯示分館管理主頁面
        [HttpGet]
        public IActionResult Index()
        {
            return View(); // 對應 Views/Branch/Index.cshtml
        }

        // POST: /Branch/Save
        // 處理新增或編輯分館資料
        [HttpPost]
        public IActionResult Save(int branchId, string branchName, string? region, string phone, string address, bool acceptsNewBookings, string? imageUrl, string? description)
        {
            if (branchId == 0)
            {
                // TODO: 執行「新增分館」資料庫邏輯
            }
            else
            {
                // TODO: 執行「修改分館」資料庫邏輯
            }

            // 儲存完成後重導向回主頁面
            return RedirectToAction(nameof(Index));
        }
    }
}
