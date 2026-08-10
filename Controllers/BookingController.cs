using Microsoft.AspNetCore.Mvc;
using HotelManagementSystem.Models;

namespace HotelManagementSystem.Controllers
{
    public class BookingController : Controller
    {
        [HttpGet]
        public IActionResult RoomSelection(string branchName, DateTime checkIn, DateTime checkOut, int guests)
        {
            // 接收第一頁傳來的參數，打包進 ViewModel
            var model = new RoomSelectionViewModel
            {
                BranchName = branchName,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Guests = guests,

                // 模擬房型資料 (未來 DB 建好後再換成 LINQ 撈資料)
                RoomList = new List<RoomItemDto>
                {
                    new RoomItemDto { RoomTypeId = 1, RoomName = "標準雙人房", Description = "1大床・最多2人", Price = 2400, Stock = 3 },
                    new RoomItemDto { RoomTypeId = 2, RoomName = "精緻雙床房", Description = "2小床・最多2人", Price = 2800, Stock = 1 },
                    new RoomItemDto { RoomTypeId = 3, RoomName = "豪華套房", Description = "1特大床・最多2人", Price = 4500, Stock = 0 } // 測試無庫存狀況
                }
            };

            // 把 model 帶到 RoomSelection.cshtml 畫面
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
