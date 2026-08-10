using System;
using System.Collections.Generic;

namespace HotelManagementSystem.Models.Entities;

public partial class Employee
{
    public string EmployeeNumber { get; set; } = null!;

    public string EmployeeName { get; set; } = null!;

    public bool IsActive { get; set; }

    public int? BranchId { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<OperationLog> OperationLogs { get; set; } = new List<OperationLog>();

    public virtual ICollection<StayRecord> StayRecordCheckedInByEmployeeNumberNavigations { get; set; } = new List<StayRecord>();

    public virtual ICollection<StayRecord> StayRecordCheckedOutByEmployeeNumberNavigations { get; set; } = new List<StayRecord>();
}
