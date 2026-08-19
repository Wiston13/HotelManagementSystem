using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "請輸入員工編號")]
        public string Username { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        public string Password { get; set; }
    }
}
