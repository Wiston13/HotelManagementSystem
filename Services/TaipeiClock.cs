namespace HotelManagementSystem.Services
{
    public class TaipeiClock
    {
        private static readonly TimeZoneInfo TaipeiTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

        public DateTime Now =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTimeZone);

        public DateOnly Today =>
            DateOnly.FromDateTime(Now);
    }
}