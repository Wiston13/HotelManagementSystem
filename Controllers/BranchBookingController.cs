using Microsoft.AspNetCore.Mvc;

//到時候改成從資料庫抓
public class BookingData
{
    public string? BookingNum { get; set; }
    public DateTime BookingDate { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Roomtype { get; set; }
    public string? BookingStatus { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
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
            BookingDate = new DateTime(2026, 7, 25, 14, 30, 0),
            Name = "王小明",
            Phone = "0912-345-678",
            Roomtype = "標準雙人房",
            BookingStatus = "已退房",
            StartDate = new DateTime(2026, 8, 8),
            EndDate = new DateTime(2026, 8, 10),
            Price = "NT$ 4,800"
        },
        new BookingData
        {
            BookingNum = "BK20260808002",
            BookingDate = new DateTime(2026, 7, 26, 10, 15, 0),
            Name = "林雅雯",
            Phone = "0922-123-456",
            Roomtype = "精緻雙床房",
            BookingStatus = "已退房",
            StartDate = new DateTime(2026, 8, 8),
            EndDate = new DateTime(2026, 8, 9),
            Price = "NT$ 5,600"
        },
        new BookingData
        {
            BookingNum = "BK20260809003",
            BookingDate = new DateTime(2026, 7, 27, 16, 40, 0),
            Name = "張家豪",
            Phone = "0933-555-888",
            Roomtype = "豪華雙人房",
            BookingStatus = "已入住",
            StartDate = new DateTime(2026, 8, 9),
            EndDate = new DateTime(2026, 8, 11),
            Price = "NT$ 6,800"
        },
        new BookingData
        {
            BookingNum = "BK20260809004",
            BookingDate = new DateTime(2026, 7, 28, 11, 00, 0),
            Name = "陳美玲",
            Phone = "0944-777-999",
            Roomtype = "標準單人房",
            BookingStatus = "已入住",
            StartDate = new DateTime(2026, 8, 9),
            EndDate = new DateTime(2026, 8, 10),
            Price = "NT$ 3,200"
        },
        new BookingData
        {
            BookingNum = "BK20260810018",
            BookingDate = new DateTime(2026, 7, 29, 19, 10, 0),
            Name = "李志豪",
            Phone = "0955-111-222",
            Roomtype = "標準雙人房",
            BookingStatus = "已成立",
            StartDate = new DateTime(2026, 8, 10),
            EndDate = new DateTime(2026, 8, 12),
            Price = "NT$ 4,800"
        },
        new BookingData
        {
            BookingNum = "BK20260810019",
            BookingDate = new DateTime(2026, 7, 29, 20, 30, 0),
            Name = "黃淑芬",
            Phone = "0966-333-444",
            Roomtype = "家庭四人房",
            BookingStatus = "已成立",
            StartDate = new DateTime(2026, 8, 10),
            EndDate = new DateTime(2026, 8, 13),
            Price = "NT$ 8,800"
        },
        new BookingData
        {
            BookingNum = "BK20260810020",
            BookingDate = new DateTime(2026, 7, 30, 09, 20, 0),
            Name = "趙建國",
            Phone = "0977-888-777",
            Roomtype = "精緻雙床房",
            BookingStatus = "已取消",
            StartDate = new DateTime(2026, 8, 10),
            EndDate = new DateTime(2026, 8, 11),
            Price = "NT$ 5,600"
        },
        new BookingData
        {
            BookingNum = "BK20260811021",
            BookingDate = new DateTime(2026, 7, 30, 14, 00, 0),
            Name = "周怡君",
            Phone = "0988-222-333",
            Roomtype = "標準雙人房",
            BookingStatus = "已成立",
            StartDate = new DateTime(2026, 8, 11),
            EndDate = new DateTime(2026, 8, 13),
            Price = "NT$ 4,800"
        },
        new BookingData
        {
            BookingNum = "BK20260811022",
            BookingDate = new DateTime(2026, 7, 31, 15, 45, 0),
            Name = "吳冠宇",
            Phone = "0999-666-555",
            Roomtype = "豪華雙人房",
            BookingStatus = "逾期未入住",
            StartDate = new DateTime(2026, 8, 11),
            EndDate = new DateTime(2026, 8, 12),
            Price = "NT$ 6,800"
        },
        new BookingData
        {
            BookingNum = "BK20260812023",
            BookingDate = new DateTime(2026, 8, 1, 11, 10, 0),
            Name = "蔡依婷",
            Phone = "0911-223-344",
            Roomtype = "標準單人房",
            BookingStatus = "已成立",
            StartDate = new DateTime(2026, 8, 12),
            EndDate = new DateTime(2026, 8, 14),
            Price = "NT$ 6,400"
        },
        new BookingData
        {
            BookingNum = "BK20260812024",
            BookingDate = new DateTime(2026, 8, 1, 13, 20, 0),
            Name = "楊宗翰",
            Phone = "0922-334-455",
            Roomtype = "標準雙人房",
            BookingStatus = "已成立",
            StartDate = new DateTime(2026, 8, 12),
            EndDate = new DateTime(2026, 8, 15),
            Price = "NT$ 7,200"
        },
        new BookingData
        {
            BookingNum = "BK20260813025",
            BookingDate = new DateTime(2026, 8, 2, 10, 05, 0),
            Name = "許雅婷",
            Phone = "0933-445-566",
            Roomtype = "家庭四人房",
            BookingStatus = "已成立",
            StartDate = new DateTime(2026, 8, 13),
            EndDate = new DateTime(2026, 8, 16),
            Price = "NT$ 11,200"
        },
        new BookingData
        {
            BookingNum = "BK20260813026",
            BookingDate = new DateTime(2026, 8, 2, 17, 50, 0),
            Name = "鄭文傑",
            Phone = "0944-556-677",
            Roomtype = "精緻雙床房",
            BookingStatus = "已取消",
            StartDate = new DateTime(2026, 8, 13),
            EndDate = new DateTime(2026, 8, 14),
            Price = "NT$ 5,600"
        },
        new BookingData
        {
            BookingNum = "BK20260814027",
            BookingDate = new DateTime(2026, 8, 3, 12, 30, 0),
            Name = "謝佩如",
            Phone = "0955-667-788",
            Roomtype = "標準雙人房",
            BookingStatus = "已成立",
            StartDate = new DateTime(2026, 8, 14),
            EndDate = new DateTime(2026, 8, 16),
            Price = "NT$ 4,800"
        },
        new BookingData
        {
            BookingNum = "BK20260815028",
            BookingDate = new DateTime(2026, 8, 4, 15, 00, 0),
            Name = "郭家宏",
            Phone = "0966-778-899",
            Roomtype = "豪華雙人房",
            BookingStatus = "已成立",
            StartDate = new DateTime(2026, 8, 15),
            EndDate = new DateTime(2026, 8, 18),
            Price = "NT$ 15,800"
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
        
    }
}