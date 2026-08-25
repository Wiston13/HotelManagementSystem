using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Hosting; // 必須引入以使用 IWebHostEnvironment
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagementSystem.Controllers
{
    public class BranchController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly IWebHostEnvironment _environment;

        // 注入 DbContext 與 IWebHostEnvironment
        public BranchController(HotelManagementContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Branch/
        // 從資料庫查出所有分館，並傳給 View 進行渲染
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .OrderBy(b => b.BranchId)
                .ToListAsync();

            return View(branches); // 將 Model 傳遞給 Index.cshtml
        }

        // POST: /Branch/Save
        // 處理 Modal 表單送出的「新增」或「編輯」
        [HttpPost]
        [ValidateAntiForgeryToken] // 提升表單安全性，防止 CSRF 攻擊
        public async Task<IActionResult> Save(Branch branch)
        {
            if (!ModelState.IsValid)
            {
                // 如果驗證沒過，重新載入 Index 畫面
                var branches = await _context.Branches.AsNoTracking().OrderBy(b => b.BranchId).ToListAsync();
                return View("Index", branches);
            }

            // 後端第二重防護：若填寫的是本地相對路徑 (/images/...)，檢查 wwwroot 實體檔案是否存在
            if (!string.IsNullOrEmpty(branch.ImageUrl) && branch.ImageUrl.StartsWith("/"))
            {
                string relativePath = branch.ImageUrl.TrimStart('/');
                string physicalPath = Path.Combine(_environment.WebRootPath, relativePath);

                if (!System.IO.File.Exists(physicalPath))
                {
                    ModelState.AddModelError("ImageUrl", "伺服器的 wwwroot 目錄中找不到該圖片檔案！");
                    var branches = await _context.Branches.AsNoTracking().OrderBy(b => b.BranchId).ToListAsync();
                    return View("Index", branches);
                }
            }

            if (branch.BranchId == 0)
            {
                // 【新增分館】
                _context.Branches.Add(branch);
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
                }
            }

            // 儲存至 SQL Server 資料庫
            await _context.SaveChangesAsync();

            // 儲存完成後重導向回 Index 頁面重新整理資料
            return RedirectToAction(nameof(Index));
        }
    }
}