using HotelManagementSystem.Models;
using HotelManagementSystem.Services;
using HotelManagementSystem.Models.BookingSearchModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using HotelManagementSystem.Models.Entities;

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

        // 轉換前端狀態字串和資料庫相同
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

        //驗證員工ID是否存在
        private async Task<int?> EmployeeVerify(string EmpNum)
        {
             var q= await _context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.EmployeeNumber == EmpNum);
             return q?.BranchId;
            // 未來調整回傳值
        }

        //查詢訂單
        [HttpGet]
        public async Task<IActionResult> BookingSearch(string keyword, string dateRange, string bookingStatus)
        {
            // 待帳號驗證(session取得 employeenum)
            string EmployeeNum = "E20260807002";

            //驗證員工ID是否存在
            int? BranchId = await EmployeeVerify(EmployeeNum);
            if (BranchId == null)
            {
                ViewBag.Error = "帳號驗證異常，即將返回登入畫面";
                return View(new List<BookingData>());
            }


            // 送回前端保存查詢欄位用
            ViewBag.Keyword = keyword;
            ViewBag.DateRange = dateRange;
            ViewBag.BookingStatus = bookingStatus;

            List<BookingData> _bookingData = new List<BookingData>();

            // 驗證搜尋為空則傳回空資料
            if (string.IsNullOrEmpty(keyword))
            {
                return View(_bookingData);
            }

            // phone正規化


            var query = _context.Bookings.AsNoTracking();
            query = query.Where(x => x.BranchId == BranchId);
            // keyword模糊查詢資料庫 及 分館ID (此處需要驗證員工帳號及帳號所屬分館取得)
            if (!string.IsNullOrWhiteSpace(keyword))
            {                
                query = query.Where(x => x.BookingNumber!.Contains(keyword) || x.BookerName!.Contains(keyword) || x.ContactPhone!.Contains(keyword));
            }

            // dateRange查詢時間範圍 (此處考慮加入前端預設時間段 避免訂單結果爆量讀取過久)
            if (!string.IsNullOrWhiteSpace(dateRange))
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


            // bookingstatu查詢訂單狀態
            if (!string.IsNullOrWhiteSpace(bookingStatus))
            {
                query = query.Where(x => x.BookingStatus == StatusLanguage(bookingStatus));
            }
            // 需要修改成await用法

            var result = query.Select(x => new BookingData
            {
                BookingNum = x.BookingNumber,
                BookingDate = x.CreatedAt,
                Name = x.BookerName,
                Phone = x.ContactPhone,
                Roomtype = x.RoomTypeNameSnapshot,
                BookingStatus = StatusLanguage(x.BookingStatus),
                StartDate = new DateTime(x.CheckInDate.Year, x.CheckInDate.Month, x.CheckInDate.Day),
                EndDate = new DateTime(x.CheckOutDate.Year, x.CheckOutDate.Month, x.CheckOutDate.Day),
                Price = "NT$ " + x.TotalAmount.ToString("N0")
            }).ToListAsync();
            

            return View(result);
        }

        // 取消訂單 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookingCancel(string bookingNum, string keyword, string dateRange,
            string keyStatus, string cancelCause ,string cancelReason)
        {
            // 待取得分館員工
            string EmployeeNum = "E20260807002";

            // 員工驗證
            int? BranchId = await EmployeeVerify(EmployeeNum);
            if (BranchId == null)
            {
                ViewBag.VerifyError = "帳號驗證異常，即將返回登入畫面";
                return View("BookingSearch",new List<BookingData>());
            }


            // 此處錯誤有解方嗎? 假如訂單狀態錯誤會是什麼情況 在使用者不寫程式的情況可以修好嗎
            // 查詢訂單
            var result = _context.Bookings.FirstOrDefault(x => x.BookingNumber == bookingNum);
            if(result == null || result.BookingStatus!= "Paid")
            {
                TempData["BookingStatusError"] = "訂單狀態錯誤，無法取消訂單";
                return RedirectToAction("BookingSearch", new {  keyword, dateRange, bookingStatus = keyStatus });
            }

            //判斷取消因素是否正確
            if (cancelCause!= "顧客因素" && cancelCause != "飯店因素")
            {
                TempData["BookingStatusError"] = "取消訂單資料錯誤，無法取消訂單";
                return RedirectToAction("BookingSearch", new { keyword, dateRange, bookingStatus = keyStatus });
            }
            result.CancellationCause = cancelCause== "顧客因素"? "GuestRequest": "HotelUnableToFulfill";

            if (string.IsNullOrWhiteSpace(cancelReason.Trim()))
            {
                TempData["BookingStatusError"] = "取消理由不可為空";
                return RedirectToAction("BookingSearch", new { keyword, dateRange, bookingStatus = keyStatus });
            }
            result.CancellationReason = cancelReason;

            result.CancelledAt = _Clock.Now;

            result.CancelledByEmployeeNumber = EmployeeNum;

            result.BookingStatus = "Cancelled";
            
            //System.Diagnostics.Debug.WriteLine("==================!!!!!=================");
            try
            {
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateException)
            {
                TempData["BookingStatusError"] = "發生不可避免的錯誤！";
                return RedirectToAction("BookingSearch", new { keyword, dateRange, bookingStatus = keyStatus });
            }

            return RedirectToAction("BookingSearch", new {keyword, dateRange, bookingStatus = keyStatus });
        }


    }
}
