using HotelManagementSystem.Models.ViewModels.Home;

namespace HotelManagementSystem.Models.ViewModels.Booking;

public class RoomSelectionViewModel
{
    public int BranchId { get; set; }

    public string BranchName { get; set; } = "";

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public int Nights { get; set; }

    public int GuestCount { get; set; }

    public List<RoomTypeViewModel> RoomTypes { get; set; } = new();

    // 該分館是否有符合入住人數的房型
    public bool HasMatchingRoomType { get; set; }
    // 房型選擇頁的分館下拉選單
    public List<BranchViewModel> Branches { get; set; } = new();
    // 房型選擇頁的入住人數下拉選單
    public List<int> GuestOptions { get; set; } = new();
    // 台北時間的今天日期
    public DateOnly Today { get; set; }    
}

public class RoomTypeViewModel
{
    public int RoomTypeId { get; set; }

    public string RoomTypeName { get; set; } = "";

    public int MaxOccupancy { get; set; }

    public string BedType { get; set; } = "";

    public decimal NightlyPrice { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public int AvailableRooms { get; set; }
}
