using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Models.ViewModels.Announcement
{
    public class AnnouncementEditViewModel
    {
        public int AnnouncementId { get; set; }

        [Required(ErrorMessage = "請輸入公告標題")]
        [StringLength(100, ErrorMessage = "公告標題最多 100 個字")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入公告內容")]
        [StringLength(1000, ErrorMessage = "公告內容最多 1000 個字")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇開始時間")]
        public DateTime? StartAt { get; set; }

        [Required(ErrorMessage = "請選擇結束時間")]
        public DateTime? EndAt { get; set; }

        public bool IsActive { get; set; }
    }
}