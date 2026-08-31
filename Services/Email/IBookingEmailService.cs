using HotelManagementSystem.Models.Entities;

namespace HotelManagementSystem.Services.Email;

public interface IBookingEmailService
{
    Task<bool> SendConfirmationAsync(
        Booking booking,
        Branch branch,
        CancellationToken cancellationToken = default);
}
