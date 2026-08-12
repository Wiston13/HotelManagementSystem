using Microsoft.AspNetCore.Mvc;

//到時候改成從資料庫抓
public class BookingData
{
    public string? BookingNum { get; set; }
    public DateTime BookingDate { get; set; }
    public string? Name { get; set; }
    public string? Roomtype { get; set; }
    public string? BookingStatus { get; set; }
}

public class BookingDetail
{
    public string? BookingNum { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime ConfirmedDate { get; set; }
    public string? Price { get; set; }

}

namespace HotelManagementSystem.Controllers
{
    
   
    public class BranchBookingController : Controller
    {
        private List<BookingData> GetMockData()
        {
            return new List<BookingData>()
            {
                new BookingData
        {
            BookingNum = "BK20260808001",
            BookingDate = new DateTime(2026, 8, 8),
            Name = "王小明",
            Roomtype = "標準雙人房",
            BookingStatus = "已退房"
        },
        new BookingData
        {
            BookingNum = "BK20260808002",
            BookingDate = new DateTime(2026, 8, 8),
            Name = "林雅雯",
            Roomtype = "精緻雙床房",
            BookingStatus = "已退房"
        },
        new BookingData
        {
            BookingNum = "BK20260809003",
            BookingDate = new DateTime(2026, 8, 9),
            Name = "張家豪",
            Roomtype = "豪華雙人房",
            BookingStatus = "已入住"
        },
        new BookingData
        {
            BookingNum = "BK20260809004",
            BookingDate = new DateTime(2026, 8, 9),
            Name = "陳美玲",
            Roomtype = "標準單人房",
            BookingStatus = "已入住"
        },
        new BookingData
        {
            BookingNum = "BK20260810018",
            BookingDate = new DateTime(2026, 8, 10),
            Name = "李志豪",
            Roomtype = "標準雙人房",
            BookingStatus = "已成立"
        },
        new BookingData
        {
            BookingNum = "BK20260810019",
            BookingDate = new DateTime(2026, 8, 10),
            Name = "黃淑芬",
            Roomtype = "家庭四人房",
            BookingStatus = "已成立"
        },
        new BookingData
        {
            BookingNum = "BK20260810020",
            BookingDate = new DateTime(2026, 8, 10),
            Name = "趙建國",
            Roomtype = "精緻雙床房",
            BookingStatus = "已取消"
        },
        new BookingData
        {
            BookingNum = "BK20260811021",
            BookingDate = new DateTime(2026, 8, 11),
            Name = "周怡君",
            Roomtype = "標準雙人房",
            BookingStatus = "已成立"
        },
        new BookingData
        {
            BookingNum = "BK20260811022",
            BookingDate = new DateTime(2026, 8, 11),
            Name = "吳冠宇",
            Roomtype = "豪華雙人房",
            BookingStatus = "逾期未入住"
        },
        new BookingData
        {
            BookingNum = "BK20260812023",
            BookingDate = new DateTime(2026, 8, 12),
            Name = "蔡依婷",
            Roomtype = "標準單人房",
            BookingStatus = "已成立"
        },
        new BookingData
        {
            BookingNum = "BK20260812024",
            BookingDate = new DateTime(2026, 8, 12),
            Name = "楊宗翰",
            Roomtype = "標準雙人房",
            BookingStatus = "已成立"
        },
        new BookingData
        {
            BookingNum = "BK20260813025",
            BookingDate = new DateTime(2026, 8, 13),
            Name = "許雅婷",
            Roomtype = "家庭四人房",
            BookingStatus = "已成立"
        },
        new BookingData
        {
            BookingNum = "BK20260813026",
            BookingDate = new DateTime(2026, 8, 13),
            Name = "鄭文傑",
            Roomtype = "精緻雙床房",
            BookingStatus = "已取消"
        },
        new BookingData
        {
            BookingNum = "BK20260814027",
            BookingDate = new DateTime(2026, 8, 14),
            Name = "謝佩如",
            Roomtype = "標準雙人房",
            BookingStatus = "已成立"
        },
        new BookingData
        {
            BookingNum = "BK20260815028",
            BookingDate = new DateTime(2026, 8, 15),
            Name = "郭家宏",
            Roomtype = "豪華雙人房",
            BookingStatus = "已成立"
        }
            };
        }

        public IActionResult BookingSearch(string keyword,string dateRange, string bookingStatus)
        {


            var allData = GetMockData();
            var query = allData.AsQueryable();

            if(string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(bookingStatus))
            {
                return View(new List<BookingData>());
            }


            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.BookingNum!.Contains(keyword) || x.Name!.Contains(keyword));
            }

            if (!string.IsNullOrEmpty(dateRange))
            {
                var dates = dateRange.Split(" - ");
                if (dates.Length == 2)
                {
                    if (DateTime.TryParse(dates[0], out DateTime startDate) && DateTime.TryParse(dates[1], out DateTime endDate))
                    {
                        query = query.Where(x => x.BookingDate >= startDate && x.BookingDate <= endDate);
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
        public IActionResult BookingResult()
        {
            return View();
        }
    }
}