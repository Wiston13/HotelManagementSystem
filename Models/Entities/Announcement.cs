namespace HotelManagementSystem.Models.Entities
{
    public class Announcement
    {
        public int AnnouncementId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public bool IsActive { get; set; }

        public bool ShowToGuest { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
