using HotelManagementSystem.Models;
using HotelManagementSystem.Models.ViewModels.EmployeeHome;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class EmployeeHomeController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly TaipeiClock _clock;
        private readonly NoShowService _noShowService;

        public EmployeeHomeController(HotelManagementContext context, TaipeiClock clock, NoShowService noShowService)
        {
            _context = context;
            _clock = clock;
            _noShowService = noShowService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await _noShowService.UpdateNoShowsAsync();

            var currentEmployeeNumber = "E20260807002"; // TODO 假設是登入的員工編號，實際應從登入資訊取得"

            var staff = _context.Employees
                .FirstOrDefault(e => e.EmployeeNumber == currentEmployeeNumber && e.IsActive);

            if (staff == null)
            {
                return Content("員工資料錯誤，請重新登入");
            }

            var today = _clock.Today;

            var model = new EmployeeHomeViewModel
            {
                PendingCheckIns = _context.Bookings.Where(b => b.BranchId == staff.BranchId
                                                            && b.BookingStatus == "Paid"
                                                            && b.CheckInDate <= today)
                    .Select(b => new PendingCheckInItemViewModel
                    {
                        BookingNumber = b.BookingNumber,
                        BookerName = b.BookerName,
                        ContactPhone = b.ContactPhone,
                        RoomTypeName = b.RoomTypeNameSnapshot,
                        CheckInDate = b.CheckInDate,
                    }).ToList(),

                ActiveStays = _context.StayRecords.Where(s => s.BookingNumberNavigation.BranchId == staff.BranchId
                                                           && s.BookingNumberNavigation.BookingStatus == "CheckedIn"
                                                           && s.ActualCheckOutAt == null)
                    .Select(s => new StayItemViewModel
                    {
                        BookingNumber = s.BookingNumber,
                        PrimaryGuestName = s.PrimaryGuestName,
                        RoomNumber = s.RoomNumberSnapshot,
                        ActualCheckInAt = s.ActualCheckInAt,
                        CheckOutDate = s.BookingNumberNavigation.CheckOutDate,
                    }).ToList(),

                PendingCheckOuts = _context.StayRecords.Where(s => s.BookingNumberNavigation.BranchId == staff.BranchId
                                                           && s.BookingNumberNavigation.BookingStatus == "CheckedIn"
                                                           && s.ActualCheckOutAt == null
                                                           && s.BookingNumberNavigation.CheckOutDate <= today)
                    .Select(s => new StayItemViewModel
                    {
                        BookingNumber = s.BookingNumber,
                        PrimaryGuestName = s.PrimaryGuestName,
                        RoomNumber = s.RoomNumberSnapshot,
                        ActualCheckInAt = s.ActualCheckInAt,
                        CheckOutDate = s.BookingNumberNavigation.CheckOutDate,
                    }).ToList(),

                RoomsToClean = _context.Rooms.Where(r => r.BranchId == staff.BranchId
                                      && r.CleaningStatus == "NeedsCleaning"
                                      && !r.StayRecords.Any(s => s.ActualCheckOutAt == null))
                    .Select(r => new RoomCleaningItemViewModel
                    {
                        RoomId = r.RoomId,
                        RoomNumber = r.RoomNumber,
                        RoomTypeName = r.RoomType.RoomTypeName,
                        Floor = r.Floor,
                    }).ToList()
            };

            return View(model);
        }
    }
}
