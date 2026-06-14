using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethodId { get; set; }

        [StringLength(50)]
        public string PromotionId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public int OrderStatus { get; set; } = 0; // 0: Chờ duyệt, 1: Đóng gói, 2: Đang giao, 3: Hoàn thành, 4: Hủy
        public int PaymentStatus { get; set; } = 0; // 0: Chưa thanh toán, 1: Đã thanh toán

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; }

        [Required]
        [StringLength(255)]
        public string ShippingFullName { get; set; }

        [Required]
        [StringLength(20)]
        public string ShippingPhoneNumber { get; set; }

        [Required]
        public string ShippingAddress { get; set; }

        [Required]
        [StringLength(255)]
        public string ShippingCity { get; set; }

        [Required]
        [StringLength(255)]
        public string ShippingDistrict { get; set; }

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }

        [ForeignKey("PaymentMethodId")]
        public virtual PaymentMethod PaymentMethod { get; set; }

        [ForeignKey("PromotionId")]
        public virtual Promotion Promotion { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}