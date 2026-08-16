using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class BookingController : Controller
    {
        [HttpGet]
        public IActionResult RoomSelection(int branchId, DateTime checkIn, DateTime checkOut, int guests)
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
