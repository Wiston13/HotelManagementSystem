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
        public async Task<IActionResult> Save(Room model, bool confirmCapacityShortage = false)
        {
            // 排除導覽屬性與前端未填寫欄位的 Model 驗證錯誤（使用字串避免編譯期找不到屬性）
            foreach (var key in ModelState.Keys.Where(k => k.Contains(".")).ToList())
            {
                ModelState.Remove(key);
            }
            ModelState.Remove("Branch");
            ModelState.Remove("RoomType");
            ModelState.Remove("StayRecords");
            ModelState.Remove("Bookings");
            ModelState.Remove("CleaningStatus");

            // 基本 Model 驗證
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                        .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                        .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}")
                        .ToList();

                string detailedError = "資料驗證失敗：" + string.Join(" | ", errors);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(detailedError);
                }

                TempData["ErrorMessage"] = detailedError;
                return RedirectToAction(nameof(Index));
            }

            var logsToInsert = new List<OperationLog>();

            // 取得目前操作員編號（可依您的登入機制 User.FindFirst 等方式調整）
            string currentOperator = User.Identity?.Name ?? "System";

            // ==========================================
            // 情況一：新增房間 (RoomId == 0)
            // ==========================================
            if (model.RoomId == 0)
            {
                // 規則 6：新增房間時只接受 Open 或 Disabled
                if (model.SupplyStatus == "Reserved")
                {
                    string errorMsg = "新增失敗：管理員建立房間時不可直接設為預留狀態 (Reserved)！";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(errorMsg);
                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(Index));
                }

                // 1. 驗證傳入的分館是否存在
                bool branchExists = await _context.Branches.AnyAsync(b => b.BranchId == model.BranchId);
                if (!branchExists)
                {
                    string errorMsg = $"新增失敗：指定的分館 ID ({model.BranchId}) 不存在！";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(errorMsg);
                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(Index));
                }

                // 2. 驗證 RoomTypeId 是否屬於該指定分館
                var roomType = await _context.RoomTypes
                    .FirstOrDefaultAsync(rt => rt.RoomTypeId == model.RoomTypeId && rt.BranchId == model.BranchId);

                if (roomType == null)
                {
                    string errorMsg = "新增失敗：所選房型不屬於指定分館！";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(errorMsg);
                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(Index));
                }

                // 3. 新增房間
                var newRoom = new Room
                {
                    BranchId = model.BranchId,
                    RoomTypeId = model.RoomTypeId,
                    RoomNumber = model.RoomNumber,
                    Floor = model.Floor,
                    SupplyStatus = (model.SupplyStatus == "Disabled") ? "Disabled" : "Open",
                    CleaningStatus = string.IsNullOrEmpty(model.CleaningStatus) ? "Clean" : model.CleaningStatus,
                    DisabledReason = model.SupplyStatus == "Disabled" ? model.DisabledReason : null
                };

                _context.Rooms.Add(newRoom);

                // 寫入日誌 (OperationTypeId = 9: RoomCreated)
                logsToInsert.Add(new OperationLog
                {
                    TargetBranchId = model.BranchId,
                    OperatedAt = DateTime.Now,
                    OperatorEmployeeNumber = currentOperator,
                    OperationTypeId = 9,
                    TargetType = "Room",
                    TargetIdentifier = model.RoomNumber,
                    Description = $"新增房間【{model.RoomNumber}】(房型ID: {model.RoomTypeId}, 樓層: {model.Floor}, 初始狀態: {newRoom.SupplyStatus})"
                });

                TempData["SuccessMessage"] = $"新增房間【{model.RoomNumber}】成功！";
            }
            // ==========================================
            // 情況二：編輯房間 (RoomId > 0)
            // ==========================================
            else
            {
                var existingRoom = await _context.Rooms
                    .Include(r => r.StayRecords)
                    .FirstOrDefaultAsync(r => r.RoomId == model.RoomId);

                // 規則 15：RoomId 不存在時明確失敗
                if (existingRoom == null)
                {
                    string errorMsg = $"修改失敗：找不到 ID 為 {model.RoomId} 的房間資料！";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(errorMsg);
                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(Index));
                }

                // 防止非法跨分館轉移房間
                if (model.BranchId != 0 && model.BranchId != existingRoom.BranchId)
                {
                    string errorMsg = "修改失敗：房間不可直接變更所屬分館！";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(errorMsg);
                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(Index));
                }

                string currentStatus = existingRoom.SupplyStatus;
                string requestedStatus = model.SupplyStatus;

                // 規則 9：後端嚴格校驗供應狀態轉換
                bool isValidTransition = (currentStatus, requestedStatus) switch
                {
                    ("Open", "Open") => true,
                    ("Open", "Disabled") => true,
                    ("Disabled", "Disabled") => true,
                    ("Disabled", "Open") => true,
                    ("Reserved", "Reserved") => true,
                    _ => false
                };

                if (!isValidTransition)
                {
                    string errorMsg = $"非法狀態變更：無法將房間供應狀態從【{currentStatus}】變更為【{requestedStatus}】！";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(errorMsg);
                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(Index));
                }

                bool isRoomTypeChanging = existingRoom.RoomTypeId != model.RoomTypeId;
                bool isChangingToDisabled = currentStatus == "Open" && requestedStatus == "Disabled";
                bool isChangingToOpen = currentStatus == "Disabled" && requestedStatus == "Open";

                bool hasActiveStay = existingRoom.StayRecords.Any(s => s.ActualCheckOutAt == null);

                // 規則 A：有入住中住客 (Active StayRecord) 的限制
                if (hasActiveStay)
                {
                    if (isRoomTypeChanging)
                    {
                        string errorMsg = "修改失敗：目前有住客的房間不得修改所屬房型！";
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(errorMsg);
                        TempData["ErrorMessage"] = errorMsg;
                        return RedirectToAction(nameof(Index));
                    }

                    if (isChangingToDisabled)
                    {
                        string errorMsg = "停用失敗：目前有住客在房內，無法將房間設為停用 (Disabled)！";
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(errorMsg);
                        TempData["ErrorMessage"] = errorMsg;
                        return RedirectToAction(nameof(Index));
                    }

                    if (isChangingToOpen)
                    {
                        string errorMsg = "恢復失敗：該房間目前仍有尚未辦理退房的住客，無法恢復為開放販售 (Open)！";
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(errorMsg);
                        TempData["ErrorMessage"] = errorMsg;
                        return RedirectToAction(nameof(Index));
                    }
                }

                // 規則 5：變更房型前的房量檢核 (硬性阻擋)
                if (isRoomTypeChanging && currentStatus == "Open")
                {
                    var (canChange, capacityError) = await CanChangeRoomTypeAsync(existingRoom.RoomTypeId);
                    if (!canChange)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(capacityError);
                        TempData["ErrorMessage"] = capacityError;
                        return RedirectToAction(nameof(Index));
                    }
                }

                // 規則 7：Open -> Disabled 前檢查房量缺口
                if (isChangingToDisabled && !confirmCapacityShortage)
                {
                    var (hasShortage, shortageDetails) = await CheckDisableRoomCapacityShortageAsync(existingRoom.RoomTypeId);
                    if (hasShortage)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return Ok(new
                            {
                                success = false,
                                requireConfirmation = true,
                                message = $"停用提醒：該房型在未來日期存在訂單房量缺口：\n{shortageDetails}\n是否仍要強制停用？"
                            });
                        }

                        TempData["WarningMessage"] = $"停用提醒：該房型存在房量缺口（{shortageDetails}），請再次確認後提交！";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // 檢查新 RoomType 是否屬於該房間的分館
                var targetRoomType = await _context.RoomTypes
                    .FirstOrDefaultAsync(rt => rt.RoomTypeId == model.RoomTypeId && rt.BranchId == existingRoom.BranchId);

                if (targetRoomType == null)
                {
                    string errorMsg = "修改失敗：所選房型不屬於該房間現有之分館！";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return BadRequest(errorMsg);
                    TempData["ErrorMessage"] = errorMsg;
                    return RedirectToAction(nameof(Index));
                }

                // 比較一般欄位變動並紀錄日誌
                bool isBasicInfoChanged = existingRoom.RoomNumber != model.RoomNumber ||
                                          existingRoom.Floor != model.Floor ||
                                          existingRoom.RoomTypeId != model.RoomTypeId ||
                                          (!string.IsNullOrEmpty(model.CleaningStatus) && existingRoom.CleaningStatus != model.CleaningStatus);

                if (isBasicInfoChanged)
                {
                    logsToInsert.Add(new OperationLog
                    {
                        TargetBranchId = existingRoom.BranchId,
                        OperatedAt = DateTime.Now,
                        OperatorEmployeeNumber = currentOperator,
                        OperationTypeId = 10, // RoomUpdated
                        TargetType = "Room",
                        TargetIdentifier = existingRoom.RoomNumber,
                        Description = $"修改房間一般資料【{existingRoom.RoomNumber}】(房號: {existingRoom.RoomNumber} -> {model.RoomNumber}, 樓層: {existingRoom.Floor} -> {model.Floor}, 房型ID: {existingRoom.RoomTypeId} -> {model.RoomTypeId})"
                    });
                }

                if (isChangingToDisabled)
                {
                    logsToInsert.Add(new OperationLog
                    {
                        TargetBranchId = existingRoom.BranchId,
                        OperatedAt = DateTime.Now,
                        OperatorEmployeeNumber = currentOperator,
                        OperationTypeId = 11, // RoomDisabled
                        TargetType = "Room",
                        TargetIdentifier = model.RoomNumber,
                        Description = $"停用房間【{model.RoomNumber}】(原因: {model.DisabledReason ?? "無"})"
                    });
                }
                else if (isChangingToOpen)
                {
                    logsToInsert.Add(new OperationLog
                    {
                        TargetBranchId = existingRoom.BranchId,
                        OperatedAt = DateTime.Now,
                        OperatorEmployeeNumber = currentOperator,
                        OperationTypeId = 12, // RoomEnabled
                        TargetType = "Room",
                        TargetIdentifier = model.RoomNumber,
                        Description = $"恢復開放房間【{model.RoomNumber}】"
                    });
                }

                // 更新欄位
                existingRoom.RoomTypeId = model.RoomTypeId;
                existingRoom.RoomNumber = model.RoomNumber;
                existingRoom.Floor = model.Floor;
                existingRoom.SupplyStatus = requestedStatus;
                existingRoom.DisabledReason = (requestedStatus == "Disabled") ? model.DisabledReason : null;

                if (!string.IsNullOrEmpty(model.CleaningStatus))
                {
                    existingRoom.CleaningStatus = model.CleaningStatus;
                }

                _context.Rooms.Update(existingRoom);
                TempData["SuccessMessage"] = $"修改房間【{model.RoomNumber}】成功！";
            }

            if (logsToInsert.Any())
            {
                _context.OperationLogs.AddRange(logsToInsert);
            }

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(new { success = true });
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // Private Auxiliary Methods
        // ==========================================

        private async Task<(bool CanChange, string ErrorMessage)> CanChangeRoomTypeAsync(int originalRoomTypeId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var endDate = today.AddDays(60);

            int currentOpenCount = await _context.Rooms
                .CountAsync(r => r.RoomTypeId == originalRoomTypeId && r.SupplyStatus == "Open");

            int newOpenCapacity = currentOpenCount - 1;

            var activeBookings = await _context.Bookings
                .Where(b => b.RoomTypeId == originalRoomTypeId
                         && (b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn")
                         && b.CheckInDate < endDate
                         && b.CheckOutDate > today)
                .AsNoTracking()
                .ToListAsync();

            for (var date = today; date < endDate; date = date.AddDays(1))
            {
                var nextDate = date.AddDays(1);
                int dailyDemand = activeBookings.Count(b => b.CheckInDate <= date && b.CheckOutDate >= nextDate);

                if (date == today)
                {
                    int overdueOccupancy = activeBookings.Count(b => b.BookingStatus == "CheckedIn" && b.CheckOutDate <= today);
                    dailyDemand += overdueOccupancy;
                }

                if (dailyDemand > newOpenCapacity)
                {
                    return (false, $"修改失敗：原房型在 {date:yyyy-MM-dd} 已成立訂單需求 ({dailyDemand} 間) 將超過變更後的剩餘房量 ({newOpenCapacity} 間)！");
                }
            }

            return (true, string.Empty);
        }

        private async Task<(bool HasShortage, string ShortageDetails)> CheckDisableRoomCapacityShortageAsync(int roomTypeId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var endDate = today.AddDays(60);

            int currentOpenCount = await _context.Rooms
                .CountAsync(r => r.RoomTypeId == roomTypeId && r.SupplyStatus == "Open");

            int newOpenCapacity = currentOpenCount - 1;

            var activeBookings = await _context.Bookings
                .Where(b => b.RoomTypeId == roomTypeId
                         && (b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn")
                         && b.CheckInDate < endDate
                         && b.CheckOutDate > today)
                .AsNoTracking()
                .ToListAsync();

            List<string> shortageList = new List<string>();

            for (var date = today; date < endDate; date = date.AddDays(1))
            {
                var nextDate = date.AddDays(1);
                int dailyDemand = activeBookings.Count(b => b.CheckInDate <= date && b.CheckOutDate >= nextDate);

                if (date == today)
                {
                    int overdueOccupancy = activeBookings.Count(b => b.BookingStatus == "CheckedIn" && b.CheckOutDate <= today);
                    dailyDemand += overdueOccupancy;
                }

                if (dailyDemand > newOpenCapacity)
                {
                    int shortage = dailyDemand - newOpenCapacity;
                    shortageList.Add($"• {date:yyyy-MM-dd}：缺少 {shortage} 間（需求 {dailyDemand} 間 / 剩餘容量 {newOpenCapacity} 間）");
                }
            }

            if (shortageList.Any())
            {
                string details = string.Join("\n", shortageList.Take(5));
                if (shortageList.Count > 5)
                {
                    details += $"\n...等共 {shortageList.Count} 天存在缺口";
                }
                return (true, details);
            }

            return (false, string.Empty);
        }
    }
}