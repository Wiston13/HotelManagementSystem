using HotelManagementSystem.Models;
using HotelManagementSystem.Models.ViewModels.EmployeeHome;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class EmployeeHomeController : BranchEmployeeControllerBase
    {
        private readonly HotelManagementContext _context;
        private readonly TaipeiClock _clock;
        private readonly NoShowService _noShowService;

        public EmployeeHomeController(HotelManagementContext context, TaipeiClock clock, NoShowService noShowService)
            : base(context)
        {
            _context = context;
            _clock = clock;
            _noShowService = noShowService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await _noShowService.UpdateNoShowsAsync();

            var today = _clock.Today;
            var now = _clock.Now;

            var model = new EmployeeHomeViewModel
            {
                PendingCheckIns = await _context.Bookings.Where(b => b.BranchId == CurrentBranchId
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

                ActiveStays = await _context.StayRecords.Where(s => s.BookingNumberNavigation.BranchId == CurrentBranchId
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

                PendingCheckOuts = await _context.StayRecords.Where(s => s.BookingNumberNavigation.BranchId == CurrentBranchId
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

                RoomsToClean = await _context.Rooms.Where(r => r.BranchId == CurrentBranchId
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
                    }).ToListAsync(),

                Announcements = await _context.Announcements
                         .Where(a => a.IsActive
                                           && a.StartAt <= now
                                           && a.EndAt >= now)
                        .AsNoTracking()
                        .OrderByDescending(a => a.StartAt)
                        .Take(1)
                        .Select(a => new AnnouncementItemViewModel
                        {
                                Title = a.Title,
                                Content = a.Content,
                                StartAt = a.StartAt,
                                EndAt = a.EndAt
                         })
                        .ToListAsync()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Announcements()
        {
            var now = _clock.Now;

            var announcements = await _context.Announcements
                .Where(a => a.IsActive
                         && a.StartAt <= now
                         && a.EndAt >= now)
                .AsNoTracking()
                .OrderByDescending(a => a.StartAt)
                .Select(a => new AnnouncementItemViewModel
                {
                    Title = a.Title,
                    Content = a.Content,
                    StartAt = a.StartAt,
                    EndAt = a.EndAt
                })
                .ToListAsync();

            return View(announcements);
        }
    }
}
