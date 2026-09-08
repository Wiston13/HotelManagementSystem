using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Models.BookingSearchModel;

public sealed class ResendConfirmationEmailInputViewModel
{
    [Required(ErrorMessage = "缺少訂單編號。")]
    public string BookingNumber { get; set; } = string.Empty;
}
