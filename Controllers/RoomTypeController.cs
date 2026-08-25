using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting; // 必須引入此命名空間以使用 IWebHostEnvironment
using System.IO;
using System.Threading.Tasks;
using HotelManagementSystem.Models; // 請替換為專案正確的 DbContext 命名空間
using HotelManagementSystem.Models.Entities; // 請替換為實體類別命名空間

namespace HotelManagementSystem.Controllers
{
    public class RoomTypeController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly IWebHostEnvironment _environment;

        // 注入 DbContext 與 IWebHostEnvironment
        public RoomTypeController(HotelManagementContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 撈出分館清單傳給選單
            ViewBag.Branches = await _context.Branches.ToListAsync();

            // 撈出所有房型資料傳給頁面
            var roomTypes = await _context.RoomTypes.ToListAsync();
            return View(roomTypes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(RoomType model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Branches = await _context.Branches.ToListAsync();
                return View("Index", await _context.RoomTypes.ToListAsync());
            }

            // 後端第二重防護：若為本地相對路徑 (/images/...)，檢查 wwwroot 實體檔案是否存在
            if (!string.IsNullOrEmpty(model.ImageUrl) && model.ImageUrl.StartsWith("/"))
            {
                string relativePath = model.ImageUrl.TrimStart('/');
                string physicalPath = Path.Combine(_environment.WebRootPath, relativePath);

                if (!System.IO.File.Exists(physicalPath))
                {
                    // 若伺服器找不到檔案，可記錄或阻擋（前端已做過第一層防護，這裡做雙重保險）
                    ModelState.AddModelError("ImageUrl", "伺服器的 wwwroot 目錄中找不到該圖片檔案！");
                    ViewBag.Branches = await _context.Branches.ToListAsync();
                    return View("Index", await _context.RoomTypes.ToListAsync());
                }
            }

            if (model.RoomTypeId == 0)
            {
                // 新增房型
                _context.RoomTypes.Add(model);
            }
            else
            {
                // 修改房型
                _context.RoomTypes.Update(model);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}