using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("ReviewImages")]
    public class ReviewImage
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string ReviewId { get; set; }

        [Required]
        [StringLength(255)]
        public string PublicId { get; set; }

        [StringLength(50)]
        public string AssetVersion { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ReviewId")]
        public virtual Reviews Review { get; set; }
    }
}