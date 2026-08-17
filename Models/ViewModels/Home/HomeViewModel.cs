namespace HotelManagementSystem.Models.ViewModels.Home;

public class HomeViewModel
{
    public List<BranchViewModel> Branches { get; set; } = new();
    public List<RoomTypeViewModel> RoomTypes { get; set; } = new();
}

public class BranchViewModel
{
    public int BranchId { get; set; }

    public string BranchName { get; set; } = "";

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Description { get; set; }

    public string? Region { get; set; }

    public string? ImageUrl { get; set; }
}

public class RoomTypeViewModel
{
    public int RoomTypeId { get; set; }

    public int BranchId { get; set; }

    public string RoomTypeName { get; set; } = "";

    public int MaxOccupancy { get; set; }

    public bool IsActive { get; set; }
}