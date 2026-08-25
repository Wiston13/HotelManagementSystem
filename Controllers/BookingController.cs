using Microsoft.AspNetCore.Mvc;
using HotelManagementSystem.Services;
using HotelManagementSystem.Models;
using HotelManagementSystem.Models.BookingSearchModel;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;


namespace HotelManagementSystem.Controllers
{
    public class BookingController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly TaipeiClock _Clock;
        private readonly NoShowService _noShowService;
        public BookingController(HotelManagementContext context,TaipeiClock clock, NoShowService noShowService)
        {
            _context = context;
            _Clock = clock;
            _noShowService = noShowService;
        }
        

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


        //把資料庫BookingStatus英文轉成中文
        private string StatusToChenese(string input)
        {
            string output = "";
            switch (input)
            {
                case "Completed":
                    output = "已完成";
                    break;
                case "CheckedIn":
                    output = "入住中";
                    break;
                case "Cancelled":
                    output = "已取消";
                    break;
                case "NoShow":
                    output = "逾期未入住";
                    break;
                case "Paid":
                    output = "已付款";
                    break;
            }
            return output;
        }

        [HttpGet]
        public async Task<IActionResult> Lookup(string BookingNum,string Phone)
        {
            var model = new BookingData();

            // 檢查bookingNum 和phone的值是否為空
            if (string.IsNullOrWhiteSpace(BookingNum)||string.IsNullOrWhiteSpace(Phone))
            {
                return View(model);
            }

            await _noShowService.UpdateNoShowsAsync();

            BookingNum = BookingNum.Trim();

            //前端phone正規化
            Phone = Phone.Trim();
            Phone = Regex.Replace(Phone, "-", "");
            if (!Phone.All(char.IsDigit))
            {
                return View(model);
            }

            // 查詢
            var booking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.BookingNumber == BookingNum &&b.ContactPhone==Phone);

            // 沒結果吐回空資料及noresult 
            if (booking == null)
            {
                model.BookingNum = BookingNum;
                model.Phone = Phone;
                ViewBag.NoResult = true;
                return View(model);
            }

            NoShowService _noshow = new NoShowService(_Clock, _context);

            // 查詢訂單分館
            var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.BranchId == booking!.BranchId);

            // 打包結果
            model.BookingNum = booking.BookingNumber;
            model.Phone = booking.ContactPhone;
            model.BranchName = branch?.BranchName;
            model.Roomtype = booking.RoomTypeNameSnapshot;
            model.StartDate = new DateTime(booking.CheckInDate.Year, booking.CheckInDate.Month, booking.CheckInDate.Day);
            model.EndDate = new DateTime(booking.CheckOutDate.Year, booking.CheckOutDate.Month, booking.CheckOutDate.Day); 
            model.BookingDate = booking.CreatedAt;
            model.Name = booking.BookerName;
            model.Price = booking.TotalAmount.ToString("N0");
            model.BookingStatus = StatusToChenese(booking.BookingStatus);
            return View(model);

        }
    }
}