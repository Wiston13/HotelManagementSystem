using HotelManagementSystem.Models;
using HotelManagementSystem.Models.BookingSearchModel;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Services;
using HotelManagementSystem.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class BranchBookingController : BranchEmployeeControllerBase
    {
        private readonly TaipeiClock _clock;
        private readonly HotelManagementContext _context;
        private readonly NoShowService _noShowService;
        public BranchBookingController(HotelManagementContext context, TaipeiClock clock, NoShowService noShowService)
            : base(context)
        {
            _context = context;
            _clock = clock;
            _noShowService = noShowService;
        }

        // 將前端中文篩選值轉為資料庫狀態碼。
        private static string GetBookingStatusCode(string input)
        {
            return input switch
            {
                "已付款" => "Paid",
                "入住中" => "CheckedIn",
                "已完成" => "Completed",
                "已取消" => "Cancelled",
                "逾期未入住" => "NoShow",
                _ => string.Empty
            };
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

            List<BookingData> bookingData = new List<BookingData>();

            // 驗證搜尋為空則傳回空資料
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return View(bookingData);
            }

            var query = _context.Bookings.AsNoTracking();
            query = query.Where(x => x.BranchId == CurrentBranchId);
            // keyword 模糊查詢資料庫與目前員工所屬分館。
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x => x.BookingNumber!.Contains(keyword) || x.BookerName!.Contains(keyword) || x.ContactPhone!.Contains(keyword));
            }

            // dateRange 查詢時間範圍
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
                query = query.Where(x => x.BookingStatus == GetBookingStatusCode(bookingStatus));
            }


            bookingData = await query.Select(x => new BookingData
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

            foreach (var b in bookingData)
            {
                b.BookingStatus = StatusDisplayHelper.GetBookingStatusText(b.BookingStatus);
            }

            return View(bookingData);
        }

        // 加入operationLog
        // 取消訂單 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookingCancel(string bookingNum, string keyword, string dateRange,
            string keyStatus, string cancelCause, string cancelReason)
        {
            var now = _clock.Now;
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
