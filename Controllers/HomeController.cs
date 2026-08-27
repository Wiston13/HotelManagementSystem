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
                    .ToList()
            };

            return View(model);
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
