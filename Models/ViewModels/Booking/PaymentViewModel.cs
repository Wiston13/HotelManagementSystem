namespace HotelManagementSystem.Models.ViewModels.Booking;

public class PaymentViewModel
{
    public int BranchId { get; set; }

    public string BranchName { get; set; } = "";

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public int Nights { get; set; }

    public int RoomTypeId { get; set; }

    public string RoomTypeName { get; set; } = "";

    public decimal NightlyPrice { get; set; }

    public decimal TotalPrice { get; set; }
}