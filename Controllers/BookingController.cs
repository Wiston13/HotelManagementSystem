using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels.Booking;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using HotelManagementSystem.Models.BookingSearchModel;
using System.Text.RegularExpressions;


namespace HotelManagementSystem.Controllers
{
    public class BookingController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly TaipeiClock _taipeiClock;
        private readonly NoShowService _noShowService;
        private readonly RoomAvailabilityService _roomAvailabilityService;
        

        public BookingController(HotelManagementContext context, TaipeiClock taipeiClock, NoShowService noShowService, RoomAvailabilityService roomAvailabilityService)
        {
            _context = context;
            _taipeiClock = taipeiClock;
            _noShowService = noShowService;
            _roomAvailabilityService = roomAvailabilityService;
        }



        [HttpGet]
        public IActionResult RoomSelection(int branchId, DateOnly checkIn, DateOnly checkOut, int guestCount)
        {
            // 日期後端驗證
            var today = _taipeiClock.Today;
            var maxBookingDate = today.AddDays(60);
            if (checkIn < today ||
                checkOut <= checkIn ||
                checkOut > maxBookingDate)
            {
                return BadRequest("入住或退房日期不符合可預訂範圍。");
            }

            // 根據首頁傳來的 branchId 查詢分館
            var branch = _context.Branches
                .FirstOrDefault(b => b.BranchId == branchId && b.AcceptsNewBookings);

            // 如果找不到分館
            if (branch == null)
            {
                return NotFound("找不到指定的分館，或該分館目前不開放訂房。");
            }

            // 查詢該分館 符合所選房型人數 且 啟用 的房型
            var roomTypes = _context.RoomTypes
                .Where(r => 
                    r.BranchId == branchId && 
                    r.MaxOccupancy == guestCount && 
                    r.IsActive
                ).ToList();            




            // 只留下目前仍有空房的房型
            var availableRoomTypes = roomTypes
                .Select(r =>
                {
                    // 依照統一的房量規則 計算該房型 剩餘房量
                    var remainingCount =
                        _roomAvailabilityService.CalculateMinimumRemainingRooms(
                            r.RoomTypeId,
                            checkIn,
                            checkOut);

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
                })
                .Where(r => r.AvailableRooms > 0)
                .ToList();


            // 沒有任何可預訂房型，回到首頁
            if (!availableRoomTypes.Any())
            {
                TempData["NoAvailableRoom"] =
                    "很抱歉，目前沒有符合查詢條件的可訂房間，請調整查詢條件後再試一次。";

                return RedirectToAction("Index", "Home");
            }

            // 計算入住晚數
            var nights = checkOut.DayNumber - checkIn.DayNumber;

            var model = new RoomSelectionViewModel
            {
                BranchId = branchId,
                BranchName = branch.BranchName,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Nights = nights,
                GuestCount = guestCount,
                RoomTypes = availableRoomTypes
            };

            return View(model);
        }



        private PaymentViewModel? GetPaymentViewModel(int branchId, DateOnly checkIn, DateOnly checkOut, int roomTypeId, int guestCount) 
        {
            // 根據房型選擇頁傳來的 branchId 找該分館資料
            var branch = _context.Branches.FirstOrDefault(b => b.BranchId == branchId);
            // 根據房型選擇頁傳來的 roomTypeId 找該房型資料
            var roomType = _context.RoomTypes.FirstOrDefault(rt => rt.RoomTypeId == roomTypeId);

            if (branch == null || roomType == null)
            {
                return null;
            }

            // 根據房型選擇頁傳來的 checkIn, checkOut 計算入住晚數
            var nights = checkOut.DayNumber - checkIn.DayNumber;

            // 建立 PaymentViewModel
            return new PaymentViewModel
            {
                BranchId = branchId,
                BranchName = branch.BranchName,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Nights = nights,
                RoomTypeId = roomTypeId,
                RoomTypeName = roomType.RoomTypeName,
                NightlyPrice = roomType.NightlyPrice,
                TotalPrice = roomType.NightlyPrice * nights,
                GuestCount = guestCount
            };
        }

