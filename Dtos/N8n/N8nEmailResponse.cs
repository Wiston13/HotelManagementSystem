using System.Text.Json.Serialization;

namespace HotelManagementSystem.Dtos.N8n;

public sealed class N8nEmailResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("bookingNumber")]
    public string BookingNumber { get; set; } = string.Empty;

    [JsonPropertyName("emailAccepted")]
    public bool EmailAccepted { get; set; }

    [JsonPropertyName("n8nCompletedAtUtc")]
    public DateTimeOffset N8nCompletedAtUtc { get; set; }
}
