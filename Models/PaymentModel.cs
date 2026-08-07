using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Models
{
    public class PaymentModel
    {
        [Key]
        public int Id { get; set; } // 自動遞增的訂單編號 

        [Required]
        public string Name { get; set; } // 姓名

        [Required]
        public string Phone { get; set; } // 聯絡電話

        [Required]
        public string Email { get; set; } // 電子信箱

        [Required]
        public string CardNumber { get; set; } // 信用卡號

        [Required]
        public string Expiry { get; set; } // 有效期限

        [Required]
        public string Cvc { get; set; } // 安全碼

        public DateTime CreatedAt { get; set; } = DateTime.Now; // 付款時間
    }
}
