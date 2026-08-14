using HotelManagementSystem.Models.Entities;

namespace HotelManagementSystem.Services
{
    public class NoShowService
    {
        public readonly TaipeiClock _Clock;
        public NoShowService(TaipeiClock clock)
        {
            _Clock = clock;
        }

        private static readonly TimeOnly NoShowCutoffTime = new(12, 0);


        public bool IsNoShow(Booking booking)
        {
            if (booking.BookingStatus != "Paid")
            {
                return false;
            }

            if (booking.StayRecord != null)
            {
                return false;
            }

            if (_Clock.Now >= booking.CheckOutDate.ToDateTime(NoShowCutoffTime))
            {
                return true;
            }

            return false;
        }


    }
}