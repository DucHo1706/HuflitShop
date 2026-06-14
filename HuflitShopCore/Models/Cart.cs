using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("Carts")]
    public class Cart
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [StringLength(50)]
        public string? UserId { get; set; } 

        [Required]
        [StringLength(50)]
        public string ProductVariantId { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        [StringLength(255)]
        public string? SessionId { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant? ProductVariant { get; set; }
    }
}