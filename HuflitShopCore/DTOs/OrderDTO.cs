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
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PackingStartedAt { get; set; }
        public DateTime? ShippingStartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal FinalAmount { get; set; }
        
        public string ShippingFullName { get; set; } = string.Empty;
        public string ShippingPhoneNumber { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingDistrict { get; set; } = string.Empty;
        public string ShippingWard { get; set; } = string.Empty;
        public string ShippingProvider { get; set; } = string.Empty;
        public string ShippingServiceName { get; set; } = string.Empty;
        public string? CarrierOrderCode { get; set; }
        public string ShippingStatus { get; set; } = string.Empty;
        public decimal? ActualShippingFee { get; set; }
        public DateTime? ExpectedDeliveryTime { get; set; }
        
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

        public string CustomerShippingStatusName => OrderStatus switch
        {
            0 => "Shop đang xác nhận đơn hàng",
            1 => "Chờ đơn vị vận chuyển lấy hàng",
            2 => "Đơn vị vận chuyển đang giao hàng",
            3 => "Đã giao hàng thành công",
            4 => "Đơn hàng đã hủy",
            5 => "Đang xử lý yêu cầu hoàn tiền",
            _ => ShippingStatusName
        };

        public string ShippingStatusName => ShippingStatus switch
        {
            "quoted" => "Đã tính phí, chưa tạo vận đơn",
            "ready_to_pick" => "Chờ GHN lấy hàng",
            "picking" => "GHN đang lấy hàng",
            "picked" => "GHN đã lấy hàng",
            "storing" => "Đang lưu kho",
            "transporting" => "Đang trung chuyển",
            "sorting" => "Đang phân loại",
            "delivering" or "money_collect_delivering" => "Đang giao hàng",
            "delivery_fail" => "Giao hàng chưa thành công",
            "waiting_to_return" => "Chờ hoàn hàng",
            "return" or "return_transporting" or "return_sorting" or "returning" => "Đang hoàn hàng",
            "returned" => "Đã hoàn hàng",
            "delivered" => "Đã giao thành công",
            "cancel" => "Đã hủy vận đơn",
            _ => string.IsNullOrWhiteSpace(ShippingStatus) ? "Chưa có trạng thái GHN" : ShippingStatus
        };
    }
}
