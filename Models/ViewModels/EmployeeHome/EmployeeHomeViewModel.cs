namespace HotelManagementSystem.Models.ViewModels.EmployeeHome
{
    public class EmployeeHomeViewModel
    {
        public List<PendingCheckInItemViewModel> PendingCheckIns { get; set; } = new();

        public List<StayItemViewModel> ActiveStays { get; set; } = new();

        public List<StayItemViewModel> PendingCheckOuts { get; set; } = new();

        public List<RoomCleaningItemViewModel> RoomsToClean { get; set; } = new();
    }

    public class PendingCheckInItemViewModel
    {
        public string BookingNumber { get; set; } = null!;

        public string BookerName { get; set; } = null!;

        public string ContactPhone { get; set; } = null!;

        public string RoomTypeName { get; set; } = null!;

        public DateOnly CheckInDate { get; set; }
    }

    public class StayItemViewModel
    {
        public string BookingNumber { get; set; } = null!;

        public string PrimaryGuestName { get; set; } = null!;

        public string RoomNumber { get; set; } = null!;

        public DateTime ActualCheckInAt { get; set; }

        public DateOnly CheckOutDate { get; set; }
    }

    public class RoomCleaningItemViewModel
    {
        public int RoomId { get; set; }

        public string RoomNumber { get; set; } = null!;

        public string RoomTypeName { get; set; } = null!;

        public short Floor { get; set; }
    }
}