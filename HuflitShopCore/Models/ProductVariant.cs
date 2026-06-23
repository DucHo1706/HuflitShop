using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("ProductVariants")]
    public class ProductVariant
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string SizeId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ColorId { get; set; } = string.Empty;

        public int StockQuantity { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdditionalPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AverageCostPrice { get; set; } = 0;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
        [ForeignKey("SizeId")]
        public virtual Size? Size { get; set; }
        [ForeignKey("ColorId")]
        public virtual Color? Color { get; set; }
        public bool IsActive { get; set; } = true;
    }
}