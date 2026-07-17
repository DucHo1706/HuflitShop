using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    public class ShiftRegistration
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

        public DateTime RegistrationDate { get; set; } // Ngày đăng ký làm việc

        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
