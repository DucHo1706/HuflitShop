using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("ChatMessages")]
    public class ChatMessage
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string SessionId { get; set; }

        [Required]
        [StringLength(50)]
        public string SenderType { get; set; } // Ví dụ: "User" hoặc "Admin"

        [Required]
        public string MessageContent { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        [ForeignKey("SessionId")]
        public virtual ChatSession ChatSession { get; set; }
    }
}