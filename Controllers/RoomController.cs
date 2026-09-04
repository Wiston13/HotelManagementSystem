using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels;
using HotelManagementSystem.Services;

namespace HotelManagementSystem.Controllers
{
    public class RoomController : SystemAdminControllerBase
    {
        private readonly HotelManagementContext _context;
        private readonly TaipeiClock _clock;
        private readonly RoomAvailabilityService _roomAvailabilityService;

        public RoomController(HotelManagementContext context, TaipeiClock clock,RoomAvailabilityService roomAvailabilityService) : base(context)
        {
            _context = context;
            _roomAvailabilityService = roomAvailabilityService;
            _clock = clock;
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
        public async Task<IActionResult> Save(
    Room model,
    bool confirmCapacityShortage = false)
        {
            // 移除導覽屬性等前端不負責提供的 ModelState 驗證
            foreach (var key in ModelState.Keys
                         .Where(k => k.Contains("."))
                         .ToList())
            {
                ModelState.Remove(key);
            }

            ModelState.Remove("Branch");
            ModelState.Remove("RoomType");
            ModelState.Remove("StayRecords");
            ModelState.Remove("Bookings");
            ModelState.Remove("CleaningStatus");

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                    .Select(x =>
                        $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}")
                    .ToList();

                return Fail(
                    "資料驗證失敗：" + string.Join(" | ", errors));
            }

            var currentOperator = CurrentEmployeeNumber!;
            var logsToInsert = new List<OperationLog>();

            var roomNumber = model.RoomNumber?.Trim();

            if (string.IsNullOrWhiteSpace(roomNumber))
            {
                return Fail("房號不可空白。");
            }

            // =====================================================
            // 新增房間
            // =====================================================
            if (model.RoomId == 0)
            {
                var requestedStatus = model.SupplyStatus?.Trim();

                // 管理員新增只能 Open / Disabled
                if (requestedStatus != "Open" &&
                    requestedStatus != "Disabled")
                {
                    return Fail(
                        "新增失敗：房間初始供應狀態只能為「開放販售」或「停用」。");
                }

                // 驗證分館
                var branchExists = await _context.Branches
                    .AnyAsync(b => b.BranchId == model.BranchId);

                if (!branchExists)
                {
                    return Fail(
                        $"新增失敗：指定的分館 ID ({model.BranchId}) 不存在。");
                }

                // 驗證房型必須屬於該分館，並取得房型名稱供操作紀錄使用。
                var roomType = await _context.RoomTypes
                    .FirstOrDefaultAsync(rt =>
                        rt.RoomTypeId == model.RoomTypeId &&
                        rt.BranchId == model.BranchId);

                if (roomType == null)
                {
                    return Fail(
                        "新增失敗：所選房型不屬於指定分館。");
                }

                // 同分館房號不可重複
                var isDuplicate = await _context.Rooms
                    .AnyAsync(r =>
                        r.BranchId == model.BranchId &&
                        r.RoomNumber == roomNumber);

                if (isDuplicate)
                {
                    return Fail(
                        $"新增失敗：該分館內已有房號【{roomNumber}】。");
                }

                // DisabledReason
                string? disabledReason = null;

                if (requestedStatus == "Disabled")
                {
                    if (string.IsNullOrWhiteSpace(model.DisabledReason))
                    {
                        return Fail(
                            "新增失敗：停用房間時必須填寫停用原因。");
                    }

                    disabledReason = model.DisabledReason.Trim();

                    if (disabledReason.Length > 200)
                    {
                        return Fail(
                            "新增失敗：停用原因不可超過 200 字。");
                    }
                }

                var newRoom = new Room
                {
                    BranchId = model.BranchId,
                    RoomTypeId = model.RoomTypeId,
                    RoomNumber = roomNumber,
                    Floor = model.Floor,

                    SupplyStatus = requestedStatus,

                    // SystemAdmin 不可自行指定 CleaningStatus
                    CleaningStatus = "Clean",

                    DisabledReason = disabledReason
                };

                _context.Rooms.Add(newRoom);
                var initialStatusText = requestedStatus == "Open" ? "開放販售" : "停用";

                logsToInsert.Add(new OperationLog
                {
                    TargetBranchId = model.BranchId,
                    OperatedAt = _clock.Now,
                    OperatorEmployeeNumber = currentOperator,

                    OperationTypeId = 9,
                    TargetType = "Room",
                    TargetIdentifier = roomNumber,

                    Description =
                        $"新增房間【{roomNumber}】" +
                        $"(房型: {roomType.RoomTypeName}, " +
                        $"樓層: {model.Floor}, " +
                        $"初始狀態: {initialStatusText})"
                });

                TempData["SuccessMessage"] =
                    $"新增房間【{roomNumber}】成功！";
            }

            // =====================================================
            // 修改房間
            // =====================================================
            else
            {
                var existingRoom = await _context.Rooms
                    .Include(r => r.StayRecords)
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r =>
                        r.RoomId == model.RoomId);

                if (existingRoom == null)
                {
                    return Fail(
                        $"修改失敗：找不到 ID 為 {model.RoomId} 的房間資料。");
                }

                // 房間建立後不可直接換分館
                if (model.BranchId != 0 &&
                    model.BranchId != existingRoom.BranchId)
                {
                    return Fail(
                        "修改失敗：房間不可直接變更所屬分館。");
                }

                // 同分館房號不可重複
                var isDuplicate = await _context.Rooms
                    .AnyAsync(r =>
                        r.BranchId == existingRoom.BranchId &&
                        r.RoomNumber == roomNumber &&
                        r.RoomId != existingRoom.RoomId);

                if (isDuplicate)
                {
                    return Fail(
                        $"修改失敗：該分館內已有房號【{roomNumber}】。");
                }

                var isRoomTypeChanging =
                    existingRoom.RoomTypeId != model.RoomTypeId;
                var targetRoomTypeName = existingRoom.RoomType.RoomTypeName;

                if (isRoomTypeChanging)
                {
                    // 新 RoomType 必須仍屬於原分館，並取得名稱供操作紀錄使用。
                    var targetRoomType = await _context.RoomTypes
                        .FirstOrDefaultAsync(rt =>
                            rt.RoomTypeId == model.RoomTypeId &&
                            rt.BranchId == existingRoom.BranchId);

                    if (targetRoomType == null)
                    {
                        return Fail(
                            "修改失敗：所選房型不屬於該房間目前所屬分館。");
                    }

                    targetRoomTypeName = targetRoomType.RoomTypeName;
                }

                var currentStatus = existingRoom.SupplyStatus;
                var requestedStatus = model.SupplyStatus?.Trim();

                // 狀態轉換白名單
                var isValidTransition =
                    (currentStatus, requestedStatus) switch
                    {
                        ("Open", "Open") => true,
                        ("Open", "Disabled") => true,

                        ("Disabled", "Disabled") => true,
                        ("Disabled", "Open") => true,

                        // 系統 Reserved 只能維持 Reserved
                        ("Reserved", "Reserved") => true,

                        _ => false
                    };

                if (!isValidTransition)
                {
                    var currentStatusText = currentStatus switch
                    {
                        "Open" => "開放販售",
                        "Reserved" => "保留",
                        "Disabled" => "停用",
                        _ => "未知狀態"
                    };
                    var requestedStatusText = requestedStatus switch
                    {
                        "Open" => "開放販售",
                        "Reserved" => "保留",
                        "Disabled" => "停用",
                        _ => "未知狀態"
                    };

                    return Fail(
                        $"非法狀態變更：無法將房間供應狀態從【{currentStatusText}】變更為【{requestedStatusText}】。");
                }

                var isChangingToDisabled =
                    currentStatus == "Open" &&
                    requestedStatus == "Disabled";

                var isChangingToOpen =
                    currentStatus == "Disabled" &&
                    requestedStatus == "Open";

                // =================================================
                // 二次確認 stale-state protection
                // =================================================
                if (confirmCapacityShortage)
                {
                    if (currentStatus != "Open" ||
                        requestedStatus != "Disabled")
                    {
                        return Fail(
                            "房間狀態已變更，請重新整理後再操作。");
                    }
                }

                // =================================================
                // DisabledReason 後端驗證
                // =================================================
                string? disabledReason = null;

                if (requestedStatus == "Disabled")
                {
                    if (string.IsNullOrWhiteSpace(model.DisabledReason))
                    {
                        return Fail(
                            "修改失敗：停用房間時必須填寫停用原因。");
                    }

                    disabledReason = model.DisabledReason.Trim();

                    if (disabledReason.Length > 200)
                    {
                        return Fail(
                            "修改失敗：停用原因不可超過 200 字。");
                    }
                }

                // =================================================
                // Active Stay
                // =================================================
                var hasActiveStay =
                    existingRoom.StayRecords
                        .Any(s => s.ActualCheckOutAt == null);

                if (hasActiveStay)
                {
                    if (existingRoom.RoomNumber != roomNumber)
                    {
                        return Fail(
                            "修改失敗：目前有住客的房間不得修改房號。");
                    }

                    if (isRoomTypeChanging)
                    {
                        return Fail(
                            "修改失敗：目前有住客的房間不得修改所屬房型。");
                    }

                    if (isChangingToDisabled)
                    {
                        return Fail(
                            "停用失敗：目前有住客在房內，無法將房間設為停用。");
                    }

                    if (isChangingToOpen)
                    {
                        return Fail(
                            "恢復失敗：該房間目前仍有尚未辦理退房的住客，無法恢復為開放販售。");
                    }
                }
                var capacityCheckStartDate = _clock.Today;
                var capacityCheckEndDate = capacityCheckStartDate.AddDays(60);

                // =================================================
                // 換 RoomType 前檢查「舊房型」少一間後是否 shortage
                //
                // 換房型造成 shortage 是硬性禁止，不提供二次確認。
                // =================================================
                if (isRoomTypeChanging &&
    currentStatus == "Open")
                {
                    var shortages =
                        _roomAvailabilityService.FindCapacityShortages(
                            existingRoom.RoomTypeId,
                            capacityCheckStartDate,
                            capacityCheckEndDate,
                            supplyReduction: 1);

                    if (shortages.Any())
                    {
                        var firstShortage = shortages
                            .OrderBy(x => x.Key)
                            .First();

                        return Fail(
                            $"修改失敗：變更房型後會造成原房型 " +
                            $"{firstShortage.Key:yyyy-MM-dd} " +
                            $"房量不足 {firstShortage.Value} 間。");
                    }
                }

                // =================================================
                // Open → Disabled
                // =================================================
                if (isChangingToDisabled &&
     !confirmCapacityShortage)
                {
                    var shortages =
                        _roomAvailabilityService.FindCapacityShortages(
                            existingRoom.RoomTypeId,
                            capacityCheckStartDate,
                            capacityCheckEndDate,
                            supplyReduction: 1);

                    // 無 shortage：
                    // 不 return，繼續往下 Save
                    if (shortages.Any())
                    {
                        var shortageDetails = string.Join(
                            "\n",
                            shortages
                                .OrderBy(x => x.Key)
                                .Take(5)
                                .Select(x =>
                                    $"• {x.Key:yyyy-MM-dd}：缺少 {x.Value} 間"));

                        if (shortages.Count > 5)
                        {
                            shortageDetails +=
                                $"\n...等共 {shortages.Count} 天存在缺口";
                        }

                        return Ok(new
                        {
                            success = false,
                            requireConfirmation = true,
                            message =
                                "停用此房間後，部分日期會造成已成立訂單房量不足：\n" +
                                shortageDetails +
                                "\n是否仍要停用？"
                        });
                    }
                }
                // =================================================
                // OperationLog：先判斷，再修改 entity
                // =================================================
                var oldDisabledReason = existingRoom.DisabledReason;
                var isBasicInfoChanged =
                    existingRoom.RoomNumber != roomNumber ||
                    existingRoom.Floor != model.Floor ||
                    existingRoom.RoomTypeId != model.RoomTypeId;

                if (isBasicInfoChanged)
                {
                    logsToInsert.Add(new OperationLog
                    {
                        TargetBranchId = existingRoom.BranchId,
                        OperatedAt = _clock.Now,
                        OperatorEmployeeNumber = currentOperator,

                        OperationTypeId = 10,
                        TargetType = "Room",
                        TargetIdentifier = roomNumber,

                        Description =
                            $"修改房間【{existingRoom.RoomNumber}】" +
                            $"(房號: {existingRoom.RoomNumber} -> {roomNumber}, " +
                            $"樓層: {existingRoom.Floor} -> {model.Floor}, " +
                            $"房型: {existingRoom.RoomType.RoomTypeName} -> {targetRoomTypeName})"
                    });
                }

                if (isChangingToDisabled)
                {
                    logsToInsert.Add(new OperationLog
                    {
                        TargetBranchId = existingRoom.BranchId,
                        OperatedAt = _clock.Now,
                        OperatorEmployeeNumber = currentOperator,

                        OperationTypeId = 11,
                        TargetType = "Room",
                        TargetIdentifier = roomNumber,

                        Description =
                            $"停用房間【{roomNumber}】" +
                            $"(原因: {disabledReason})"
                    });
                }
                else if (isChangingToOpen)
                {
                    logsToInsert.Add(new OperationLog
                    {
                        TargetBranchId = existingRoom.BranchId,
                        OperatedAt = _clock.Now,
                        OperatorEmployeeNumber = currentOperator,

                        OperationTypeId = 12,
                        TargetType = "Room",
                        TargetIdentifier = roomNumber,

                        Description =
                            $"恢復開放房間【{roomNumber}】"
                    });
                }

                if (currentStatus == "Disabled" &&
                    requestedStatus == "Disabled" &&
                    oldDisabledReason != disabledReason)
                {
                    logsToInsert.Add(new OperationLog
                    {
                        TargetBranchId = existingRoom.BranchId,
                        OperatedAt = _clock.Now,
                        OperatorEmployeeNumber = currentOperator,
                        OperationTypeId = 24,
                        TargetType = "Room",
                        TargetIdentifier = roomNumber,
                        Description =
                            $"將房間 {roomNumber} 的停用原因由「{oldDisabledReason?.TrimEnd('。')}」修改為「{disabledReason?.TrimEnd('。')}」。"
                    });
                }

                // =================================================
                // 真正允許 SystemAdmin 修改的欄位
                // =================================================
                existingRoom.RoomNumber = roomNumber;
                existingRoom.RoomTypeId = model.RoomTypeId;
                existingRoom.Floor = model.Floor;
                existingRoom.SupplyStatus = requestedStatus!;
                existingRoom.DisabledReason = disabledReason;


                TempData["SuccessMessage"] =
                    $"修改房間【{roomNumber}】成功！";
            }

            // =====================================================
            // 統一寫 Log + Save
            // =====================================================
            if (logsToInsert.Any())
            {
                _context.OperationLogs.AddRange(logsToInsert);
            }

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] ==
                "XMLHttpRequest")
            {
                return Ok(new
                {
                    success = true
                });
            }

            return RedirectToAction(nameof(Index));
        }
      
        private IActionResult Fail(string message)
        {
            if (Request.Headers["X-Requested-With"] ==
                "XMLHttpRequest")
            {
                return BadRequest(message);
            }

            TempData["ErrorMessage"] = message;

            return RedirectToAction(nameof(Index));
        }
    }
}
