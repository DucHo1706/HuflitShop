using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class OrderService
    {
        private readonly AppDbContext _context;

        public OrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrderDTO>> GetAllOrdersAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.PaymentMethod)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(o => MapToDTO(o)).ToList();
        }

        public async Task<List<OrderDTO>> GetPendingOrdersAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.PaymentMethod)
                .Where(o => o.OrderStatus == 0) // Lọc đơn "Chờ duyệt"
                .OrderBy(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(o => MapToDTO(o)).ToList();
        }

        public async Task<OrderDTO?> GetOrderByIdAsync(string id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.PaymentMethod)
                .Include(o => o.Promotion)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return null;

            var dto = MapToDTO(order);
            dto.PromoCode = order.Promotion?.PromoCode;
            
            dto.OrderDetails = order.OrderDetails.Select(od => new OrderDetailDTO
            {
                Id = od.Id,
                OrderId = od.OrderId,
                ProductVariantId = od.ProductVariantId,
                Quantity = od.Quantity,
                PurchasedPrice = od.PurchasedPrice,
                ProductNameSnapshot = od.ProductNameSnapshot,
                SizeNameSnapshot = od.SizeNameSnapshot,
                ColorNameSnapshot = od.ColorNameSnapshot
            }).ToList();

            return dto;
        }

        public async Task<bool> UpdateOrderStatusAsync(string id, int status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return false;

            order.OrderStatus = status;
            // Nếu đơn hoàn thành (3), ta đánh dấu luôn là đã thanh toán (1)
            if (status == 3) order.PaymentStatus = 1;
            
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return true;
        }

        private OrderDTO MapToDTO(Models.Order o)
        {
            return new OrderDTO
            {
                Id = o.Id,
                UserId = o.UserId,
                CustomerName = o.User?.FullName ?? o.User?.Email,
                PaymentMethodName = o.PaymentMethod?.MethodName ?? "---",
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                FinalAmount = o.FinalAmount,
                ShippingFullName = o.ShippingFullName
            };
        }
    }
}