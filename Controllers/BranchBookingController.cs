using HotelManagementSystem.Models;
using HotelManagementSystem.Models.BookingSearchModel;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public class BranchBookingController : BranchEmployeeControllerBase
    {
        private readonly TaipeiClock _Clock;
        private readonly HotelManagementContext _context;
        private readonly NoShowService _noShowService;
        public BranchBookingController(HotelManagementContext context, TaipeiClock clock, NoShowService noShowService)
            : base(context)
        {
            _context = context;
            _Clock = clock;
            _noShowService = noShowService;
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

        //查詢訂單
        [HttpGet]
        public async Task<IActionResult> BookingSearch(string keyword, string dateRange, string bookingStatus)
        {
            // 刷新 noshow
            await _noShowService.UpdateNoShowsAsync();

            // 送回前端保存查詢欄位用
            ViewBag.Keyword = keyword;
            ViewBag.DateRange = dateRange;
            ViewBag.BookingStatus = bookingStatus;

            List<BookingData> _bookingData = new List<BookingData>();

            // 驗證搜尋為空則傳回空資料
            if (string.IsNullOrEmpty(keyword) || string.IsNullOrWhiteSpace(keyword))
            {
                return View(_bookingData);
            }

            var query = _context.Bookings.AsNoTracking();
            query = query.Where(x => x.BranchId == CurrentBranchId);
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
                        && x.CheckInDate <= DateOnly.FromDateTime(endDate));
                    }
                }
            }


            // bookingstatu查詢訂單狀態
            if (!string.IsNullOrWhiteSpace(bookingStatus))
            {
                query = query.Where(x => x.BookingStatus == StatusLanguage(bookingStatus));
            }


            _bookingData = await query.Select(x => new BookingData
            {
                BookingNum = x.BookingNumber,
                BookingDate = x.CreatedAt,
                Name = x.BookerName,
                Phone = x.ContactPhone,
                Roomtype = x.RoomTypeNameSnapshot,
                BookingStatus = x.BookingStatus,
                StartDate = new DateTime(x.CheckInDate.Year, x.CheckInDate.Month, x.CheckInDate.Day),
                EndDate = new DateTime(x.CheckOutDate.Year, x.CheckOutDate.Month, x.CheckOutDate.Day),
                Price = "NT$ " + x.TotalAmount.ToString("N0")
            }).ToListAsync();

            foreach (var b in _bookingData)
            {
                b.BookingStatus = StatusLanguage(b.BookingStatus);
            }

            return View(_bookingData);
        }

        // 加入operationLog
        // 取消訂單 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookingCancel(string bookingNum, string keyword, string dateRange,
            string keyStatus, string cancelCause, string cancelReason)
        {
            var now = _Clock.Now;
            await _noShowService.UpdateNoShowsAsync();

            // 查詢訂單
            var result = _context.Bookings.FirstOrDefault(x => x.BookingNumber == bookingNum && x.BranchId == CurrentBranchId && x.StayRecord == null);
            if (result == null || result.BookingStatus != "Paid")
            {
                TempData["BookingStatusError"] = "訂單狀態錯誤，目前無法取消訂單";
                return RedirectToAction("BookingSearch", new { keyword, dateRange, bookingStatus = keyStatus });
            }

            // 判斷顧客因素+是否超過取消時間

            if (cancelCause == "顧客因素" && DateOnly.FromDateTime(now) >= result.CheckInDate)
            {
                TempData["BookingStatusError"] = "超過顧客取消時間，無法取消訂單";
                return RedirectToAction("BookingSearch", new { keyword, dateRange, bookingStatus = keyStatus });
            }


            //判斷取消因素是否正確
            if (cancelCause != "顧客因素" && cancelCause != "飯店因素")
            {
                TempData["BookingStatusError"] = "取消訂單資料錯誤，無法取消訂單";
                return RedirectToAction("BookingSearch", new { keyword, dateRange, bookingStatus = keyStatus });
            }
            result.CancellationCause = cancelCause == "顧客因素" ? "GuestRequest" : "HotelUnableToFulfill";

            // 限制500字
            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                TempData["BookingStatusError"] = "取消理由不可為空";
                return RedirectToAction("BookingSearch", new { keyword, dateRange, bookingStatus = keyStatus });
            }
            if (cancelReason.Length > 500)
            {
                TempData["BookingStatusError"] = "取消理由超過500字";
                return RedirectToAction("BookingSearch", new { keyword, dateRange, bookingStatus = keyStatus });
            }

            result.CancellationReason = cancelReason;

            result.CancelledAt = now;

            result.CancelledByEmployeeNumber = CurrentEmployeeNumber;

            result.BookingStatus = "Cancelled";


            // 新增操作紀錄
            var operationLog = new OperationLog
            {
                TargetBranchId = result.BranchId,
                OperatedAt = now,
                OperatorEmployeeNumber = CurrentEmployeeNumber!,
                OperationTypeId = 21,
                TargetType = "Booking",
                TargetIdentifier = result.BookingNumber,
                Description = $"因{cancelCause}取消訂單 {result.BookingNumber}。"
            };
            _context.OperationLogs.Add(operationLog);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["BookingStatusError"] = "發生不可避免的錯誤，請重新操作訂單。";
                return RedirectToAction("BookingSearch", new { keyword, dateRange, bookingStatus = keyStatus });
            }

            return RedirectToAction("BookingSearch", new { keyword = bookingNum, dateRange, bookingStatus = "" });
        }


    }
}
