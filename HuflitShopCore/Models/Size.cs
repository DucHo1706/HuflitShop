using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("Sizes")]
    public class Size
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = System.Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string SizeName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string SizeType { get; set; } = string.Empty;
    }
}