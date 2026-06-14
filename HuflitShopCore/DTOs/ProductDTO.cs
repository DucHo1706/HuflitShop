using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class ProductDTO
    {
        public string? Id { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public string CategoryId { get; set; } = string.Empty;
        public string? CategoryName { get; set; } // Hỗ trợ hiển thị tên danh mục trên View

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        public string ProductName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm không được để trống")]
        public decimal CurrentPrice { get; set; }

        public int? ManufactureYear { get; set; }
        public string? Origin { get; set; }
        public string? Trademark { get; set; }
        
        public bool IsDeleted { get; set; }
    }
}