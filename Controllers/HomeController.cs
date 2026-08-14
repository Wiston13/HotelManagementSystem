using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
            var branches = _context.Branches.ToList();
            return View(branches);
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
