using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class ProductImageDTO
    {
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        public string ProductId { get; set; } = string.Empty;
        
        public string ProductName { get; set; } = string.Empty;
        
        public string ImageUrl { get; set; } = string.Empty;
        
        public string PublicId { get; set; } = string.Empty;
        
        public string? AssetVersion { get; set; }
        
        // Dùng để nhận file upload từ client
        public IFormFile? ImageFile { get; set; }
    }
}