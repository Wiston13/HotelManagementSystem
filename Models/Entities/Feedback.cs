using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagementSystem.Models.Entities
{
    public class Feedback
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int BranchId { get; set; }

        public string CustomerName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string? Phone { get; set; }

        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

     
        public string Status { get; set; } = "未處理";

        public string? AdminReply { get; set; }

        public virtual Branch Branch { get; set; } = null!;
    }
}
