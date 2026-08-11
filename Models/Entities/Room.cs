using System;
using System.Collections.Generic;

namespace HotelManagementSystem.Models.Entities;

public partial class Room
{
    public int RoomId { get; set; }

    public int BranchId { get; set; }

    public int RoomTypeId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public short Floor { get; set; }

    public string SupplyStatus { get; set; } = null!;

    public string CleaningStatus { get; set; } = null!;

    public string? DisabledReason { get; set; }

    public virtual RoomType RoomType { get; set; } = null!;

    public virtual ICollection<StayRecord> StayRecords { get; set; } = new List<StayRecord>();
}
