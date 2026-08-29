using HotelManagementSystem.Helper;
using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class BranchController : SystemAdminControllerBase
    {
        private readonly HotelManagementContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly TaipeiClock _clock;

        public BranchController(HotelManagementContext context, IWebHostEnvironment environment, TaipeiClock taipeiClock) : base(context)
        {
            _context = context;
            _environment = environment;
            _clock = taipeiClock;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .OrderBy(b => b.BranchId)
                .ToListAsync();

            return View(branches);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Branch branch)
        {
            var currentOperator = CurrentEmployeeNumber!;

            // 前端不提供導覽屬性，避免其驗證造成 ModelState 無效。
            ModelState.Remove("Rooms");
            ModelState.Remove("RoomTypes");

            if (!string.IsNullOrEmpty(branch.ImageUrl) && !branch.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                string cleanPath = branch.ImageUrl.TrimStart('~', '/', '\\');
                string physicalPath = Path.Combine(_environment.WebRootPath, cleanPath);

                if (!System.IO.File.Exists(physicalPath))
                {
                    string normalizedPath = cleanPath.Replace('\\', '/');
                    ModelState.AddModelError("ImageUrl", $"伺服器找不到圖片檔案：wwwroot/{normalizedPath}");
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                TempData["ErrorMessage"] = "儲存失敗：" + string.Join(" | ", errors);

                var branches = await _context.Branches.AsNoTracking().OrderBy(b => b.BranchId).ToListAsync();
                return View("Index", branches);
            }

            if (!PhoneHelper.TryNormalize(branch.Phone, out var normalizedPhone))
            {
                TempData["ErrorMessage"] = "電話格式不正確，移除空白與半形連字號後須為 1～20 碼半形數字。";
                return RedirectToAction(nameof(Index));
            }
            branch.Phone = normalizedPhone;

            OperationLog bookingStatusLog = new OperationLog();
            int isBookingOpenOrStopped = 0;

            try
            {
                if (branch.BranchId == 0)
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        _context.Branches.Add(branch);
                        await _context.SaveChangesAsync();

                        bookingStatusLog.TargetBranchId = branch.BranchId;
                        bookingStatusLog.OperatedAt = _clock.Now;
                        bookingStatusLog.OperatorEmployeeNumber = currentOperator;
                        bookingStatusLog.OperationTypeId = 1;
                        bookingStatusLog.TargetType = "Branch";
                        bookingStatusLog.TargetIdentifier = branch.BranchName;
                        bookingStatusLog.Description = $"建立{branch.BranchName}商旅";

                        _context.Add(bookingStatusLog);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = $"新增分館【{branch.BranchName}】成功！";
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                else
                {
                    var existingBranch = await _context.Branches.FindAsync(branch.BranchId);

                    if (existingBranch == null)
                    {
                        TempData["ErrorMessage"] = "找不到該分館資料，修改失敗！";
                        return RedirectToAction(nameof(Index));
                    }

                    bool hasGeneralChanges =
                    existingBranch.BranchName != branch.BranchName ||
                    existingBranch.Region != branch.Region ||
                    existingBranch.Phone != branch.Phone ||
                    existingBranch.Address != branch.Address ||
                    existingBranch.ImageUrl != branch.ImageUrl ||
                    existingBranch.Description != branch.Description;
                    bool bookingStatusChanged = existingBranch.AcceptsNewBookings != branch.AcceptsNewBookings;

                    existingBranch.BranchName = branch.BranchName;
                    existingBranch.Region = branch.Region;
                    existingBranch.Phone = branch.Phone;
                    existingBranch.Address = branch.Address;
                    if (bookingStatusChanged)
                    {
                        if (existingBranch.AcceptsNewBookings == false)
                        {
                            isBookingOpenOrStopped = 3;
                        }
                        else
                        {
                            isBookingOpenOrStopped = 4;
                        }
                    }

                    existingBranch.AcceptsNewBookings = branch.AcceptsNewBookings;

                    if (hasGeneralChanges)
                    {

                        existingBranch.ImageUrl = branch.ImageUrl;
                        existingBranch.Description = branch.Description;

                        TempData["SuccessMessage"] = $"修改分館【{branch.BranchName}】成功！";

                        bookingStatusLog.TargetBranchId = branch.BranchId;
                        bookingStatusLog.OperatedAt = _clock.Now;
                        bookingStatusLog.OperatorEmployeeNumber = currentOperator;
                        bookingStatusLog.OperationTypeId = 2;
                        bookingStatusLog.TargetType = "Branch";
                        bookingStatusLog.TargetIdentifier = branch.BranchName;
                        bookingStatusLog.Description = $"修改{branch.BranchName}商旅資料";
                        _context.Add(bookingStatusLog);
                    }
                }
                if (isBookingOpenOrStopped != 0)
                {
                    string temp = isBookingOpenOrStopped == 3 ? "開放接受" : "停止接受";
                    var updateLog = new OperationLog()
                    {
                        TargetBranchId = branch.BranchId,
                        OperatedAt = _clock.Now,
                        OperatorEmployeeNumber = currentOperator,
                        OperationTypeId = isBookingOpenOrStopped,
                        TargetType = "Branch",
                        TargetIdentifier = branch.BranchName,
                        Description = $"修改{branch.BranchName}商旅資料 將{branch.BranchName}商旅設定為{temp}新訂房。"
                    };

                    _context.Add(updateLog);
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
