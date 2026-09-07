using System;

namespace HotelManagementSystem.Models.Entities;

public partial class Announcement
{
    public int AnnouncementId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public bool IsActive { get; set; } = true;

    public bool ShowToGuest { get; set; }

    public DateTime CreatedAt { get; set; }
}
