namespace HotelManagementSystem.Helper
{
    public static class StatusDisplayHelper
    {
        private static readonly Dictionary<string, string> BookingStatusTexts = new()
        {
            ["Paid"] = "已付款",
            ["CheckedIn"] = "已入住",
            ["Completed"] = "已完成",
            ["Cancelled"] = "已取消",
            ["NoShow"] = "逾期未入住"
        };

        public static string GetBookingStatusText(string? status)
        {
            if (status == null) { return string.Empty; }

            return BookingStatusTexts.TryGetValue(status, out string? value) ? value : status;
        }
    }
}
