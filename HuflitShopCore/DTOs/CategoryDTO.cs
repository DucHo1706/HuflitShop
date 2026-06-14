using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class CategoryDTO
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        public string CategoryName { get; set; } = string.Empty;

        public string? ParentId { get; set; }
        public string? ParentName { get; set; } // Tiện ích để hiển thị tên danh mục cha trên View
    }
}