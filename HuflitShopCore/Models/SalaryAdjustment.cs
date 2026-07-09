using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("SalaryAdjustments")]
    public class SalaryAdjustment
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string StaffId { get; set; }

        [Required]
        [StringLength(20)]
        public string Type { get; set; } // "Allowance" hoặc "Deduction"

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("StaffId")]
        public virtual AppUser? Staff { get; set; }
    }
}
