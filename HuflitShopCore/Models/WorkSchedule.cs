using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    public class WorkSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string StaffId { get; set; }

        [ForeignKey("StaffId")]
        public virtual AppUser Staff { get; set; }

        [Required]
        public int ShiftId { get; set; }

        [ForeignKey("ShiftId")]
        public virtual Shift Shift { get; set; }

        [Required]
        public DateTime WorkDate { get; set; } // Ngày làm việc chính thức

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
