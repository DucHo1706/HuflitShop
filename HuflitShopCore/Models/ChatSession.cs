using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("ChatSessions")]
    public class ChatSession
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string UserId { get; set; }

        public DateTime SessionStartedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }

        public virtual ICollection<ChatMessage> ChatMessages { get; set; }
    }
}