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
                // 驗證SupplyStatus合法性
                if (SupplyStatus != "Open" && SupplyStatus != "Disabled")
                {
                    TempData["ErrorMessage"] = "房間停用狀態資料異常，請重新操作！";
                    return RedirectToAction(nameof(Index));
                }
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
                else
                {
                    // 驗證roomID合法性
                    TempData["ErrorMessage"] = "房間編號資料異常，請重新操作！";
                    return RedirectToAction(nameof(Index));
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}

/*PR#31 
 * 
 * 4.和8類似 有人住不能修改房型
 * 5.修改房型前檢查原房型未來房量 (有點複雜最後處理)
 * 6.加入限制 管理員改變房間狀態要有限制: 新增房間只能:開啟、停用 (不太懂先跳過)
 * 7.open->disable 計算訂單異動 傳出資料至前端 讓管理員操作知道有房間和日期受影響 再次確認
 * 8.disable->open 檢查訂單狀態 如果該房該時間有人住 拒絕開放
 * 9.supplyStatus 修改狀態時驗證 資料正確性
 * 14.roomcontroller加入operationLog
 * 15.驗證傳入的RoomId是否合法
 * 17.驗證roomController 傳進來的supplyStatus是否合法
 ○* 18.Views/Room/Index.cshtml 移除 如果是保留 不能操作 
 ○* 19.Views/RoomType/Index.cshtml 基本上不用改 後端正確驗證就好
 */