using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels.Booking;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using HotelManagementSystem.Models.BookingSearchModel;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;


namespace HotelManagementSystem.Controllers
{
    public class BookingController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly NoShowService _noShowService;
        private readonly TaipeiClock _taipeiClock;

        public BookingController(HotelManagementContext context, TaipeiClock taipeiClock, NoShowService noShowService)
        {
            _context = context;
            _taipeiClock = taipeiClock;
            _noShowService = noShowService;
        }

       

        // 計算 某個房型 在指定住宿期間 的 最低剩餘房量
        private int CalculateMinimumRemainingRooms(int roomTypeId, DateOnly checkIn, DateOnly checkOut)
        {
            var now = _taipeiClock.Now;
            var today = _taipeiClock.Today;

            // 飯店退房時間為 12:00
            var checkOutTime = new TimeSpan(12, 0, 0);

            // 判斷目前時間是否已超過退房時間
            var isAfterCheckOutTime = now.TimeOfDay >= checkOutTime;


            // 取得可售房量（該房型 供應狀態為 Open 的房間總數）
            var availableCount = _context.Rooms
                .Count(r => 
                r.RoomTypeId == roomTypeId && 
                r.SupplyStatus == "Open");

            // 日期重疊的有效訂單
            var bookings = _context.Bookings
                .Where(b => 
                    b.RoomTypeId == roomTypeId &&
                    b.CheckInDate < checkOut && b.CheckOutDate > checkIn &&
                    (b.BookingStatus == "Paid" || b.BookingStatus == "CheckedIn")
                ).ToList(); 
            

            // 逐晚計算，取整段住宿期間最低剩餘房量
            var minimumRemaining = availableCount;

            for (var date = checkIn; date < checkOut; date = date.AddDays(1))
            {
                // 該晚正常有效訂單占用數量
                var bookingCount = bookings
                    .Count(b =>
                        b.CheckInDate <= date &&
                        b.CheckOutDate > date);

                // 該晚剩餘房量
                var remaining = availableCount - bookingCount;

                // 當日期為今天且已達退房時間，再扣除逾期未退房房量
                if (date == today && isAfterCheckOutTime)
                {
                    var overdueCount = _context.StayRecords
                        .Count(s =>
                            s.ActualCheckOutAt == null &&
                            s.Room.RoomTypeId == roomTypeId &&
                            s.BookingNumberNavigation.CheckOutDate == today);

                    remaining -= overdueCount;
                }

                // 取整段住宿期間的最低剩餘房量
                minimumRemaining = Math.Min(minimumRemaining, remaining);
            }

            return minimumRemaining;
        }





        [HttpGet]
        public IActionResult RoomSelection(int branchId, DateOnly checkIn, DateOnly checkOut, int guestCount)
        {
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
                    // 依照統一的房量規則 計算該房型 剩餘房量
                    var remainingCount = CalculateMinimumRemainingRooms(r.RoomTypeId, checkIn, checkOut);

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



        [HttpPost]
        public IActionResult Success(int branchId, DateOnly checkIn, DateOnly checkOut, int roomTypeId, string bookerName, string contactPhone, string email, int guestCount)
        {
            // 根據付款頁傳來的 branchId 找該分館資料
            var branch = _context.Branches.FirstOrDefault(b => b.BranchId == branchId && b.AcceptsNewBookings);

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
                    WHERE RoomTypeId = {roomTypeId} AND BranchId = {branchId} AND IsActive = 1")
                    .FirstOrDefault();

                if (roomType == null)
                {
                    return NotFound("找不到指定的房型。");
                }
                if (roomType.MaxOccupancy != guestCount)
                {
                    return BadRequest("房型入住人數與訂房人數不符。");
                }


                // 再次確認整段住宿期間的最低剩餘房量
                var remainingCount = CalculateMinimumRemainingRooms(roomTypeId, checkIn, checkOut);

                // 房量不足，不建立訂單，回到房型選擇頁重新查詢
                if (remainingCount < 1)
                {
                    return RedirectToAction("RoomSelection", new
                    {
                        branchId = branchId,
                        checkIn = checkIn,
                        checkOut = checkOut,
                        guestCount = guestCount
                    });
                }

                // 產生訂單編號
                var now = _taipeiClock.Now;
                var today = now.Date;
                var tomorrow = today.AddDays(1);

                var prefix = $"BK{now:yyMMdd}{roomTypeId:D4}";

                var lastBookingNumber = _context.Bookings
                    .Where(b =>
                        b.RoomTypeId == roomTypeId &&
                        b.CreatedAt >= today &&
                        b.CreatedAt < tomorrow &&
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

                    BranchId = branchId,
                    RoomTypeId = roomTypeId,

                    BookerName = bookerName,
                    ContactPhone = contactPhone,
                    Email = email,

                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,

                    RoomTypeNameSnapshot = roomType.RoomTypeName,
                    MaxOccupancySnapshot = roomType.MaxOccupancy,
                    NightlyPriceSnapshot = roomType.NightlyPrice,

                    TotalAmount = roomType.NightlyPrice * (checkOut.DayNumber - checkIn.DayNumber),

                    BookingStatus = "Paid",
                    CreatedAt = _taipeiClock.Now
                };

                // 加入 DbContext
                _context.Bookings.Add(booking);

                // 寫入資料庫
                _context.SaveChanges();

                // 交易完成
                transaction.Commit();


                // 建立 SuccessViewModel
                var model = new SuccessViewModel
                {
                    BookingNumber = bookingNumber,
                    BranchName = branch.BranchName,
                    RoomTypeName = roomType.RoomTypeName,
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,
                    Email = email
                };
                return View(model);

            }
            catch 
            {
                transaction.Rollback();
                throw;
            }
            
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