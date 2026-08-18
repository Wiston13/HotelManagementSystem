using HotelManagementSystem.Models;
using HotelManagementSystem.Models.BookingSearchModel;
using Microsoft.AspNetCore.Mvc;


namespace HotelManagementSystem.Controllers
{
    public class BranchBookingController : Controller
    {
        private readonly HotelManagementContext _context;

        public BranchBookingController(HotelManagementContext context)
        {
            _context = context;
        }
        /*if (s == "已完成") r = "bg-success-subtle text-success";
        else if (s == "入住中") r = "bg-primary-subtle text-primary";
        else if (s == "已付款") r = "bg-info-subtle text-info";
        else if (s == "已取消") r = "bg-danger-subtle text-danger";*/
        private string StatusToChenese(string input)
        {
            string output="";
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
                case "":
                    output = "";
                    break;
            }
            return output;
        }


        public IActionResult BookingSearch(string keyword,string dateRange, string bookingStatus)
        {
            List<BookingData> _bookingData = new List<BookingData>();

            foreach (var item in _context.Bookings)
            {
                _bookingData.Add(new BookingData
                {
                    BookingNum = item.BookingNumber,
                    BookingDate = item.CreatedAt,
                    Name = item.BookerName,
                    Phone = item.ContactPhone,
                    Roomtype = item.RoomTypeNameSnapshot,
                    BookingStatus = StatusToChenese(item.BookingStatus),
                    StartDate = new DateTime(item.CheckInDate.Year, item.CheckInDate.Month, item.CheckInDate.Day),
                    EndDate = new DateTime(item.CheckOutDate.Year, item.CheckOutDate.Month, item.CheckOutDate.Day),
                    Price = "NT$ " + item.NightlyPriceSnapshot.ToString("N0")
                });
            }

            var allData = _bookingData;
            var query = allData.AsQueryable();

            if(string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(bookingStatus))
            {
                return View(new List<BookingData>());
            }


            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.BookingNum!.Contains(keyword) || x.Name!.Contains(keyword) || x.Phone!.Contains(keyword));
            }

            if (!string.IsNullOrEmpty(dateRange))
            {
                var dates = dateRange.Split(" - ");
                if (dates.Length == 2)
                {
                    if (DateTime.TryParse(dates[0], out DateTime startDate) && DateTime.TryParse(dates[1], out DateTime endDate))
                    {
                        query = query.Where(x => x.StartDate >= startDate && x.StartDate <= endDate);
                    }
                }
            }

            if (!string.IsNullOrEmpty(bookingStatus))
            {
                query=query.Where(x=>x.BookingStatus == bookingStatus);
            }

            var result = query.ToList();

            ViewBag.Keyword = keyword;
            ViewBag.DateRange = dateRange;
            ViewBag.BookingStatus = bookingStatus;


            return View(result);
        }
        
    }
}