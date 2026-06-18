using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("Reviews")]
    public class Reviews
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductVariantId { get; set; }

        [Required]
        public int RatingStars { get; set; }

        public string ReviewComment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant ProductVariant { get; set; }

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }

        public virtual ICollection<ReviewImage> ReviewImages { get; set; }

        public string GetCommentText()
        {
            if (string.IsNullOrEmpty(ReviewComment)) return string.Empty;
            var parts = ReviewComment.Split("|||", StringSplitOptions.None);
            return parts[0].Trim();
        }

        public string GetAdminReply()
        {
            if (string.IsNullOrEmpty(ReviewComment)) return string.Empty;
            var parts = ReviewComment.Split("|||", StringSplitOptions.None);
            foreach (var part in parts)
            {
                if (part.StartsWith("REPLY:", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Substring(6).Trim();
                }
            }
            return string.Empty;
        }

        public int GetHelpfulVotes()
        {
            if (string.IsNullOrEmpty(ReviewComment)) return 0;
            var parts = ReviewComment.Split("|||", StringSplitOptions.None);
            foreach (var part in parts)
            {
                if (part.StartsWith("VOTES:", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(part.Substring(6).Trim(), out int votes))
                    {
                        return votes;
                    }
                }
            }
            return 0;
        }
    }
}