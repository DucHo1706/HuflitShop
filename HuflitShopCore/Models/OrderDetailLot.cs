using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("OrderDetailLots")]
    public class OrderDetailLot
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string OrderDetailId { get; set; }

        [Required]
        [StringLength(50)]
        public string InventoryLotId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }

        [ForeignKey("OrderDetailId")]
        public virtual OrderDetail OrderDetail { get; set; }

        [ForeignKey("InventoryLotId")]
        public virtual InventoryLot InventoryLot { get; set; }
    }
}
