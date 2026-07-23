using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("Shipments")]
    public class Shipment
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string OrderId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Provider { get; set; } = "GHN";

        public int ServiceId { get; set; }
        public int ServiceTypeId { get; set; }

        [Required]
        [StringLength(100)]
        public string ServiceName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? CarrierOrderCode { get; set; }

        [StringLength(100)]
        public string Status { get; set; } = "quoted";

        [StringLength(1000)]
        public string? FailureReason { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal QuotedFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ActualFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CodAmount { get; set; }

        public int WeightGrams { get; set; }
        public int LengthCm { get; set; }
        public int WidthCm { get; set; }
        public int HeightCm { get; set; }
        public DateTime? ExpectedDeliveryTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime StatusUpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CarrierCreatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;
    }
}
