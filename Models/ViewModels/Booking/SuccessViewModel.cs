namespace HotelManagementSystem.Models.ViewModels.Booking;

public class SuccessViewModel
{
    public string BookingNumber { get; set; } = "";

    public string BranchName { get; set; } = "";

    public string RoomTypeName { get; set; } = "";

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    //public string Email { get; set; } = "";
}