using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Models.ViewModels.Booking
{
    public class BookingPaymentInputViewModel
    {
        public int BranchId { get; set; }

        public DateOnly CheckInDate { get; set; }

        public DateOnly CheckOutDate { get; set; }

        public int RoomTypeId { get; set; }

        public int GuestCount { get; set; }


        [Required]
        [StringLength(50)]
        public string BookerName { get; set; } = "";

        [Required]
        public string ContactPhone { get; set; } = "";

        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = "";


        [Required]
        public string CardNumber { get; set; } = "";

        [Required]
        public string Expiry { get; set; } = "";

        [Required]
        public string Cvc { get; set; } = "";


        public decimal ConfirmedNightlyPrice { get; set; }
    }
}
