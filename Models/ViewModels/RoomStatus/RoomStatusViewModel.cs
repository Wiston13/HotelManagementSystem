namespace HotelManagementSystem.Models.ViewModels.RoomStatus
{
    public class RoomStatusViewModel
    {
    }
    public class RoomStatusItemViewModel
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = null!;
        public int Floor { get; set; }
        public string RoomTypeName { get; set; } = null!;
        public string SupplyStatus { get; set; } = null!;
        public string CleaningStatus { get; set; } = null!;
        public string? DisabledReason { get; set; }
        public bool IsOccupied { get; set; }
        public string? PrimaryGuestName { get; set; }
        public string? ContactPhone { get; set; }
        public string? BookingNumber { get; set; }
        public DateOnly? CheckInDate { get; set; }
        public DateTime? ActualCheckInAt { get; set; }
        public DateOnly? CheckOutDate { get; set; }
    }
}
