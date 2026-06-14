using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class ColorDTO
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên màu sắc")]
        [StringLength(50)]
        public string ColorName { get; set; } = string.Empty;

        [StringLength(10)]
        public string? HexCode { get; set; }
    }
}