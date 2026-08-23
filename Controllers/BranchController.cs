using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class BranchController : Controller
    {
        private readonly HotelManagementContext _context;

        public BranchController(HotelManagementContext context)
        {
            _context = context;
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
                var branches = await _context.Branches.AsNoTracking().ToListAsync();
                return View("Index", branches);
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