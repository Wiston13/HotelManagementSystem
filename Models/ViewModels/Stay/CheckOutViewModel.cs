namespace HotelManagementSystem.Models.ViewModels.Stay
{
    public class CheckOutViewModel
    {
        public string? SearchValue { get; set; }
        public string? BookingNumber { get; set; }
        public string? BookerName { get; set; }
        public DateTime? CheckInAt { get; set; }
        public string? RoomTypeName { get; set; }
        public string? BookingStatus { get; set; }
        public string? RoomNumber { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public bool HasResult { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
