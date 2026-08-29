using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class RoomTypeController : SystemAdminControllerBase
    {
        private readonly HotelManagementContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly TaipeiClock _clock;

        public RoomTypeController(HotelManagementContext context, IWebHostEnvironment environment, TaipeiClock clock) : base(context)
        {
            _context = context;
            _environment = environment;
            _clock = clock;
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
            var currentOperator = CurrentEmployeeNumber!;

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

                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        _context.RoomTypes.Add(model);
                        await _context.SaveChangesAsync();
                        OperationLog createLog = new OperationLog()
                        {
                            TargetBranchId = model.BranchId,
                            OperatedAt = _clock.Now,
                            OperatorEmployeeNumber = currentOperator,
                            OperationTypeId = 5,
                            TargetType = "RoomType",
                            TargetIdentifier = model.RoomTypeName,
                            Description = $"新增房型：{model.RoomTypeName}"
                        };
                        _context.OperationLogs.Add(createLog);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = "新增房型成功！";
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                else
                {

                    var existingRoomType = await _context.RoomTypes.FindAsync(model.RoomTypeId);
                    if (existingRoomType == null)
                    {
                        TempData["ErrorMessage"] = "找不到該房型資料。";
                        return RedirectToAction(nameof(Index));
                    }

                    bool isOtherChanged = existingRoomType.RoomTypeName != model.RoomTypeName ||
                                          existingRoomType.MaxOccupancy != model.MaxOccupancy ||
                                          existingRoomType.BedType != model.BedType ||
                                          existingRoomType.NightlyPrice != model.NightlyPrice ||
                                          existingRoomType.ImageUrl != model.ImageUrl ||
                                          existingRoomType.Description != model.Description;
                    bool isActiveChanged = existingRoomType.IsActive != model.IsActive;
                    bool isNameChanged = existingRoomType.RoomTypeName != model.RoomTypeName;
                    if (isOtherChanged)
                    {

                        OperationLog updatedLog = new OperationLog()
                        {
                            TargetBranchId = existingRoomType.BranchId,
                            OperatedAt = _clock.Now,
                            OperatorEmployeeNumber = currentOperator,
                            OperationTypeId = 6,
                            TargetType = "RoomType",
                            TargetIdentifier = model.RoomTypeName,
                            Description = isNameChanged
                                ? $"修改房型名稱：{existingRoomType.RoomTypeName} → {model.RoomTypeName}。"
                                : $"修改房型：{existingRoomType.RoomTypeName}。"
                        };
                        _context.OperationLogs.Add(updatedLog);
                    }
                    if (isActiveChanged)
                    {
                        string action = model.IsActive ? "啟用" : "停用";
                        OperationLog roomActiveLog = new OperationLog()
                        {
                            TargetBranchId = existingRoomType.BranchId,
                            OperatedAt = _clock.Now,
                            OperatorEmployeeNumber = currentOperator,
                            OperationTypeId = model.IsActive ? 8 : 7,
                            TargetType = "RoomType",
                            TargetIdentifier = model.RoomTypeName,
                            Description = $"{action}房型：{model.RoomTypeName}。"
                        };
                        _context.OperationLogs.Add(roomActiveLog);
                    }
                    existingRoomType.RoomTypeName = model.RoomTypeName;
                    existingRoomType.MaxOccupancy = model.MaxOccupancy;
                    existingRoomType.BedType = model.BedType;
                    existingRoomType.NightlyPrice = model.NightlyPrice;
                    existingRoomType.IsActive = model.IsActive;
                    existingRoomType.ImageUrl = model.ImageUrl;
                    existingRoomType.Description = model.Description;


                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "修改房型成功！";

                }

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
