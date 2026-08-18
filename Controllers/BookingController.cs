using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels.Booking;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class BookingController : Controller
    {
        private readonly HotelManagementContext _context;
        public BookingController(HotelManagementContext context)
        {
            _context = context;
        }


        [HttpGet]
        public IActionResult RoomSelection(int branchId, DateTime checkIn, DateTime checkOut, int guestCount)
        {
            // 根據 BranchId 查詢Branches資料表中整筆分館資料
            var branch = _context.Branches
                        .FirstOrDefault(b => b.BranchId == branchId);

            // 查詢該分館的啟用房型
            var roomTypes = _context.RoomTypes
                            .Where(r => r.BranchId == branchId && r.IsActive)
                            .ToList();

            // 建立 ViewModel
            var model = new RoomSelectionViewModel
            {
                BranchId = branchId,
                BranchName = branch?.BranchName ?? "",  //ps. ?? --> 如果左邊是 null，就使用右邊的值(空字串)
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                GuestCount = guestCount,

                RoomTypes = roomTypes.Select(r => new RoomTypeViewModel
                {
                    RoomTypeId = r.RoomTypeId,
                    RoomTypeName = r.RoomTypeName,
                    MaxOccupancy = r.MaxOccupancy,
                    BedType = r.BedType,
                    NightlyPrice = r.NightlyPrice,
                    Description = r.Description,
                    ImageUrl = r.ImageUrl
                }).ToList()

            };

            return View(model);
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
