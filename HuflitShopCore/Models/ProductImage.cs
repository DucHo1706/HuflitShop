using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("ProductImages")]
    public class ProductImage
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string ProductId { get; set; }

        [StringLength(50)]
        public string? ColorId { get; set; }

        [StringLength(255)]
        public string PublicId { get; set; } = string.Empty;

        [StringLength(50)]
        public string? AssetVersion { get; set; }

        public int ImageOrder { get; set; } = 0;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("ColorId")]
        public virtual Color? Color { get; set; }
    }
}