        [HttpPost]
        [ActionName("Payment")]
        public IActionResult PaymentPost(int branchId, DateOnly checkIn, DateOnly checkOut, int roomTypeId, int guestCount)
        {
            // 日期後端驗證
            var today = _taipeiClock.Today;
            var maxBookingDate = today.AddDays(60);
            if (checkIn < today ||
                checkOut <= checkIn ||
                checkOut > maxBookingDate)
            {
                return BadRequest("入住或退房日期不符合可預訂範圍。");
            }

            var model = GetPaymentViewModel(branchId, checkIn, checkOut, roomTypeId, guestCount);

            if (model == null)
            {
                return NotFound("找不到指定的分館或房型。");
            }

            return View(model);
        }

        [HttpGet]
        [ActionName("Payment")]
        public IActionResult PaymentGet(int branchId, DateOnly checkIn, DateOnly checkOut, int roomTypeId, int guestCount)
        {
            // 日期後端驗證
            var today = _taipeiClock.Today;
            var maxBookingDate = today.AddDays(60);
            if (checkIn < today ||
                checkOut <= checkIn ||
                checkOut > maxBookingDate)
            {
                return BadRequest("入住或退房日期不符合可預訂範圍。");
            }

            var model = GetPaymentViewModel(branchId, checkIn, checkOut, roomTypeId, guestCount);

            if (model == null)
            {
                return NotFound("找不到指定的分館或房型。");
            }

            return View(model);
        }



