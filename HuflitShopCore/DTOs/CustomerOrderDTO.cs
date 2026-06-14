using System;

namespace HuflitShopCore.DTOs
{
    public class CustomerOrderDTO
    {
        public string Id { get; set; } = string.Empty;
        public DateTime? OrderDate { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        public decimal FinalAmount { get; set; }
        public int OrderStatus { get; set; }
        public string OrderStatusName => OrderStatus switch
        {
            0 => "Chờ duyệt",
            1 => "Đóng gói",
            2 => "Đang giao",
            3 => "Hoàn thành",
            4 => "Đã hủy",
            _ => "Không xác định"
        };
    }
}