namespace HotelManagementSystem.Options;

public sealed class N8nOptions
{
    public const string SectionName = "N8n";

    public string WebhookUrl { get; set; } = string.Empty;

    public string HeaderName { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;
}
