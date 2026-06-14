using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class SizeDTO
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập kích thước")]
        [StringLength(50)]
        public string SizeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại kích thước")]
        [StringLength(50)]
        public string SizeType { get; set; } = "Clothes";
    }
}