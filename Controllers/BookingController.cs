using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels.Booking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            // 查詢該分館 符合所選房型人數 且 啟用 的房型
            var roomTypes = _context.RoomTypes
                .Include(r => r.Rooms)
                .Where(r => 
                r.BranchId == branchId && 
                r.MaxOccupancy == guestCount && 
                r.IsActive
                )
                .ToList();

            // 查詢日期重疊且未取消的訂單
            var bookings = _context.Bookings
                .Where(b =>
                    b.BranchId == branchId &&
                    b.CheckInDate < DateOnly.FromDateTime(checkOut) &&
                    b.CheckOutDate > DateOnly.FromDateTime(checkIn) &&
                    b.BookingStatus != "Cancelled"
                )
                .ToList();
            


            // 建立 ViewModel
            var model = new RoomSelectionViewModel
            {
                BranchId = branchId,
                BranchName = branch?.BranchName ?? "",  //ps. ?? --> 如果左邊是 null，就使用右邊的值(空字串)
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                GuestCount = guestCount,

                RoomTypes = roomTypes.Select(r =>
                {
                    // 該房型目前可售的房間數
                    var availableCount = r.Rooms
                        .Count(room => room.SupplyStatus == "Open");

                    // 該房型在查詢日期內已被訂的數量
                    var bookingCount = bookings
                        .Count(b => b.RoomTypeId == r.RoomTypeId);

                    // 剩餘房數
                    var remainingCount = availableCount - bookingCount;

                    return new RoomTypeViewModel
                    {
                        RoomTypeId = r.RoomTypeId,
                        RoomTypeName = r.RoomTypeName,
                        MaxOccupancy = r.MaxOccupancy,
                        BedType = r.BedType,
                        NightlyPrice = r.NightlyPrice,
                        Description = r.Description,
                        ImageUrl = r.ImageUrl,

                        AvailableRooms = remainingCount
                    };
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
