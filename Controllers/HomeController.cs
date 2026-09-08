using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using HotelManagementSystem.Models.ViewModels.Home;
using HotelManagementSystem.Services;

namespace HotelManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly HotelManagementContext _context;

        private readonly TaipeiClock _taipeiClock;
        public HomeController(HotelManagementContext context, TaipeiClock taipeiClock) 
        {
            _context = context;

            _taipeiClock = taipeiClock;
        }


        public IActionResult Index()
        {
            var now = _taipeiClock.Now;

            var model = new HomeViewModel
            {
                Today = _taipeiClock.Today,

                Branches = _context.Branches
                    .Select(b => new BranchViewModel
                    {
                        BranchId = b.BranchId,
                        BranchName = b.BranchName,
                        AcceptsNewBookings = b.AcceptsNewBookings,
                        Phone = b.Phone,
                        Address = b.Address,
                        Description = b.Description,
                        Region = b.Region,
                        ImageUrl = b.ImageUrl
                    })
                    .ToList(),

                RoomTypes = _context.RoomTypes
                    .Where(r => r.IsActive)
                    .Select(r => new RoomTypeViewModel
                    {
                        RoomTypeId = r.RoomTypeId,
                        BranchId = r.BranchId,
                        RoomTypeName = r.RoomTypeName,
                        MaxOccupancy = r.MaxOccupancy,
                        IsActive = r.IsActive
                    })
                    .ToList(),

                Announcements = _context.Announcements
                            .Where(a => a.IsActive
                                              && a.ShowToGuest
                                              && a.StartAt <= now
                                              && a.EndAt >= now)
                            .OrderByDescending(a => a.StartAt)
                            .Take(1)
                            .Select(a => new HomeAnnouncementViewModel
                             {
                                    Title = a.Title,
                                    Content = a.Content,
                                    StartAt = a.StartAt,
                                    EndAt = a.EndAt
                              })
                              .ToList()
            };

            return View(model);
        }

        public IActionResult Announcements()
        {
            var now = _taipeiClock.Now;

            var announcements = _context.Announcements
                .Where(a => a.IsActive
                         && a.ShowToGuest
                         && a.StartAt <= now
                         && a.EndAt >= now)
                .OrderByDescending(a => a.StartAt)
                .Select(a => new HomeAnnouncementViewModel
                {
                    Title = a.Title,
                    Content = a.Content,
                    StartAt = a.StartAt,
                    EndAt = a.EndAt
                })
                .ToList();

            return View(announcements);
        }

        public IActionResult GuestGuide()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