        [HttpPost]
        public IActionResult Success(BookingPaymentInputViewModel input)
        {
            // ViewModel 基本驗證
            if (!ModelState.IsValid)
            {
                return BadRequest("填寫資料格式不正確。");
            }

            // 訂房人姓名去除前後空白
            input.BookerName = input.BookerName.Trim();

            // Email 去除前後空白
            input.Email = input.Email.Trim();

            // 聯絡電話驗證
            if (string.IsNullOrWhiteSpace(input.ContactPhone))
            {
                return BadRequest("請輸入聯絡電話。");
            }
            // 移除所有空白與半形"-"
            var normalizedPhone = new string(
                input.ContactPhone
                    .Where(c => !char.IsWhiteSpace(c) && c != '-')
                    .ToArray());
            // 正規化後必須有值，且只能包含 ASCII 0～9
            if (string.IsNullOrEmpty(normalizedPhone) ||
                !normalizedPhone.All(c => c >= '0' && c <= '9'))
            {
                return BadRequest("聯絡電話格式不正確。");
            }

            // 信用卡卡號正規化：移除空白
            var normalizedCardNumber = input.CardNumber.Replace(" ", "");
            // 卡號必須為 13～19 碼 ASCII 數字，並通過 Luhn 驗證
            if (normalizedCardNumber.Length < 13 ||
                normalizedCardNumber.Length > 19 ||
                !normalizedCardNumber.All(c => c >= '0' && c <= '9') ||
                !IsValidCardNumber(normalizedCardNumber))
            {
                return BadRequest("信用卡卡號格式不正確。");
            }

            // 信用卡到期日去除前後空白
            input.Expiry = input.Expiry.Trim();
            // 信用卡到期日格式驗證
            if (!IsValidExpiryFormat(input.Expiry))
            {
                return BadRequest("信用卡到期日格式不正確。");
            }
            // 信用卡到期日是否過期
            if (IsExpired(input.Expiry))
            {
                return BadRequest("信用卡已過期。");
            }

            // CVC 去除前後空白
            input.Cvc = input.Cvc.Trim();
            // CVC 必須為 3 碼 ASCII 數字
            if (input.Cvc.Length != 3 ||
                !input.Cvc.All(c => c >= '0' && c <= '9'))
            {
                return BadRequest("CVC 格式不正確。");
            }



            // 訂房日期後端驗證
            var today = _taipeiClock.Today;
            var maxBookingDate = today.AddDays(60);
            if (input.CheckInDate < today ||
                input.CheckOutDate <= input.CheckInDate ||
                input.CheckOutDate > maxBookingDate)
            {
                return BadRequest("入住或退房日期不符合可預訂範圍。");
            }

            // 根據付款頁傳來的 branchId 找該分館資料
            var branch = _context.Branches.FirstOrDefault(b => b.BranchId == input.BranchId && b.AcceptsNewBookings);
            if (branch == null)
            {
                return NotFound("找不到指定的分館，或該分館目前不開放訂房。");
            }


            // 交易開始
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var roomType = _context.RoomTypes.FromSqlInterpolated($@"
                    SELECT *
                    FROM RoomTypes WITH (UPDLOCK, HOLDLOCK)
                    WHERE RoomTypeId = {input.RoomTypeId} AND BranchId = {input.BranchId} AND IsActive = 1")
                    .FirstOrDefault();

                if (roomType == null)
                {
                    return NotFound("找不到指定的房型。");
                }
                if (roomType.MaxOccupancy != input.GuestCount)
                {
                    return BadRequest("房型入住人數與訂房人數不符。");
                }

                // 確認付款頁顯示的房價是否仍與目前資料庫房價一致
                if (input.ConfirmedNightlyPrice != roomType.NightlyPrice)
                {
                    TempData["PriceChanged"] = "房價已更新，請重新確認房型與最新價格後再繼續訂房。";

                    return RedirectToAction("RoomSelection", new
                    {
                        branchId = input.BranchId,
                        checkIn = input.CheckInDate,
                        checkOut = input.CheckOutDate,
                        guestCount = input.GuestCount
                    });
                }


                // 再次確認整段住宿期間的最低剩餘房量
                var remainingCount = _roomAvailabilityService.CalculateMinimumRemainingRooms(input.RoomTypeId, input.CheckInDate, input.CheckOutDate);

                // 選擇房型已無空房，回房型選擇頁重新查詢可訂房型
                if (remainingCount < 1)
                {
                    return RedirectToAction("RoomSelection", new
                    {
                        branchId = input.BranchId,
                        checkIn = input.CheckInDate,
                        checkOut = input.CheckOutDate,
                        guestCount = input.GuestCount
                    });
                }


                // 指定測試卡號模擬付款失敗
                if (normalizedCardNumber == "4000000000000002")
                {
                    TempData["PaymentError"] = "付款失敗，訂單尚未建立，請確認付款資訊後重新嘗試。";

                    return RedirectToAction("Payment", new
                    {
                        branchId = input.BranchId,
                        checkIn = input.CheckInDate,
                        checkOut = input.CheckOutDate,
                        roomTypeId = input.RoomTypeId,
                        guestCount = input.GuestCount
                    });
                }


                // 產生訂單編號
                var now = _taipeiClock.Now;
                var todayStartTime = now.Date;
                var tomorrowStartTime = todayStartTime.AddDays(1);

                var prefix = $"BK{now:yyMMdd}{input.RoomTypeId:D4}";

                var lastBookingNumber = _context.Bookings
                    .Where(b =>
                        b.RoomTypeId == input.RoomTypeId &&
                        b.CreatedAt >= todayStartTime &&
                        b.CreatedAt < tomorrowStartTime &&
                        b.BookingNumber.StartsWith(prefix))
                    .OrderByDescending(b => b.BookingNumber)
                    .Select(b => b.BookingNumber)
                    .FirstOrDefault();

                int nextSequence = 1;
                if (lastBookingNumber != null)
                {
                    var lastSequence = int.Parse(lastBookingNumber.Substring(lastBookingNumber.Length - 4));
                    nextSequence = lastSequence + 1;
                }

                var bookingNumber = $"{prefix}{nextSequence:D4}";

                // 建立訂單
                var booking = new Booking
                {
                    BookingNumber = bookingNumber,

                    BranchId = input.BranchId,
                    RoomTypeId = input.RoomTypeId,

                    BookerName = input.BookerName,
                    ContactPhone = normalizedPhone,
                    Email = input.Email,

                    CheckInDate = input.CheckInDate,
                    CheckOutDate = input.CheckOutDate,

                    RoomTypeNameSnapshot = roomType.RoomTypeName,
                    MaxOccupancySnapshot = roomType.MaxOccupancy,
                    NightlyPriceSnapshot = roomType.NightlyPrice,

                    TotalAmount = roomType.NightlyPrice * (input.CheckOutDate.DayNumber - input.CheckInDate.DayNumber),

                    BookingStatus = "Paid",
                    CreatedAt = _taipeiClock.Now
                };

                // 加入 DbContext
                _context.Bookings.Add(booking);

                // 寫入資料庫
                _context.SaveChanges();

                // 交易完成
                transaction.Commit();

                // Redirect 到 GET Success，避免重新整理時重複 POST
                return RedirectToAction(nameof(Success), new
                {
                    bookingNumber = bookingNumber
                });
            }
            catch 
            {
                transaction.Rollback();
                throw;
            }            
        }

