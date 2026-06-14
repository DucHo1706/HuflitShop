using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class ProductVariantDTO
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        public string ProductId { get; set; } = string.Empty;
        public string? ProductName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn kích thước")]
        public string SizeId { get; set; } = string.Empty;
        public string? SizeName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn màu sắc")]
        public string ColorId { get; set; } = string.Empty;
        public string? ColorName { get; set; }
        public string? ColorHexCode { get; set; }

        public int StockQuantity { get; set; }
        public decimal AdditionalPrice { get; set; }
        public bool IsActive { get; set; }

        public bool IsSelected { get; set; }
    }
}