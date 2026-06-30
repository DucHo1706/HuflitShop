using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WorkScheduleId { get; set; }

        [ForeignKey("WorkScheduleId")]
        public virtual WorkSchedule WorkSchedule { get; set; }

        public DateTime? CheckInTime { get; set; }
        
        [StringLength(50)]
        public string? CheckInIpAddress { get; set; }

        public DateTime? CheckOutTime { get; set; }
        
        [StringLength(50)]
        public string? CheckOutIpAddress { get; set; }

        public double CalculatedHours { get; set; } = 0; // Giờ công thực tế sau khi tính phạt

        [StringLength(20)]
        public string Status { get; set; } = "Absent"; // Present, Late, Absent, ForgotCheckout

        public string? Note { get; set; }
    }
}
