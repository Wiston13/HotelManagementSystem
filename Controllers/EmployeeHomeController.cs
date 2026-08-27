using HotelManagementSystem.Models;
using HotelManagementSystem.Models.ViewModels.EmployeeHome;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelManagementSystem.Controllers
{
    [Authorize(Roles = "BranchEmployee")]
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

            var currentEmployeeNumber = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var staff = _context.Employees
                .FirstOrDefault(e => e.EmployeeNumber == currentEmployeeNumber && e.IsActive);

            if (staff == null)
            {
                return Content("員工資料錯誤，請重新登入");
            }

            var today = _clock.Today;

            var model = new EmployeeHomeViewModel
            {
                PendingCheckIns = await _context.Bookings.Where(b => b.BranchId == staff.BranchId
                                                            && b.BookingStatus == "Paid"
                                                            && b.CheckInDate <= today)
                    .AsNoTracking()
                    .OrderBy(b => b.CheckInDate)
                    .ThenBy(b => b.BookingNumber)
                    .Select(b => new PendingCheckInItemViewModel
                    {
                        BookingNumber = b.BookingNumber,
                        BookerName = b.BookerName,
                        ContactPhone = b.ContactPhone,
                        RoomTypeName = b.RoomTypeNameSnapshot,
                        CheckInDate = b.CheckInDate,
                    }).ToListAsync(),

                ActiveStays = await _context.StayRecords.Where(s => s.BookingNumberNavigation.BranchId == staff.BranchId
                                                           && s.BookingNumberNavigation.BookingStatus == "CheckedIn"
                                                           && s.ActualCheckOutAt == null)
                    .AsNoTracking()
                    .OrderBy(s => s.ActualCheckInAt)
                    .ThenBy(s => s.RoomNumberSnapshot)
                    .Select(s => new StayItemViewModel
                    {
                        BookingNumber = s.BookingNumber,
                        PrimaryGuestName = s.PrimaryGuestName,
                        RoomNumber = s.RoomNumberSnapshot,
                        ActualCheckInAt = s.ActualCheckInAt,
                        CheckOutDate = s.BookingNumberNavigation.CheckOutDate,
                    }).ToListAsync(),

                PendingCheckOuts = await _context.StayRecords.Where(s => s.BookingNumberNavigation.BranchId == staff.BranchId
                                                           && s.BookingNumberNavigation.BookingStatus == "CheckedIn"
                                                           && s.ActualCheckOutAt == null
                                                           && s.BookingNumberNavigation.CheckOutDate <= today)
                    .AsNoTracking()
                    .OrderBy(s => s.BookingNumberNavigation.CheckOutDate)
                    .ThenBy(s => s.RoomNumberSnapshot)
                    .Select(s => new StayItemViewModel
                    {
                        BookingNumber = s.BookingNumber,
                        PrimaryGuestName = s.PrimaryGuestName,
                        RoomNumber = s.RoomNumberSnapshot,
                        ActualCheckInAt = s.ActualCheckInAt,
                        CheckOutDate = s.BookingNumberNavigation.CheckOutDate,
                    }).ToListAsync(),

                RoomsToClean = await _context.Rooms.Where(r => r.BranchId == staff.BranchId
                                      && r.CleaningStatus == "NeedsCleaning"
                                      && !r.StayRecords.Any(s => s.ActualCheckOutAt == null))
                    .AsNoTracking()
                    .OrderBy(r => r.Floor)
                    .ThenBy(r => r.RoomNumber)
                    .Select(r => new RoomCleaningItemViewModel
                    {
                        RoomId = r.RoomId,
                        RoomNumber = r.RoomNumber,
                        RoomTypeName = r.RoomType.RoomTypeName,
                        Floor = r.Floor,
                    }).ToListAsync()
            };

            return View(model);
        }
    }
}
