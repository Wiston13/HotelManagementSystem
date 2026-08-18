using Microsoft.AspNetCore.Mvc;
using HotelManagementSystem.Models;

namespace HotelManagementSystem.Controllers
{
    public class BookingController : Controller
    {
        [HttpGet]
        public IActionResult RoomSelection(string branchName, DateTime checkIn, DateTime checkOut, int guests)
        {            
            return View();
        }

        public IActionResult Payment()
        {
            return View();
        }

        public IActionResult Success()
        {
            return View();
        }
        
        public IActionResult Lookup()
        {
            return View();
        }
    }
}
