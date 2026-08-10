using System;
using System.Collections.Generic;

namespace HotelManagementSystem.Models.Entities;

public partial class Booking
{
    public string BookingNumber { get; set; } = null!;

    public int BranchId { get; set; }

    public int RoomTypeId { get; set; }

    public string BookerName { get; set; } = null!;

    public string ContactPhone { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public string RoomTypeNameSnapshot { get; set; } = null!;

    public byte MaxOccupancySnapshot { get; set; }

    public decimal NightlyPriceSnapshot { get; set; }

    public decimal TotalAmount { get; set; }

    public string BookingStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? CancellationCause { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelledByEmployeeNumber { get; set; }

    public virtual Employee? CancelledByEmployeeNumberNavigation { get; set; }

    public virtual RoomType RoomType { get; set; } = null!;

    public virtual StayRecord? StayRecord { get; set; }
}
