using HotelManagementSystem.Models.ViewModels.Announcement;

namespace HotelManagementSystem.Models.ViewModels
{
    public class AnnouncementIndexViewModel
    {
        public AnnouncementCreateViewModel Create { get; set; }
            = new AnnouncementCreateViewModel();

        public AnnouncementEditViewModel Edit { get; set; }
            = new AnnouncementEditViewModel();

        public List<HotelManagementSystem.Models.Entities.Announcement> Announcements { get; set; }
            = new List<HotelManagementSystem.Models.Entities.Announcement>();
    }
}