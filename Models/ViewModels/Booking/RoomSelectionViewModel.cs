using HotelManagementSystem.Models.ViewModels.Home;

namespace HotelManagementSystem.Models.ViewModels.Booking;

public class RoomSelectionViewModel
{
    public int BranchId { get; set; }

    public string BranchName { get; set; } = "";

    public DateTime CheckInDate { get; set; }

    public DateTime CheckOutDate { get; set; }

    public int GuestCount { get; set; }

    public List<RoomTypeViewModel> RoomTypes { get; set; } = new();
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
