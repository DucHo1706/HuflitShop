using System;
using System.Collections.Generic;

namespace HuflitShopCore.DTOs
{
    public class OrderDTO
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? CustomerName { get; set; } // Lấy từ User.FullName hoặc Email
        public string PaymentMethodName { get; set; } = string.Empty;
        public string? PromoCode { get; set; }
        
        public DateTime OrderDate { get; set; }
        public int OrderStatus { get; set; }
        public int PaymentStatus { get; set; }
        
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal FinalAmount { get; set; }
        
        public string ShippingFullName { get; set; } = string.Empty;
        public string ShippingPhoneNumber { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingDistrict { get; set; } = string.Empty;
        
        public List<OrderDetailDTO> OrderDetails { get; set; } = new List<OrderDetailDTO>();

        public string OrderStatusName => OrderStatus switch
        {
            0 => "Chờ duyệt",
            1 => "Đóng gói",
            2 => "Đang giao",
            3 => "Hoàn thành",
            4 => "Đã hủy",
            _ => "Không xác định"
        };

        public string PaymentStatusName => PaymentStatus == 0 ? "Chưa thanh toán" : "Đã thanh toán";
    }
}