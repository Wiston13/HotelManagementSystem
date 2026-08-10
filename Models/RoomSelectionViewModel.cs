namespace HotelManagementSystem.Models
{
    // 第二頁整頁的資料
    public class RoomSelectionViewModel
    {
        public string BranchName { get; set; } = "台北館";
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int Guests { get; set; }

        // 自動計算住幾晚
        public int Nights => (CheckOutDate - CheckInDate).Days > 0 ? (CheckOutDate - CheckInDate).Days : 1;

        // 可選房型清單
        public List<RoomItemDto> RoomList { get; set; } = new List<RoomItemDto>();
    }

    // 代表單一房型的資料
    public class RoomItemDto
    {
        public int RoomTypeId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}