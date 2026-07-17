using System;
using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.Models
{
    public class ShopSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; }

        public string Value { get; set; }

        public string? Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
