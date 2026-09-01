namespace HotelManagementSystem.Models.BookingSearchModel
{
    public class BookingData
    {
        public string? BookingNum { get; set; }
        public DateTime BookingDate { get; set; }
        public string? BranchName { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Roomtype { get; set; }
        public string? BookingStatus { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Price { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
