using HotelManagementSystem.Models;
using HotelManagementSystem.Services;
using HotelManagementSystem.Models.BookingSearchModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

/*
 員工帳號驗證:
 每個ACTION都要驗證員工身分，員工身分怎麼驗證?
 employeeNum  用session抓  但session週期只有一次轉跳
 有filter做驗證 或更好做法
 但有同時需要branchID 和 employeeNum嗎?
 只要有employeeNum就可以抓資料庫判斷分館了
 那這樣會有危險嗎?
 例如轉跳action帶的employee是假的 又或者裝到_layout或哪裡的Model 或viewdata tempdata
 viewdata和viewbag->轉跳一次請求消失 tempdata轉跳存成session 
 
 每步操作要驗證資料庫正確性:
 更新時加入判斷資料庫的資料是否有誤 例如paid->cancelled 確定是不是已經是cancelled 訂單資料是否一樣

 可優化:
*訂單讀取時間
*前端取消的訂單 有取消理由
*取消後單獨顯示取消的訂單?或更好的做法
*即時更改選單刷新訂單
*訂單類別排序

 
 */

namespace HotelManagementSystem.Controllers
{
    public class BranchBookingController : Controller
    {
        private readonly TaipeiClock _Clock;        
        private readonly HotelManagementContext _context;
        public BranchBookingController(HotelManagementContext context, TaipeiClock clock)
        {
            _context = context;
            _Clock = clock;
        }

        private string StatusLanguage(string input)
        {
            string output = "";
            switch (input)
            {
                case "Completed": output = "已完成"; break;
                case "CheckedIn": output = "入住中"; break;
                case "Cancelled": output = "已取消"; break;
                case "NoShow": output = "逾期未入住"; break;
                case "Paid": output = "已付款"; break;
                case "已完成": output = "Completed"; break;
                case "入住中": output = "CheckedIn"; break;
                case "已取消": output = "Cancelled"; break;
                case "逾期未入住": output = "NoShow"; break;
                case "已付款": output = "Paid"; break;
            }
            return output;
        }

        public IActionResult BookingSearch(string keyword, string dateRange, string bookingStatus)
        {
            int BranchId = 1;//銜接分館判斷
            ViewBag.Keyword = keyword;
            ViewBag.DateRange = dateRange;
            ViewBag.BookingStatus = bookingStatus;
            if (string.IsNullOrEmpty(keyword))
            {
                return View(new List<BookingData>());
            }

            List<BookingData> _bookingData = new List<BookingData>();

            var allData = _context.Bookings;
            var query = allData.AsQueryable();


            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => (x.BookingNumber!.Contains(keyword) || x.BookerName!.Contains(keyword) || x.ContactPhone!.Contains(keyword)) && x.BranchId == BranchId);
            }

            if (!string.IsNullOrEmpty(dateRange))
            {
                var dates = dateRange.Split(" - ");
                if (dates.Length == 2)
                {
                    if (DateTime.TryParse(dates[0], out DateTime startDate) && DateTime.TryParse(dates[1], out DateTime endDate))
                    {
                        query = query.Where(x => x.CheckInDate >= DateOnly.FromDateTime(startDate)
                        && x.CheckOutDate <= DateOnly.FromDateTime(endDate));
                    }
                }
            }

            if (!string.IsNullOrEmpty(bookingStatus))
            {
                query = query.Where(x => x.BookingStatus == StatusLanguage(bookingStatus));
            }

            foreach (var item in query)
            {
                _bookingData.Add(new BookingData
                {
                    BookingNum = item.BookingNumber,
                    BookingDate = item.CreatedAt,
                    Name = item.BookerName,
                    Phone = item.ContactPhone,
                    Roomtype = item.RoomTypeNameSnapshot,
                    BookingStatus = StatusLanguage(item.BookingStatus),
                    StartDate = new DateTime(item.CheckInDate.Year, item.CheckInDate.Month, item.CheckInDate.Day) ,
                    EndDate = new DateTime(item.CheckOutDate.Year, item.CheckOutDate.Month, item.CheckOutDate.Day),
                    Price = "NT$ " + item.NightlyPriceSnapshot.ToString("N0")
                });
            }

            var result = _bookingData.ToList();

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BookingCancel(string bookingNum, string keyword, string dateRange,
            string keyStatus, string cancelCause ,string cancelReason, string employeeNum)
        {
            var result = _context.Bookings.FirstOrDefault(x => x.BookingNumber == bookingNum);
            if(result == null ||(cancelCause != "顧客因素" && cancelCause != "飯店因素"))
            {
                return RedirectToAction("BookingSearch", new {  keyword, dateRange, bookingStatus = keyStatus });
            }


            result.BookingStatus = "Cancelled";
            result.CancellationCause = cancelCause== "顧客因素"? "GuestRequest": "HotelUnableToFulfill";
            result.CancellationReason = cancelReason;
            result.CancelledAt = _Clock.Now;
            result.CancelledByEmployeeNumber = employeeNum;
            //System.Diagnostics.Debug.WriteLine("==================!!!!!=================");
            _context.SaveChanges();

            return RedirectToAction("BookingSearch", new {keyword, dateRange, bookingStatus = keyStatus });
        }

    }
}
