using HotelManagementSystem.Models;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Models.ViewModels.Stay;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers
{
    public class StayController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly TaipeiClock _Clock;

        public StayController(HotelManagementContext context, TaipeiClock clock)
        {
            _context = context;
            _Clock = clock;
        }

        public IActionResult CheckOut()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CheckIn(string? bookingNumber)
        {
            var model = new CheckInViewModel
            {
                BookingNumber = bookingNumber
            };

            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                return View(model);
            }

            var booking = _context.Bookings.FirstOrDefault(b => b.BookingNumber == bookingNumber);

            if (booking == null)
            {
                model.ErrorMessage = "查無此訂單";
                return View(model);
            }

            if (booking.BookingStatus != "Paid")
            {
                model.ErrorMessage = "此訂單目前無法辦理入住";
                return View(model);
            }

            var checkInStart = booking.CheckInDate.ToDateTime(new TimeOnly(16, 0));
            var checkOutDeadline = booking.CheckOutDate.ToDateTime(new TimeOnly(12, 0));
            var now = _Clock.Now;

            if (now < checkInStart)
            {
                model.ErrorMessage = "尚未到可辦理入住時間";
                return View(model);
            }

            if (now >= checkOutDeadline)
            {
                model.ErrorMessage = "此訂單已超過可辦理入住時間";
                return View(model);
            }

            var hasStayRecord = _context.StayRecords.Any(s => s.BookingNumber == booking.BookingNumber);

            if (hasStayRecord)
            {
                model.ErrorMessage = "此訂單已建立住房紀錄";
                return View(model);
            }

            model.HasResult = true;
            model.BookerName = booking.BookerName;
            model.ContactPhone = booking.ContactPhone;
            model.RoomTypeName = booking.RoomTypeNameSnapshot;
            model.CheckInDate = booking.CheckInDate;
            model.CheckOutDate = booking.CheckOutDate;
            model.BookingStatus = booking.BookingStatus;
            model.MaxOccupancy = booking.MaxOccupancySnapshot;

            model.AvailableRooms = _context.Rooms
                .Where(r =>
                    r.BranchId == booking.BranchId &&
                    r.RoomTypeId == booking.RoomTypeId &&
                    r.SupplyStatus == "Open" &&
                    r.CleaningStatus == "Clean" &&
                    !r.StayRecords.Any(s => s.ActualCheckOutAt == null))
                .Select(r => new RoomOption
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber
                })
                .ToList();

            if (model.AvailableRooms.Count == 0)
            {
                model.ErrorMessage = "目前沒有可指派的房間";
                return View(model);
            }

            model.CanCheckIn = true;


            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckIn(CheckInViewModel inputModel)
        {
            var model = new CheckInViewModel
            {
                BookingNumber = inputModel.BookingNumber
            };

            if (string.IsNullOrWhiteSpace(inputModel.BookingNumber) ||
                !inputModel.SelectedRoomId.HasValue ||
                !inputModel.ActualGuestCount.HasValue)
            {
                model.ErrorMessage = "入住資料不完整";
                return View(model);
            }

            var booking = _context.Bookings.FirstOrDefault(b => b.BookingNumber == inputModel.BookingNumber);

            if (booking == null)
            {
                model.ErrorMessage = "查無此訂單";
                return View(model);
            }

            if (booking.BookingStatus != "Paid")
            {
                model.ErrorMessage = "此訂單目前無法辦理入住";
                return View(model);
            }

            var checkInStart = booking.CheckInDate.ToDateTime(new TimeOnly(16, 0));
            var checkOutDeadline = booking.CheckOutDate.ToDateTime(new TimeOnly(12, 0));
            var now = _Clock.Now;

            if (now < checkInStart)
            {
                model.ErrorMessage = "尚未到可辦理入住時間";
                return View(model);
            }

            if (now >= checkOutDeadline)
            {
                model.ErrorMessage = "此訂單已超過可辦理入住時間";
                return View(model);
            }

            var hasStayRecord = _context.StayRecords.Any(s => s.BookingNumber == booking.BookingNumber);

            if (hasStayRecord)
            {
                model.ErrorMessage = "此訂單已建立住房紀錄";
                return View(model);
            }

            if (inputModel.ActualGuestCount.Value <= 0 ||
                inputModel.ActualGuestCount.Value > booking.MaxOccupancySnapshot)
            {
                model.ErrorMessage = "實際入住人數不符合限制";
                return View(model);
            }

            var room = _context.Rooms.FirstOrDefault(r =>
                r.RoomId == inputModel.SelectedRoomId.Value &&
                r.BranchId == booking.BranchId &&
                r.RoomTypeId == booking.RoomTypeId &&
                r.SupplyStatus == "Open" &&
                r.CleaningStatus == "Clean" &&
                !r.StayRecords.Any(s => s.ActualCheckOutAt == null)
            );

            if (room == null)
            {
                model.ErrorMessage = "所選房間目前已無法辦理入住";
                return View(model);
            }

            var stayRecord = new StayRecord
            {
                BookingNumber = booking.BookingNumber,
                RoomId = room.RoomId,
                RoomNumberSnapshot = room.RoomNumber,
                ActualCheckInAt = now,
                PrimaryGuestName = booking.BookerName,
                ActualGuestCount = inputModel.ActualGuestCount.Value,
                CheckedInByEmployeeNumber = "E20260807002" // TODO 假設是登入的員工編號，實際應從登入資訊取得"
            };

            _context.StayRecords.Add(stayRecord);
            booking.BookingStatus = "CheckedIn";

            _context.SaveChanges();

            return Content("入住完成");
        }
    }
}
