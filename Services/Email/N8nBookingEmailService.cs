using HotelManagementSystem.Dtos.N8n;
using HotelManagementSystem.Models.Entities;
using HotelManagementSystem.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace HotelManagementSystem.Services.Email;

public sealed class N8nBookingEmailService : IBookingEmailService
{
    private readonly HttpClient _httpClient;
    private readonly N8nOptions _options;
    private readonly ILogger<N8nBookingEmailService> _logger;

    public N8nBookingEmailService(
        HttpClient httpClient,
        IOptions<N8nOptions> options,
        ILogger<N8nBookingEmailService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendConfirmationAsync(
        Booking booking,
        Branch branch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(branch);

        var request = new N8nBookingEmailRequest
        {
            BookingNumber = booking.BookingNumber,
            BookerName = booking.BookerName,
            Email = booking.Email,            
            RoomTypeName = booking.RoomTypeNameSnapshot,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            TotalAmount = booking.TotalAmount,
            BranchName = branch.BranchName,
            BranchAddress = branch.Address,
            BranchPhone = branch.Phone
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            _options.WebhookUrl)
        {
            Content = JsonContent.Create(request)
        };

        httpRequest.Headers.Add(
            _options.HeaderName,
            _options.WebhookSecret);

        try
        {
            using var response = await _httpClient.SendAsync(
                httpRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "n8n 寄信請求失敗。BookingNumber: {BookingNumber}, StatusCode: {StatusCode}",
                    booking.BookingNumber,
                    response.StatusCode);

                return false;
            }

            var result =
                await response.Content.ReadFromJsonAsync<N8nEmailResponse>(
                    cancellationToken);

            var isValidResult =
                result is not null
                && result.Success
                && result.EmailAccepted
                && string.Equals(
                    result.BookingNumber,
                    booking.BookingNumber,
                    StringComparison.Ordinal);

            if (!isValidResult)
            {
                _logger.LogWarning(
                    "n8n 回應內容不符合預期。BookingNumber: {BookingNumber}",
                    booking.BookingNumber);

                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "呼叫 n8n 寄信服務逾時。BookingNumber: {BookingNumber}",
                booking.BookingNumber);

            return false;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "呼叫 n8n 寄信服務時發生網路錯誤。BookingNumber: {BookingNumber}",
                booking.BookingNumber);

            return false;
        }
        catch (JsonException exception)
        {
            _logger.LogError(
                exception,
                "無法解析 n8n 回應。BookingNumber: {BookingNumber}",
                booking.BookingNumber);

            return false;
        }
    }
}
