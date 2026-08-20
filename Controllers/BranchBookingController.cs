using HotelManagementSystem.Models;
using HotelManagementSystem.Models.BookingSearchModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

//validate anti forgery token

namespace HotelManagementSystem.Controllers
{
    public class BranchBookingController : Controller
    {
        private readonly HotelManagementContext _context;

        public BranchBookingController(HotelManagementContext context)
        {
            _context = context;
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
                query = query.Where(x => x.BookingNumber!.Contains(keyword) || x.BookerName!.Contains(keyword) || x.ContactPhone!.Contains(keyword));
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

        

    }
}