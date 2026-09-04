using System;
using System.Collections.Generic;

namespace HotelManagementSystem.Models.Entities;

public partial class Feedback
{
    public int Id { get; set; }

    public int BranchId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;
}