using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("OrderDetails")]
    public class OrderDetail
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string OrderId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductVariantId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasedPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; } = 0;

        /// <summary>
        /// Phần giảm giá (từ mã khuyến mãi) được phân bổ cho sản phẩm này theo tỉ lệ giá trị.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAllocation { get; set; } = 0;

        [Required]
        [StringLength(255)]
        public string ProductNameSnapshot { get; set; }

        [Required]
        [StringLength(50)]
        public string SizeNameSnapshot { get; set; }

        [Required]
        [StringLength(50)]
        public string ColorNameSnapshot { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
        
        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant ProductVariant { get; set; }

        public virtual ICollection<OrderDetailLot> OrderDetailLots { get; set; }
    }
}