using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagementSystem.Controllers
{
    public class BranchController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly IWebHostEnvironment _environment;

        public BranchController(HotelManagementContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Branch/
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .OrderBy(b => b.BranchId)
                .ToListAsync();

            return View(branches);
        }

        // POST: /Branch/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Branch branch)
        {
            // 💡 1. 移除導覽屬性驗證（避免 ModelState 因相關連的 Rooms / RoomTypes 為 null 而無效）
            ModelState.Remove("Rooms");
            ModelState.Remove("RoomTypes");

            // 💡 2. 檢查圖片檔案是否存在 (針對相對路徑)
            if (!string.IsNullOrEmpty(branch.ImageUrl) && !branch.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                string cleanPath = branch.ImageUrl.TrimStart('~', '/', '\\');
                string physicalPath = Path.Combine(_environment.WebRootPath, cleanPath);

                if (!System.IO.File.Exists(physicalPath))
                {
                    // 格式化為與房型頁面相同的錯誤訊息格式
                    string normalizedPath = cleanPath.Replace('\\', '/');
                    ModelState.AddModelError("ImageUrl", $"伺服器找不到圖片檔案：wwwroot/{normalizedPath}");
                }
            }

            // 💡 3. 模型驗證未通過處理
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                TempData["ErrorMessage"] = "儲存失敗：" + string.Join(" | ", errors);

                var branches = await _context.Branches.AsNoTracking().OrderBy(b => b.BranchId).ToListAsync();
                return View("Index", branches);
            }

            // branch.Phone 正規化
            string phone = branch.Phone.Trim();
            phone = phone.Replace(" ", "");
            phone = phone.Replace("-", "");
            if (string.IsNullOrEmpty(phone) || !phone.All(char.IsDigit))
            {
                TempData["ErrorMessage"] = "電話只能包含數字、空白或半形連字號，修改失敗！";
                return RedirectToAction(nameof(Index));
            }
            branch.Phone = phone;

            // 💡 4. 資料庫存取與 TempData 提示訊息
            try
            {
                if (branch.BranchId == 0)
                {
                    // 【新增分館】
                    _context.Branches.Add(branch);
                    TempData["SuccessMessage"] = $"新增分館【{branch.BranchName}】成功！";
                }
                else
                {                   

                    // 【修改分館】
                    var existingBranch = await _context.Branches.FindAsync(branch.BranchId);
                    if (existingBranch != null)
                    {
                        existingBranch.BranchName = branch.BranchName;
                        existingBranch.Region = branch.Region;
                        existingBranch.Phone = branch.Phone;
                        existingBranch.Address = branch.Address;
                        existingBranch.AcceptsNewBookings = branch.AcceptsNewBookings;
                        existingBranch.ImageUrl = branch.ImageUrl;
                        existingBranch.Description = branch.Description;

                        TempData["SuccessMessage"] = $"修改分館【{branch.BranchName}】成功！";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "找不到該分館資料，修改失敗！";
                        return RedirectToAction(nameof(Index));
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "資料庫儲存失敗，請稍後再試。";
                var branches = await _context.Branches.AsNoTracking().OrderBy(b => b.BranchId).ToListAsync();
                return View("Index", branches);
            }
        }
    }
}