using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("InventoryTransactions")]
    public class InventoryTransaction
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string ProductVariantId { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; }

        [Required]
        public int QuantityChange { get; set; }

        [StringLength(50)]
        public string ReferenceId { get; set; }

        [Required]
        public int RemainingStock { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        public string Note { get; set; }

        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant ProductVariant { get; set; }
    }
}