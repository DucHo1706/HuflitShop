using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HuflitShopCore.DTOs
{
    public class StockReceiptDTO
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp")]
        public string SupplierId { get; set; } = string.Empty;
        public string? SupplierName { get; set; } // Hiển thị

        public string? UserId { get; set; } // Người nhập kho
        public string? UserName { get; set; } // Hiển thị

        public DateTime ReceiptDate { get; set; } = DateTime.Now;
        public string? Note { get; set; }
        public decimal TotalAmount { get; set; } = 0;

        public List<StockReceiptDetailDTO> Details { get; set; } = new List<StockReceiptDetailDTO>();
    }
}