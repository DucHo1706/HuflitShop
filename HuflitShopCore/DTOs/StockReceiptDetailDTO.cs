using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class StockReceiptDetailDTO
    {
        public string Id { get; set; } = string.Empty;
        public string StockReceiptId { get; set; } = string.Empty;

        public string ProductVariantId { get; set; } = string.Empty;
        public string? ProductName { get; set; } // Hiển thị tên (VD: Áo Thun - Đỏ - Size L)

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice => Quantity * UnitPrice;
    }
}