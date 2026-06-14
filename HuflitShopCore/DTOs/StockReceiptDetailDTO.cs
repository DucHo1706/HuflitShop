using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class StockReceiptDetailDTO
    {
        public string Id { get; set; } = string.Empty;
        public string StockReceiptId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn biến thể sản phẩm")]
        public string ProductVariantId { get; set; } = string.Empty;
        public string? ProductName { get; set; } // Hiển thị tên (VD: Áo Thun - Đỏ - Size L)

        [Required(ErrorMessage = "Vui lòng nhập số lượng")]
        [Range(1, 100000, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá nhập")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá nhập không hợp lệ")]
        public decimal UnitPrice { get; set; }

        public decimal TotalPrice => Quantity * UnitPrice;
    }
}