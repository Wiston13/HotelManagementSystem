using System;
using System.Collections.Generic;

namespace HotelManagementSystem.Models.Entities;

public partial class Branch
{
    public int BranchId { get; set; }

    public string BranchName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Description { get; set; }

    public bool AcceptsNewBookings { get; set; }

    public string? Region { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<OperationLog> OperationLogs { get; set; } = new List<OperationLog>();

    public virtual ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
}
