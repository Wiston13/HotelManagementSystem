using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels.Booking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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
        public IActionResult RoomSelection(int branchId, DateOnly checkIn, DateOnly checkOut, int guestCount)
        {
            // 根據首頁傳來的 branchId 查詢 資料庫 Branches資料表中 該分館的整筆資料
            var branch = _context.Branches
                .FirstOrDefault(b => b.BranchId == branchId);

            // 如果找不到分館
            if (branch == null)
            {
                return NotFound("找不到指定的分館。");
            }

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
                    b.CheckInDate < checkOut &&
                    b.CheckOutDate > checkIn &&
                    b.BookingStatus != "Cancelled"
                )
                .ToList();

            // 計算入住晚數
            var nights = checkOut.DayNumber - checkIn.DayNumber;


            // 建立 RoomSelectionViewModel
            var model = new RoomSelectionViewModel
            {
                BranchId = branchId,
                BranchName = branch.BranchName,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Nights = nights,
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




        public IActionResult Payment(int branchId, DateOnly checkIn, DateOnly checkOut, int roomTypeId, int guestCount)
        {
            // 根據房型選擇頁傳來的 branchId 找該分館資料
            var branch = _context.Branches.FirstOrDefault(b => b.BranchId == branchId);

            if (branch == null)
            {
                return NotFound("找不到指定的分館。");
            }

            // 根據房型選擇頁傳來的 roomTypeId 找該房型資料
            var roomType = _context.RoomTypes.FirstOrDefault(rt => rt.RoomTypeId == roomTypeId);

            // 如果找不到房型
            if (roomType == null)
            {
                return NotFound("找不到指定的房型。");
            }

            // 根據房型選擇頁傳來的 checkIn, checkOut 計算入住晚數
            var nights = checkOut.DayNumber - checkIn.DayNumber;

            // 計算總金額
            var totalPrice = roomType.NightlyPrice * nights;


            // 建立 PaymentViewModel
            var model = new PaymentViewModel
            {
                BranchId = branchId,
                BranchName = branch.BranchName,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Nights = nights,
                RoomTypeId = roomTypeId,
                RoomTypeName = roomType.RoomTypeName,
                NightlyPrice = roomType.NightlyPrice,
                TotalPrice = totalPrice,
                GuestCount = guestCount
            };

            return View(model);
        }




        public IActionResult Success(int branchId, DateOnly checkIn, DateOnly checkOut, int roomTypeId, string bookerName, string contactPhone, string email)
        {
            // 根據付款頁傳來的 branchId 找該分館資料
            var branch = _context.Branches.FirstOrDefault(b => b.BranchId == branchId);

            if (branch == null)
            {
                return NotFound("找不到指定的分館。");
            }

            // 根據付款頁傳來的 roomTypeId 找該房型資料，同時載入關聯的Rooms資料
            var roomType = _context.RoomTypes
                .Include(rt => rt.Rooms)
                .FirstOrDefault(rt => rt.RoomTypeId == roomTypeId);

            if (roomType == null)
            {
                return NotFound("找不到指定的房型。");
            }


            // 再次確認剩餘房量：
            // 1.計算可售房量
            var availableCount = roomType.Rooms.Count(r => r.SupplyStatus == "Open");
            // 2.查詢日期重疊且未取消的訂單筆數
            var bookingCount = _context.Bookings
                .Count(b =>
                    b.BranchId == branchId &&
                    b.RoomTypeId == roomTypeId &&
                    b.CheckInDate < checkOut &&
                    b.CheckOutDate > checkIn &&
                    b.BookingStatus != "Cancelled"
                );
            // 3.算剩餘房量
            var remainingCount = availableCount - bookingCount;



            // 建立 SuccessViewModel
            var model = new SuccessViewModel
            {
                BookingNumber = "",
                BranchName = branch.BranchName,
                RoomTypeName = roomType.RoomTypeName,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Email = email
            };


            return View(model);
        }
        
        public IActionResult Lookup()
        {
            return View();
        }
    }
}
