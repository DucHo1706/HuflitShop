using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("InventoryLots")]
    public class InventoryLot
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string ProductVariantId { get; set; }

        [Required]
        [StringLength(50)]
        public string StockReceivedDetailId { get; set; }

        [Required]
        public int OriginalQuantity { get; set; }

        [Required]
        public int RemainingQuantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }

        public DateTime ReceivedDate { get; set; } = DateTime.Now;

        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant ProductVariant { get; set; }

        [ForeignKey("StockReceivedDetailId")]
        public virtual StockReceivedDetail StockReceivedDetail { get; set; }
    }
}
