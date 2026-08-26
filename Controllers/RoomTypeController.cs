using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagementSystem.Controllers
{
    public class RoomTypeController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly IWebHostEnvironment _environment;

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
            //  1. 清除 EF Core 導覽屬性的驗證錯誤（解決 ModelState 永遠無效的主因）
            ModelState.Remove("Branch");
            ModelState.Remove("Rooms");
            ModelState.Remove("Bookings");

            //  2. 彈性檢查本地圖片檔案是否存在（若為相對路徑）
            if (!string.IsNullOrEmpty(model.ImageUrl) && !model.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                // 清理路徑開頭的 '~', '/', '\'
                string cleanPath = model.ImageUrl.TrimStart('~', '/', '\\');
                string physicalPath = Path.Combine(_environment.WebRootPath, cleanPath);

                if (!System.IO.File.Exists(physicalPath))
                {
                    ModelState.AddModelError("ImageUrl", $"伺服器找不到圖片檔案：wwwroot/{cleanPath}");
                }
            }

            //  3. 模型驗證未通過處理
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                TempData["ErrorMessage"] = "儲存失敗：" + string.Join(" | ", errors);

                ViewBag.Branches = await _context.Branches.ToListAsync();
                return View("Index", await _context.RoomTypes.ToListAsync());
            }


           
            //  4. 資料庫存取與例外處理
            try
            {
                if (model.RoomTypeId == 0)
                {
                    var branchExists = await _context.Branches
                    .AnyAsync(b => b.BranchId == model.BranchId);

                    if (!branchExists)
                    {
                        TempData["ErrorMessage"] = "找不到指定的分館資料。";
                        return RedirectToAction(nameof(Index));
                    }

                    _context.RoomTypes.Add(model);
                    TempData["SuccessMessage"] = "新增房型成功！";
                }
                else
                {
                    var existingRoomType = await _context.RoomTypes.FindAsync(model.RoomTypeId);
                    if (existingRoomType == null)
                    {
                        TempData["ErrorMessage"] = "找不到該房型資料。";
                        return RedirectToAction(nameof(Index));
                    }
                    existingRoomType.RoomTypeName = model.RoomTypeName;
                    existingRoomType.MaxOccupancy = model.MaxOccupancy;
                    existingRoomType.BedType = model.BedType;
                    existingRoomType.NightlyPrice = model.NightlyPrice;
                    existingRoomType.IsActive = model.IsActive;
                    existingRoomType.ImageUrl = model.ImageUrl;
                    existingRoomType.Description = model.Description;
                    TempData["SuccessMessage"] = "修改房型成功！";
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "資料庫儲存失敗";
                ViewBag.Branches = await _context.Branches.ToListAsync();
                return View("Index", await _context.RoomTypes.ToListAsync());
            }
        }
    }
}