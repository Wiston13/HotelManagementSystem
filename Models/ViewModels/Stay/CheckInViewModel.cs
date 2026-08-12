namespace HotelManagementSystem.Models.ViewModels.Stay
{
    public class CheckInViewModel
    {
        public string? BookingNumber { get; set; }
        public string? BookerName { get; set; }
        public string? ContactPhone { get; set; }
        public string? RoomTypeName { get; set; }
        public DateOnly? CheckInDate { get; set; }
        public DateOnly? CheckOutDate { get; set; }
        public string? BookingStatus { get; set; }
        public bool HasResult { get; set; }
        public string? ErrorMessage { get; set; }

    }
}