        [HttpGet]
        public IActionResult Success(string bookingNumber)
        {
            var booking = _context.Bookings.FirstOrDefault(b => b.BookingNumber == bookingNumber);
            if (booking == null)
            {
                return NotFound("找不到指定的訂單。");
            }

            var branch = _context.Branches.FirstOrDefault(b => b.BranchId == booking.BranchId);
            if (branch == null)
            {
                return NotFound("找不到指定的分館。");
            }
            

            var model = new SuccessViewModel
            {
                BookingNumber = booking.BookingNumber,
                BranchName = branch.BranchName,
                RoomTypeName = booking.RoomTypeNameSnapshot,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                //Email = booking.Email
            };

            return View(model);
        }


        // =========================
        // Private Methods
        // =========================

        // 信用卡卡號 Luhn 驗證
        private bool IsValidCardNumber(string cardNumber)
        {
            var sum = 0;
            var shouldDouble = false;

            for (var i = cardNumber.Length - 1; i >= 0; i--)
            {
                var digit = cardNumber[i] - '0';

                if (shouldDouble)
                {
                    digit *= 2;

                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }
                sum += digit;
                shouldDouble = !shouldDouble;
            }
            return sum % 10 == 0;
        }

        // 信用卡到期日格式驗證
        private bool IsValidExpiryFormat(string expiry)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(
                expiry,
                @"^(0[1-9]|1[0-2])/\d{2}$");
        }

        // 信用卡到期日是否已過期
        private bool IsExpired(string expiry)
        {
            var expiryMonth = int.Parse(expiry.Substring(0, 2));
            var expiryYear = int.Parse(expiry.Substring(3, 2));

            var now = _taipeiClock.Now;
            var currentMonth = now.Month;
            var currentYear = now.Year % 100;

            return expiryYear < currentYear ||
                   (expiryYear == currentYear && expiryMonth < currentMonth);
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
        public async Task<IActionResult> Lookup(string BookingNum, string Phone)
        {
            var model = new BookingData();

            // 檢查bookingNum 和phone的值是否為空
            if (string.IsNullOrWhiteSpace(BookingNum) || string.IsNullOrWhiteSpace(Phone))
            {
                return View(model);
            }

            await _noShowService.UpdateNoShowsAsync();

            BookingNum = BookingNum.Trim();

            //前端phone正規化
            Phone = Phone.Trim();
            Phone = Regex.Replace(Phone, " ", "");
            Phone = Regex.Replace(Phone, "-", "");
            if (!Phone.All(char.IsDigit))
            {
                return View(model);
            }

            // 查詢
            var booking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.BookingNumber == BookingNum && b.ContactPhone == Phone);

            // 沒結果吐回空資料及noresult 
            if (booking == null)
            {
                model.BookingNum = BookingNum;
                model.Phone = Phone;
                ViewBag.NoResult = true;
                return View(model);
            }

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