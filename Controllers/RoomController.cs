using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagementSystem.Controllers
{
    public class RoomController : Controller
    {
        private readonly HotelManagementContext _context;

        public RoomController(HotelManagementContext context)
        {
            _context = context;
        }

        // GET: /Room/
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var viewModel = new RoomIndexViewModel
            {
                Branches = await _context.Branches
                    .AsNoTracking()
                    .OrderBy(b => b.BranchId)
                    .ToListAsync(),

                RoomTypes = await _context.RoomTypes
                    .AsNoTracking()
                    .OrderBy(rt => rt.RoomTypeId)
                    .ToListAsync(),

                Rooms = await _context.Rooms
                    .Include(r => r.RoomType) // 僅保留實體中存在的 RoomType 導覽屬性
                    .AsNoTracking()
                    .OrderBy(r => r.BranchId)
                    .ThenBy(r => r.RoomNumber)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: /Room/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            int RoomId,
            int BranchId,
            string RoomNumber,
            int RoomTypeId,
            short Floor, // 配合 Room.cs 型別調整為 short
            string SupplyStatus,
            string? DisabledReason)
        {
            // 防呆檢查：基本必填
            if (string.IsNullOrWhiteSpace(RoomNumber) || RoomTypeId <= 0 || BranchId <= 0)
            {
                TempData["ErrorMessage"] = "請填寫所有必填欄位！";
                return RedirectToAction(nameof(Index));
            }

            // 防呆檢查：如果選停用，必須有理由
            if (SupplyStatus == "Disabled" && string.IsNullOrWhiteSpace(DisabledReason))
            {
                TempData["ErrorMessage"] = "停用房間時必須填寫停用原因！";
                return RedirectToAction(nameof(Index));
            }

            // 防呆檢查：同分館內房號不能重複 (忽略自己的 RoomId)
            bool isDuplicate = await _context.Rooms.AnyAsync(r =>
                r.BranchId == BranchId &&
                r.RoomNumber == RoomNumber.Trim() &&
                r.RoomId != RoomId);

            if (isDuplicate)
            {
                TempData["ErrorMessage"] = $"該分館內已有房號【{RoomNumber}】，請勿重複新增/修改！";
                return RedirectToAction(nameof(Index));
            }

            if (RoomId == 0)
            {
                // 【新增房間】
                var newRoom = new Room
                {
                    BranchId = BranchId,
                    RoomNumber = RoomNumber.Trim(),
                    RoomTypeId = RoomTypeId,
                    Floor = Floor,
                    SupplyStatus = SupplyStatus,
                    CleaningStatus = "Clean", // 給予 CleaningStatus 預設值，避免 DB non-null 限制
                    DisabledReason = (SupplyStatus == "Disabled") ? DisabledReason?.Trim() : null
                };

                _context.Rooms.Add(newRoom);
            }
            else
            {
                // 【修改房間】
                var existingRoom = await _context.Rooms.FindAsync(RoomId);
                if (existingRoom != null)
                {
                    existingRoom.BranchId = BranchId;
                    existingRoom.RoomNumber = RoomNumber.Trim();
                    existingRoom.RoomTypeId = RoomTypeId;
                    existingRoom.Floor = Floor;
                    existingRoom.SupplyStatus = SupplyStatus;
                    existingRoom.DisabledReason = (SupplyStatus == "Disabled") ? DisabledReason?.Trim() : null;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}