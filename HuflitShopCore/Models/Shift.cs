using System;
using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.Models
{
    public class Shift
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // Sáng, Chiều, Tối

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public int MaxStaff { get; set; } = 2; // Default 2

        public bool IsActive { get; set; } = true;
    }
}
