using HotelManagementSystem.Models;
using HotelManagementSystem.Models.ViewModels.RoomStatus;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class RoomStatusController : Controller
    {
        private readonly HotelManagementContext _context;
        public RoomStatusController(HotelManagementContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var currentEmployeeNumber = "E20260807002"; // TODO 假設是登入的員工編號，實際應從登入資訊取得"

            var staff = _context.Employees
                .FirstOrDefault(e => e.EmployeeNumber == currentEmployeeNumber && e.IsActive);

            if (staff == null)
            {
                return Content("員工資料錯誤，請重新登入");
            }

            var model = new RoomStatusViewModel()
            {
                Rooms = _context.Rooms.Where(r => r.BranchId == staff.BranchId)
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

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCleaningStatus(int roomId, string targetStatus)
        {
            var currentEmployeeNumber = "E20260807002"; // TODO 假設是登入的員工編號，實際應從登入資訊取得"

            var staff = _context.Employees
                .FirstOrDefault(e => e.EmployeeNumber == currentEmployeeNumber && e.IsActive);

            if (staff == null)
            {
                return Content("員工資料錯誤，請重新登入");
            }

            var room = _context.Rooms.FirstOrDefault(r => r.RoomId == roomId && r.BranchId == staff.BranchId);
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

            room.CleaningStatus = targetStatus;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "房間清潔狀態已更新。";

            return RedirectToAction(nameof(Index));
        }

    }
}
