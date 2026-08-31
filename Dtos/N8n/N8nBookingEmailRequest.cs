using System.Text.Json.Serialization;

namespace HotelManagementSystem.Dtos.N8n;

public sealed class N8nBookingEmailRequest
{
    [JsonPropertyName("bookingNumber")]
    public string BookingNumber { get; set; } = string.Empty;

    [JsonPropertyName("bookerName")]
    public string BookerName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("roomTypeName")]
    public string RoomTypeName { get; set; } = string.Empty;

    [JsonPropertyName("checkInDate")]
    public DateOnly CheckInDate { get; set; }

    [JsonPropertyName("checkOutDate")]
    public DateOnly CheckOutDate { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("branchName")]
    public string BranchName { get; set; } = string.Empty;

    [JsonPropertyName("branchAddress")]
    public string BranchAddress { get; set; } = string.Empty;

    [JsonPropertyName("branchPhone")]
    public string BranchPhone { get; set; } = string.Empty;
}
