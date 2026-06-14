using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("Product")]
    public class Product
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string CategoryId { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string ProductName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentPrice { get; set; }

        public int? ManufactureYear { get; set; }

        [StringLength(255)]
        public string? Origin { get; set; }

        [StringLength(255)]
        public string? Trademark { get; set; }

        public int ViewCount { get; set; } = 0;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
        public virtual ICollection<ProductVariant>? ProductVariants { get; set; }
    }
}