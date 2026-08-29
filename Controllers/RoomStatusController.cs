using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels.RoomStatus;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HotelManagementSystem.Controllers
{
    public class RoomStatusController : BranchEmployeeControllerBase
    {
        private readonly HotelManagementContext _context;
        private readonly TaipeiClock _taipeiClock;
        private readonly RoomAvailabilityService _roomAvailabilityService;
        public RoomStatusController(
            HotelManagementContext context,
            TaipeiClock taipeiClock,
            RoomAvailabilityService roomAvailabilityService)
            : base(context)
        {
            _context = context;
            _taipeiClock = taipeiClock;
            _roomAvailabilityService = roomAvailabilityService;
        }

        [HttpGet]
        public IActionResult Index(int? roomId)
        {
            var model = new RoomStatusViewModel()
            {
                Rooms = _context.Rooms.Where(r => r.BranchId == CurrentBranchId)
                .Select(r => new RoomStatusItemViewModel
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber,
                    Floor = r.Floor,
                    RoomTypeName = r.RoomType.RoomTypeName,
                    SupplyStatus = r.SupplyStatus,
                    CleaningStatus = r.CleaningStatus,
                    DisabledReason = r.DisabledReason,
                    IsOccupied = r.StayRecords.Any(s => s.ActualCheckOutAt == null),
                    PrimaryGuestName = r.StayRecords.Where(s => s.ActualCheckOutAt == null).Select(s => s.PrimaryGuestName).FirstOrDefault(),
                    ContactPhone = r.StayRecords.Where(s => s.ActualCheckOutAt == null).Select(s => s.BookingNumberNavigation.ContactPhone).FirstOrDefault(),
                    BookingNumber = r.StayRecords.Where(s => s.ActualCheckOutAt == null).Select(s => s.BookingNumber).FirstOrDefault(),
                    CheckInDate = r.StayRecords.Where(s => s.ActualCheckOutAt == null).Select(s => s.BookingNumberNavigation.CheckInDate).FirstOrDefault(),
                    ActualCheckInAt = r.StayRecords.Where(s => s.ActualCheckOutAt == null).Select(s => s.ActualCheckInAt).FirstOrDefault(),
                    CheckOutDate = r.StayRecords.Where(s => s.ActualCheckOutAt == null).Select(s => s.BookingNumberNavigation.CheckOutDate).FirstOrDefault()
                }).ToList(),
            };

            model.Floors = model.Rooms
                .Select(r => r.Floor)
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            if (TempData["CapacityRiskWarning"] is string capacityRiskJson)
            {
                model.CapacityRisk =
                    JsonSerializer.Deserialize<CapacityRiskConfirmationViewModel>(capacityRiskJson);
            }

            if (roomId.HasValue &&
                model.CapacityRisk == null &&
                model.Rooms.Any(r => r.RoomId == roomId.Value))
            {
                model.OpenModalRoomId = roomId.Value;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCleaningStatus(int roomId, string targetStatus)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.RoomId == roomId && r.BranchId == CurrentBranchId);
            if (room == null)
            {
                TempData["ErrorMessage"] = "房間資料異常，請再試一次";

                return RedirectToAction(nameof(Index));
            }

            if (targetStatus != "Clean" && targetStatus != "NeedsCleaning")
            {
                TempData["ErrorMessage"] = "房間資料異常，請再試一次";

                return RedirectToAction(nameof(Index));
            }

            var isOccupied = _context.StayRecords.Any(s => s.RoomId == roomId && s.ActualCheckOutAt == null);
            if (isOccupied)
            {
                TempData["ErrorMessage"] = "入住中的房間無法修改清潔狀態。";
                return RedirectToAction(nameof(Index));
            }

            if (room.CleaningStatus == targetStatus)
            {
                TempData["SuccessMessage"] = "房間清潔狀態已是最新狀態。";
                return RedirectToAction(nameof(Index));
            }

            room.CleaningStatus = targetStatus;

            var operationLog = new OperationLog()
            {
                TargetBranchId = room.BranchId,
                OperatedAt = _taipeiClock.Now,
                OperatorEmployeeNumber = CurrentEmployeeNumber!,
                OperationTypeId = 20,
                TargetType = "Room",
                TargetIdentifier = room.RoomNumber,
                Description = $"將房間 {room.RoomNumber} 標記為 {room.CleaningStatus}。"

            };

            try
            {
                _context.OperationLogs.Add(operationLog);

                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "清潔狀態更新失敗，請再試一次";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "房間清潔狀態已更新。";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateSupplyStatus(int roomId, string targetStatus, bool confirmed = false)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.RoomId == roomId && r.BranchId == CurrentBranchId);
            if (room == null)
            {
                TempData["ErrorMessage"] = "房間資料異常，請再試一次";

                return RedirectToAction(nameof(Index));
            }

            if (targetStatus != "Open" && targetStatus != "Reserved")
            {
                TempData["ErrorMessage"] = "房間資料異常，請再試一次";

                return RedirectToAction(nameof(Index));
            }

            var isOccupied = _context.StayRecords.Any(s => s.RoomId == roomId && s.ActualCheckOutAt == null);
            if (isOccupied)
            {
                TempData["ErrorMessage"] = "入住中的房間無法修改供應狀態。";
                return RedirectToAction(nameof(Index));
            }

            if (confirmed &&
                (targetStatus != "Reserved" || room.SupplyStatus != "Open"))
            {
                TempData["ErrorMessage"] = "房間供應狀態已變更，請重新確認後再操作。";
                return RedirectToAction(nameof(Index), new { roomId });
            }

            if (room.SupplyStatus == targetStatus)
            {
                TempData["SuccessMessage"] = "房間供應狀態已是最新狀態。";
                return RedirectToAction(nameof(Index));
            }

            if (room.SupplyStatus == "Disabled")
            {
                TempData["ErrorMessage"] = "停用中的房間無法修改供應狀態。";
                return RedirectToAction(nameof(Index));
            }

            if (room.SupplyStatus == "Open" && targetStatus == "Reserved")
            {
                var today = _taipeiClock.Today;
                var shortages = _roomAvailabilityService.FindCapacityShortages(
                    room.RoomTypeId,
                    today,
                    today.AddDays(60),
                    1);

                if (shortages.Count > 0 && !confirmed)
                {
                    var capacityRisk = new CapacityRiskConfirmationViewModel
                    {
                        RoomId = room.RoomId,
                        RoomNumber = room.RoomNumber,
                        TargetStatus = targetStatus,
                        Shortages = shortages
                            .OrderBy(shortage => shortage.Key)
                            .Select(shortage => new CapacityShortageViewModel
                            {
                                Date = shortage.Key,
                                ShortageCount = shortage.Value
                            })
                            .ToList()
                    };

                    TempData["CapacityRiskWarning"] = JsonSerializer.Serialize(capacityRisk);
                    return RedirectToAction(nameof(Index), new { roomId });
                }
            }


            room.SupplyStatus = targetStatus;

            var operationLog = new OperationLog()
            {
                TargetBranchId = room.BranchId,
                OperatedAt = _taipeiClock.Now,
                OperatorEmployeeNumber = CurrentEmployeeNumber!,
                OperationTypeId = (room.SupplyStatus == "Reserved") ? 18 : 19,
                TargetType = "Room",
                TargetIdentifier = room.RoomNumber,
                Description = $"將房間 {room.RoomNumber} 供應狀態更新為 {room.SupplyStatus}。"

            };

            try
            {
                _context.OperationLogs.Add(operationLog);

                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "供應狀態更新失敗，請再試一次";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "房間供應狀態已更新。";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateDisabledStatus(
            int roomId,
            string targetStatus,
            string? disabledReason,
            bool confirmed = false)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.RoomId == roomId && r.BranchId == CurrentBranchId);
            if (room == null)
            {
                TempData["ErrorMessage"] = "房間資料異常，請再試一次";

                return RedirectToAction(nameof(Index));
            }

            if (targetStatus != "Disabled" && targetStatus != "Open")
            {
                TempData["ErrorMessage"] = "房間資料異常，請再試一次";

                return RedirectToAction(nameof(Index));
            }

            var isOccupied = _context.StayRecords.Any(s => s.RoomId == roomId && s.ActualCheckOutAt == null);
            if (isOccupied)
            {
                TempData["ErrorMessage"] = "入住中的房間無法修改供應狀態。";
                return RedirectToAction(nameof(Index));
            }

            if (targetStatus == "Disabled")
            {
                if (confirmed && room.SupplyStatus != "Open")
                {
                    TempData["ErrorMessage"] = "房間供應狀態已變更，請重新確認後再操作。";
                    return RedirectToAction(nameof(Index), new { roomId });
                }

                if (room.SupplyStatus == "Disabled")
                {
                    TempData["ErrorMessage"] = "房間目前已是停用狀態。";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(disabledReason))
                {
                    TempData["ErrorMessage"] = "請填寫停用原因。";
                    return RedirectToAction(nameof(Index));
                }

                var newReason = disabledReason.Trim();

                if (newReason.Length > 200)
                {
                    TempData["ErrorMessage"] = "停用原因不可超過 200 字。";
                    return RedirectToAction(nameof(Index));
                }

                if (room.SupplyStatus == "Open")
                {
                    var today = _taipeiClock.Today;
                    var shortages = _roomAvailabilityService.FindCapacityShortages(
                        room.RoomTypeId,
                        today,
                        today.AddDays(60),
                        1);

                    if (shortages.Count > 0 && !confirmed)
                    {
                        var capacityRisk = new CapacityRiskConfirmationViewModel
                        {
                            RoomId = room.RoomId,
                            RoomNumber = room.RoomNumber,
                            TargetStatus = targetStatus,
                            DisabledReason = newReason,
                            Shortages = shortages
                                .OrderBy(shortage => shortage.Key)
                                .Select(shortage => new CapacityShortageViewModel
                                {
                                    Date = shortage.Key,
                                    ShortageCount = shortage.Value
                                })
                                .ToList()
                        };

                        TempData["CapacityRiskWarning"] = JsonSerializer.Serialize(capacityRisk);
                        return RedirectToAction(nameof(Index), new { roomId });
                    }
                }

                room.SupplyStatus = "Disabled";
                room.DisabledReason = newReason;
            }
            else
            {
                if (room.SupplyStatus != "Disabled")
                {
                    TempData["ErrorMessage"] = "只有停用中的房間可以恢復開放販售。";
                    return RedirectToAction(nameof(Index));
                }

                room.SupplyStatus = "Open";
                room.DisabledReason = null;
            }

            var operationLog = new OperationLog()
            {
                TargetBranchId = room.BranchId,
                OperatedAt = _taipeiClock.Now,
                OperatorEmployeeNumber = CurrentEmployeeNumber!,
                OperationTypeId = (room.SupplyStatus == "Disabled") ? 11 : 12,
                TargetType = "Room",
                TargetIdentifier = room.RoomNumber,
                Description = room.SupplyStatus == "Disabled"
                    ? $"將房間 {room.RoomNumber} 停用，原因：{room.DisabledReason}。"
                    : $"將房間 {room.RoomNumber} 恢復開放販售。"
            };

            try
            {
                _context.OperationLogs.Add(operationLog);

                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "房間停用狀態更新失敗，請再試一次";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = room.SupplyStatus == "Disabled"
                ? "房間已停用。"
                : "房間已恢復開放販售。";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateDisabledReason(int roomId, string? disabledReason)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.RoomId == roomId && r.BranchId == CurrentBranchId);
            if (room == null)
            {
                TempData["ErrorMessage"] = "房間資料異常，請重新整理後再試一次。";
                return BadRequest();
            }

            if (room.SupplyStatus != "Disabled")
            {
                TempData["ErrorMessage"] = "房間目前已不是停用狀態，頁面將重新整理。";
                return BadRequest();
            }

            var isOccupied = _context.StayRecords
                .Any(s => s.RoomId == roomId &&
                          s.ActualCheckOutAt == null);

            if (isOccupied)
            {
                TempData["ErrorMessage"] = "入住中的房間無法修改停用原因。";
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(disabledReason))
            {
                TempData["ErrorMessage"] = "請填寫停用原因。";
                return BadRequest();
            }

            var oldReason = room.DisabledReason;
            var newReason = disabledReason.Trim();

            if (newReason.Length > 200)
            {
                TempData["ErrorMessage"] = "停用原因不可超過 200 字。";
                return BadRequest();
            }

            if (room.DisabledReason == newReason)
            {
                return Json(new
                {
                    disabledReason = newReason
                });
            }

            room.DisabledReason = newReason;

            var operationLog = new OperationLog()
            {
                TargetBranchId = room.BranchId,
                OperatedAt = _taipeiClock.Now,
                OperatorEmployeeNumber = CurrentEmployeeNumber!,
                OperationTypeId = 24,
                TargetType = "Room",
                TargetIdentifier = room.RoomNumber,
                Description =
                    $"將房間 {room.RoomNumber} 的停用原因由「{oldReason}」修改為「{newReason}」。"
            };

            try
            {
                _context.OperationLogs.Add(operationLog);

                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "房間停用原因更新失敗，請再試一次";
                return StatusCode(500);
            }

            return Json(new
            {
                disabledReason = newReason
            });
        }

    }
}
