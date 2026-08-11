using System;
using System.Collections.Generic;

namespace HotelManagementSystem.Models.Entities;

public partial class RoomType
{
    public int RoomTypeId { get; set; }

    public int BranchId { get; set; }

    public string RoomTypeName { get; set; } = null!;

    public byte MaxOccupancy { get; set; }

    public string BedType { get; set; } = null!;

    public decimal NightlyPrice { get; set; }

    public bool IsActive { get; set; }

    public string? Description { get; set; }

    public string ImageUrl { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Branch Branch { get; set; } = null!;

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
