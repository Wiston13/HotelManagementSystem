using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Models.ViewModels
{
    public class FeedbackCreateViewModel
    {
        [Required(ErrorMessage = "請選擇對應分館")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "訪客姓名為必填項目")]
        [StringLength(50, ErrorMessage = "姓名長度不得超過 50 個字元")]
        public string CustomerName { get; set; } = null!;

        [Required(ErrorMessage = "電子信箱為必填項目")]
        [EmailAddress(ErrorMessage = "請輸入正確格式的電子信箱")]
        [StringLength(254, ErrorMessage = "信箱長度不得超過 254 個字元")]
        public string Email { get; set; } = null!;

        [StringLength(20, ErrorMessage = "電話長度不得超過 20 個字元")]
        [Display(Name = "電話")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "意見內容不可留白")]
        [StringLength(500, ErrorMessage = "意見內容不得超過 500 字")]
        public string Content { get; set; } = null!;
    }
}