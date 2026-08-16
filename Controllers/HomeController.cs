using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using HotelManagementSystem.Models.ViewModels.Home;

namespace HotelManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly HotelManagementContext _context;
        public HomeController(HotelManagementContext context) {
            _context = context;                    
        }


        public IActionResult Index()
        {
            var branches = _context.Branches
                .Select(b => new BranchViewModel
                {
                    BranchId = b.BranchId,
                    BranchName = b.BranchName,
                    Phone = b.Phone,
                    Address = b.Address,
                    Description = b.Description,
                    Region = b.Region,
                    ImageUrl = b.ImageUrl
                })
                .ToList();

            var model = new HomeViewModel
            {
                Branches = branches
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
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
