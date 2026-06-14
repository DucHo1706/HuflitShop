using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("Colors")]
    public class Color
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = System.Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string ColorName { get; set; } = string.Empty;

        [StringLength(10)]
        public string? HexCode { get; set; }
    }
}