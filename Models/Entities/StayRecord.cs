using System;
using System.Collections.Generic;

namespace HotelManagementSystem.Models.Entities;

public partial class StayRecord
{
    public int StayRecordId { get; set; }

    public string BookingNumber { get; set; } = null!;

    public int RoomId { get; set; }

    public string RoomNumberSnapshot { get; set; } = null!;

    public DateTime ActualCheckInAt { get; set; }

    public DateTime? ActualCheckOutAt { get; set; }

    public string PrimaryGuestName { get; set; } = null!;

    public byte ActualGuestCount { get; set; }

    public string CheckedInByEmployeeNumber { get; set; } = null!;

    public string? CheckedOutByEmployeeNumber { get; set; }

    public virtual Booking BookingNumberNavigation { get; set; } = null!;

    public virtual Employee CheckedInByEmployeeNumberNavigation { get; set; } = null!;

    public virtual Employee? CheckedOutByEmployeeNumberNavigation { get; set; }

    public virtual Room Room { get; set; } = null!;
}
