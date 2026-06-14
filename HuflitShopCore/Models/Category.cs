using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = System.Guid.NewGuid().ToString();

        [Required]
        [StringLength(255)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ParentId { get; set; }
    }
}