using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Models.BookingSearchModel;

public sealed class UpdateBookingEmailInputViewModel
{
    [Required(ErrorMessage = "缺少訂單編號。")]
    public string BookingNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入新的 Email。")]
    [EmailAddress(ErrorMessage = "Email 格式不正確。")]
    public string NewEmail { get; set; } = string.Empty;
}
