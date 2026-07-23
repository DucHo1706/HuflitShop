using HuflitShopCore.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace HuflitShopCore.Services
{
    public sealed class ShipmentService
    {
        private readonly AppDbContext _context;
        private readonly GhnService _ghnService;

        public ShipmentService(AppDbContext context, GhnService ghnService)
        {
            _context = context;
            _ghnService = ghnService;
        }

        public async Task CreateGhnOrderAsync(string orderId, CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .Include(x => x.Shipment)
                .Include(x => x.OrderDetails)
                .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy đơn hàng.");

            var shipment = order.Shipment ?? throw new InvalidOperationException("Đơn hàng chưa có báo giá GHN.");
            if (!string.IsNullOrWhiteSpace(shipment.CarrierOrderCode)) return;
            if (order.OrderStatus != 1) throw new InvalidOperationException("Chỉ tạo vận đơn khi đơn hàng đang ở trạng thái Đóng gói.");
            if (order.PaymentMethodId == "pm-vnpay" && order.PaymentStatus != 1)
                throw new InvalidOperationException("Đơn VNPAY chưa thanh toán thành công nên chưa thể tạo vận đơn.");

            var result = await _ghnService.CreateOrderAsync(order, shipment, order.OrderDetails.ToList(), cancellationToken);
            shipment.CarrierOrderCode = result.OrderCode;
            shipment.ActualFee = result.TotalFee;
            shipment.ExpectedDeliveryTime = result.ExpectedDeliveryTime ?? shipment.ExpectedDeliveryTime;
            shipment.CodAmount = order.PaymentMethodId == "pm-cod" ? order.FinalAmount : 0;
            shipment.Status = "ready_to_pick";
            shipment.CarrierCreatedAt = DateTime.UtcNow;
            shipment.StatusUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task CancelGhnOrderAsync(string orderId, CancellationToken cancellationToken = default)
        {
            var shipment = await _context.Shipments.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
            if (shipment == null || string.IsNullOrWhiteSpace(shipment.CarrierOrderCode)) return;
            if (shipment.Status is "cancel" or "delivered" or "returned") return;

            await _ghnService.CancelOrderAsync(shipment.CarrierOrderCode, cancellationToken);
            shipment.Status = "cancel";
            shipment.CancelledAt = DateTime.UtcNow;
            shipment.StatusUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ApplyWebhookAsync(GhnWebhookRequest webhook, CancellationToken cancellationToken = default)
        {
            var shipment = await _context.Shipments
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x => x.CarrierOrderCode == webhook.OrderCode ||
                    (!string.IsNullOrEmpty(webhook.ClientOrderCode) && x.OrderId == webhook.ClientOrderCode), cancellationToken);

            if (shipment == null) return;

            if (!string.IsNullOrWhiteSpace(webhook.OrderCode)) shipment.CarrierOrderCode ??= webhook.OrderCode;
            shipment.Status = string.IsNullOrWhiteSpace(webhook.Status) ? shipment.Status : webhook.Status.Trim().ToLowerInvariant();
            shipment.FailureReason = string.IsNullOrWhiteSpace(webhook.Reason) ? shipment.FailureReason : webhook.Reason;
            shipment.ActualFee = webhook.TotalFee > 0 ? webhook.TotalFee : shipment.ActualFee;
            shipment.StatusUpdatedAt = webhook.Time?.ToUniversalTime() ?? DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task SynchronizeGhnOrderAsync(string orderId, CancellationToken cancellationToken = default)
        {
            var shipment = await _context.Shipments.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy thông tin vận chuyển.");
            if (string.IsNullOrWhiteSpace(shipment.CarrierOrderCode))
                throw new InvalidOperationException("Đơn hàng chưa có mã vận đơn GHN.");

            var status = await _ghnService.GetOrderStatusAsync(shipment.CarrierOrderCode, cancellationToken);
            await ApplyWebhookAsync(new GhnWebhookRequest
            {
                OrderCode = status.OrderCode,
                Status = status.Status,
                TotalFee = status.TotalFee,
                Time = DateTime.UtcNow
            }, cancellationToken);

            var trackedShipment = await _context.Shipments.FirstAsync(x => x.OrderId == orderId, cancellationToken);
            trackedShipment.ExpectedDeliveryTime = status.Leadtime ?? trackedShipment.ExpectedDeliveryTime;
            await _context.SaveChangesAsync(cancellationToken);
        }

    }

    public sealed class GhnWebhookRequest
    {
        [JsonPropertyName("OrderCode")]
        public string OrderCode { get; set; } = string.Empty;
        [JsonPropertyName("ClientOrderCode")]
        public string ClientOrderCode { get; set; } = string.Empty;
        [JsonPropertyName("Status")]
        public string Status { get; set; } = string.Empty;
        [JsonPropertyName("Reason")]
        public string? Reason { get; set; }
        [JsonPropertyName("TotalFee")]
        public decimal TotalFee { get; set; }
        [JsonPropertyName("Time")]
        public DateTime? Time { get; set; }
    }
}
