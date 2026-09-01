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
        private readonly ILogger<BranchBookingController> _logger;
        public BranchBookingController(HotelManagementContext context, TaipeiClock clock, NoShowService noShowService, ILogger<BranchBookingController> logger)
            : base(context)
        {
            _context = context;
            _clock = clock;
            _noShowService = noShowService;
            _logger = logger;
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

        [HttpGet]
        public async Task<IActionResult> BookingSearch(string keyword, string dateRange, string bookingStatus)
        {
            await _noShowService.UpdateNoShowsAsync();

            // 送回前端保存查詢欄位用
            ViewBag.Keyword = keyword;
            ViewBag.DateRange = dateRange;
            ViewBag.BookingStatus = bookingStatus;

            List<BookingData> bookingData = new List<BookingData>();

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
                Price = "NT$ " + x.TotalAmount.ToString("N0"),
                Email = x.Email
            }).ToListAsync();

            foreach (var b in bookingData)
            {
                b.BookingStatus = StatusDisplayHelper.GetBookingStatusText(b.BookingStatus);
            }

            return View(bookingData);
        }


        // 修改指定訂單的 Email
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingEmail([FromBody] UpdateBookingEmailInputViewModel? input)
        {
            // 無法取得前端傳入的資料
            if (input == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "未收到修改 Email 所需的資料。"
                });
            }

            // 移除訂單編號及 Email 前後的空白
            input.BookingNumber = input.BookingNumber?.Trim() ?? string.Empty;
            input.NewEmail = input.NewEmail?.Trim() ?? string.Empty;

            // 使用整理後的內容重新執行 ViewModel 驗證
            ModelState.Clear();
            if (!TryValidateModel(input))
            {
                var errorMessage = ModelState.Values
                    .SelectMany(value => value.Errors)
                    .Select(error => error.ErrorMessage)
                    .FirstOrDefault(message =>
                        !string.IsNullOrWhiteSpace(message))
                    ?? "輸入資料格式不正確。";
                return BadRequest(new
                {
                    success = false,
                    message = errorMessage
                });
            }

            // 只允許查詢並修改目前員工所屬分館的訂單
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(booking =>
                    booking.BookingNumber == input.BookingNumber &&
                    booking.BranchId == CurrentBranchId);
            if (booking == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "找不到指定訂單，或無法執行此操作。"
                });
            }

            // 訂房確認信目前只適用於已付款訂單
            if (booking.BookingStatus != "Paid")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "只有已付款訂單可以修改確認信 Email。"
                });
            }

            // 新 Email 與目前 Email 相同時，不需要更新資料庫
            if (string.Equals(
                booking.Email,
                input.NewEmail,
                StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new
                {
                    success = true,
                    email = booking.Email,
                    message = "Email 未變更。"
                });
            }

            // 更新訂單的 Email
            booking.Email = input.NewEmail;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                // 不記錄完整 Email，避免個資出現在 Log 中
                _logger.LogError(
                    exception,
                    "修改訂單 Email 時發生資料庫錯誤。BookingNumber: {BookingNumber}",
                    booking.BookingNumber);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Email 暫時無法修改，請稍後再試。"
                    });
            }
            return Ok(new
            {
                success = true,
                email = booking.Email,
                message = "Email 已修改完成。"
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookingCancel(string bookingNum, string keyword, string dateRange,
            string keyStatus, string cancelCause, string cancelReason)
        {
            var now = _clock.Now;
            await _noShowService.UpdateNoShowsAsync();

            var result = _context.Bookings.FirstOrDefault(x => x.BookingNumber == bookingNum && x.BranchId == CurrentBranchId && x.StayRecord == null);
            if (result == null || result.BookingStatus != "Paid")
            {
                TempData["BookingStatusError"] = "訂單狀態錯誤，目前無法取消訂單";
                return RedirectToAction("BookingSearch", new { keyword, dateRange, bookingStatus = keyStatus });
            }

            // 顧客因素僅能在入住日前取消。
            if (cancelCause == "顧客因素" && DateOnly.FromDateTime(now) >= result.CheckInDate)
            {
                TempData["BookingStatusError"] = "超過顧客取消時間，無法取消訂單";
                return RedirectToAction("BookingSearch", new { keyword, dateRange, bookingStatus = keyStatus });
            }
            // 取消因素僅允許顧客因素或飯店因素。
            if (cancelCause != "顧客因素" && cancelCause != "飯店因素")
            {
                TempData["BookingStatusError"] = "取消訂單資料錯誤，無法取消訂單";
                return RedirectToAction("BookingSearch", new { keyword, dateRange, bookingStatus = keyStatus });
            }
            result.CancellationCause = cancelCause == "顧客因素" ? "GuestRequest" : "HotelUnableToFulfill";

            // 取消理由上限為 500 字。
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
