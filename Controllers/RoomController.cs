using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels;

namespace HotelManagementSystem.Controllers
{
    public class RoomController : Controller
    {
        private readonly HotelManagementContext _context;

        public RoomController(HotelManagementContext context)
        {
            _context = context;
        }

        // GET: Room/Index
        public async Task<IActionResult> Index()
        {
            var viewModel = new RoomIndexViewModel
            {
                Branches = await _context.Branches.AsNoTracking().ToListAsync(),
                RoomTypes = await _context.RoomTypes.AsNoTracking().ToListAsync(),
                Rooms = await _context.Rooms
                    .Include(r => r.RoomType)
                    .AsNoTracking()
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Room/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Room model)
        {
            // 基本 Model 驗證（防空值或型態不符）
            if (!ModelState.IsValid)
            {
                string errorMsg = "資料驗證失敗，請檢查輸入欄位。";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(errorMsg);
                }
                TempData["ErrorMessage"] = errorMsg;
                return RedirectToAction(nameof(Index));
            }

            // ==========================================
            // 情況一：新增房間 (RoomId == 0)
            // ==========================================
            if (model.RoomId == 0)
            {
                // 1. 驗證傳入的分館是否存在
                bool branchExists = await _context.Branches.AnyAsync(b => b.BranchId == model.BranchId);
                if (!branchExists)
                {
                    string errorMsg = $"新增失敗：指定的分館 ID ({model.BranchId}) 不存在！";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return BadRequest(errorMsg);
                    }
                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(Index));
                }

                // 2. 驗證 RoomTypeId 是否屬於該指定分館
                var roomType = await _context.RoomTypes
                    .FirstOrDefaultAsync(rt => rt.RoomTypeId == model.RoomTypeId && rt.BranchId == model.BranchId);

                if (roomType == null)
                {
                    string errorMsg = "新增失敗：所選房型不屬於指定分館！";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return BadRequest(errorMsg);
                    }
                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(Index));
                }

                // 3. 通過驗證，新增房間
                var newRoom = new Room
                {
                    BranchId = model.BranchId,
                    RoomTypeId = model.RoomTypeId,
                    RoomNumber = model.RoomNumber,
                    Floor = model.Floor,
                    SupplyStatus = model.SupplyStatus ?? "Open",
                    DisabledReason = model.SupplyStatus == "Disabled" ? model.DisabledReason : null
                };

                _context.Rooms.Add(newRoom);
                TempData["SuccessMessage"] = $"新增房間【{model.RoomNumber}】成功！";
            }
            // ==========================================
            // 情況二：編輯房間 (RoomId > 0)
            // ==========================================
            else
            {
                // 1. 尋找既有房間資料
                var existingRoom = await _context.Rooms.FindAsync(model.RoomId);
                if (existingRoom == null)
                {
                    string errorMsg = "找不到欲更新的房間資料！";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return BadRequest(errorMsg);
                    }
                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(Index));
                }

                // 2. 不使用 POST 傳入的 BranchId，改用資料庫現有的 existingRoom.BranchId 檢查 RoomType
                var targetRoomType = await _context.RoomTypes
                    .FirstOrDefaultAsync(rt => rt.RoomTypeId == model.RoomTypeId && rt.BranchId == existingRoom.BranchId);

                if (targetRoomType == null)
                {
                    string errorMsg = "修改失敗：所選房型不屬於該房間現有之分館！";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return BadRequest(errorMsg);
                    }
                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(Index));
                }

                // 3. 通過驗證，寫回更新（保持原 BranchId 不變，僅更新允許修改的欄位）
                existingRoom.RoomTypeId = model.RoomTypeId;
                existingRoom.RoomNumber = model.RoomNumber;
                existingRoom.Floor = model.Floor;
                existingRoom.SupplyStatus = model.SupplyStatus ?? existingRoom.SupplyStatus;
                existingRoom.DisabledReason = model.SupplyStatus == "Disabled" ? model.DisabledReason : null;

                _context.Rooms.Update(existingRoom);
                TempData["SuccessMessage"] = $"修改房間【{model.RoomNumber}】成功！";
            }

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(new { success = true });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}