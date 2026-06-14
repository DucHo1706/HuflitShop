using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("StockReceived")]
    public class StockReceived
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [StringLength(50)]
        public string SupplierId { get; set; }

        [Required]
        [StringLength(50)]
        public string UserId { get; set; }

        public DateTime ReceivedDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; } = 0;

        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }
        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }

        public virtual ICollection<StockReceivedDetail> StockReceivedDetails { get; set; }
    }
}