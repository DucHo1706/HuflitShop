using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    public class StaffRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string StaffId { get; set; }

        [ForeignKey("StaffId")]
        public AppUser Staff { get; set; }

        public int WorkScheduleId { get; set; }

        [ForeignKey("WorkScheduleId")]
        public WorkSchedule WorkSchedule { get; set; }

        [Required]
        [MaxLength(50)]
        public string RequestType { get; set; } // "Leave", "ForgotCheckIn"

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"

        [MaxLength(500)]
        public string AdminNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
