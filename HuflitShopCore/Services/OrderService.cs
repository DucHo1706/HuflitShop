using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using HuflitShopCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class OrderService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public OrderService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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
                .Include(o => o.OrderDetails).ThenInclude(od => od.ProductVariant).ThenInclude(pv => pv.Product).ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return null;

            var dto = MapToDTO(order);
            dto.PromoCode = order.Promotion?.PromoCode;
            
            var cloudName = _configuration["Cloudinary:CloudName"] ?? _configuration["CloudinarySettings:CloudName"] ?? "dsamboqwp";

            dto.OrderDetails = order.OrderDetails.Select(od => {
                var firstImg = od.ProductVariant?.Product?.ProductImages?.OrderBy(img => img.ImageOrder).FirstOrDefault();
                string? imgUrl = "/Client/img/default-product.jpg";
                if (firstImg != null)
                {
                    imgUrl = string.IsNullOrEmpty(firstImg.AssetVersion)
                        ? firstImg.PublicId
                        : "https://res.cloudinary.com/" + cloudName + "/image/upload/v" + firstImg.AssetVersion + "/" + firstImg.PublicId + ".jpg";
                }

                return new OrderDetailDTO
                {
                    Id = od.Id,
                    OrderId = od.OrderId,
                    ProductVariantId = od.ProductVariantId,
                    Quantity = od.Quantity,
                    PurchasedPrice = od.PurchasedPrice,
                    ProductNameSnapshot = od.ProductNameSnapshot,
                    SizeNameSnapshot = od.SizeNameSnapshot,
                    ColorNameSnapshot = od.ColorNameSnapshot,
                    ProductImageUrl = imgUrl,
                    ProductId = od.ProductVariant?.ProductId
                };
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
                TotalAmount = o.TotalAmount,
                DiscountAmount = o.DiscountAmount,
                ShippingFee = o.ShippingFee,
                FinalAmount = o.FinalAmount,
                ShippingFullName = o.ShippingFullName,
                ShippingPhoneNumber = o.ShippingPhoneNumber ?? string.Empty,
                ShippingAddress = o.ShippingAddress ?? string.Empty,
                ShippingCity = o.ShippingCity ?? string.Empty,
                ShippingDistrict = o.ShippingDistrict ?? string.Empty
            };
        }

        public async Task EnsurePaymentMethodsSeededAsync()
        {
            if (!await _context.PaymentMethods.AnyAsync(pm => pm.Id == "pm-cod"))
            {
                _context.PaymentMethods.Add(new PaymentMethod { Id = "pm-cod", MethodName = "Tiền mặt (COD)" });
            }
            if (!await _context.PaymentMethods.AnyAsync(pm => pm.Id == "pm-vnpay"))
            {
                _context.PaymentMethods.Add(new PaymentMethod { Id = "pm-vnpay", MethodName = "Thanh toán online (VNPAY)" });
            }
            await _context.SaveChangesAsync();
        }

        public async Task<List<Cart>> GetCheckoutItemsAsync(string userId, string? buyNowVariantId, int buyNowQty)
        {
            if (!string.IsNullOrEmpty(buyNowVariantId))
            {
                var variant = await _context.ProductVariants
                    .Include(pv => pv.Product)
                    .Include(pv => pv.Size)
                    .Include(pv => pv.Color)
                    .FirstOrDefaultAsync(pv => pv.Id == buyNowVariantId && pv.IsActive && pv.StockQuantity > 0);

                if (variant == null) return new List<Cart>();

                return new List<Cart>
                {
                    new Cart
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProductVariantId = buyNowVariantId,
                        ProductVariant = variant,
                        Quantity = buyNowQty,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    }
                };
            }
            else
            {
                return await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Include(c => c.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Include(c => c.ProductVariant)
                        .ThenInclude(pv => pv.Size)
                    .Include(c => c.ProductVariant)
                        .ThenInclude(pv => pv.Color)
                    .ToListAsync();
            }
        }

        public async Task<List<PaymentMethod>> GetPaymentMethodsAsync()
        {
            return await _context.PaymentMethods.AsNoTracking().ToListAsync();
        }

        public async Task<Order> CreateOrderAsync(string userId, Address address, string paymentMethodId, string? appliedPromoCode, List<Cart> items, string? shippingFullName, string? shippingPhoneNumber, string? buyNowVariantId)
        {
            var paymentMethod = await _context.PaymentMethods.FindAsync(paymentMethodId);
            if (paymentMethod == null) throw new InvalidOperationException("Phương thức thanh toán không hợp lệ.");

            var orderTotal = items.Sum(c => (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity);
            decimal discountAmount = 0;
            string? promotionId = null;

            if (!string.IsNullOrWhiteSpace(appliedPromoCode))
            {
                var promo = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.PromoCode == appliedPromoCode && p.IsActive);

                if (promo != null && promo.StartDate <= DateTime.Now && promo.EndDate >= DateTime.Now && promo.MinOrderAmount <= orderTotal)
                {
                    if (!promo.UsageLimit.HasValue || promo.UsedCount < promo.UsageLimit.Value)
                    {
                        if (string.Equals(promo.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase) || 
                            string.Equals(promo.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
                        {
                            discountAmount = orderTotal * (promo.DiscountValue / 100);
                            if (promo.MaxDiscountAmount.HasValue && discountAmount > promo.MaxDiscountAmount.Value)
                            {
                                discountAmount = promo.MaxDiscountAmount.Value;
                            }
                        }
                        else
                        {
                            discountAmount = promo.DiscountValue;
                        }

                        if (discountAmount > orderTotal)
                        {
                            discountAmount = orderTotal;
                        }

                        promotionId = promo.Id;
                        promo.UsedCount += 1;
                        _context.Promotions.Update(promo);
                    }
                }
            }

            var finalAmount = orderTotal - discountAmount;

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                PaymentMethodId = paymentMethod.Id,
                PromotionId = promotionId,
                OrderDate = DateTime.UtcNow,
                OrderStatus = 0,
                PaymentStatus = 0, // 0: Chưa thanh toán
                TotalAmount = orderTotal,
                DiscountAmount = discountAmount,
                ShippingFee = 0,
                FinalAmount = finalAmount,
                ShippingFullName = !string.IsNullOrWhiteSpace(shippingFullName) ? shippingFullName : "",
                ShippingPhoneNumber = !string.IsNullOrWhiteSpace(shippingPhoneNumber) ? shippingPhoneNumber : "",
                ShippingAddress = address.SpecificAddress,
                ShippingCity = address.City,
                ShippingDistrict = address.District,
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var c in items)
            {
                var pv = c.ProductVariant;
                var product = pv?.Product;

                var detail = new OrderDetail
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    ProductVariantId = c.ProductVariantId,
                    Quantity = c.Quantity,
                    PurchasedPrice = product?.CurrentPrice ?? 0,
                    ProductNameSnapshot = product?.ProductName ?? "",
                    SizeNameSnapshot = pv?.Size?.SizeName ?? "",
                    ColorNameSnapshot = pv?.Color?.ColorName ?? ""
                };

                _context.OrderDetails.Add(detail);
            }

            // Clear cart after creating order if it's not a buy-now checkout
            if (string.IsNullOrEmpty(buyNowVariantId))
            {
                _context.Carts.RemoveRange(items);
            }

            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetOrderForSuccessAsync(string id)
        {
            return await _context.Orders
                .Include(o => o.PaymentMethod)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task UpdateOrderAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task<List<OrderDTO>> GetOrdersByUserIdAsync(string userId)
        {
            var orders = await _context.Orders
                .Include(o => o.PaymentMethod)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();

            return orders.Select(o => MapToDTO(o)).ToList();
        }

        public async Task<bool> CancelOrderByCustomerAsync(string orderId, string userId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null || order.OrderStatus != 0) return false;

            order.OrderStatus = 4; // 4: Hủy
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RequestOrderRefundAsync(string orderId, string userId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null || order.OrderStatus != 3) return false;

            order.OrderStatus = 5; // 5: Yêu cầu hoàn tiền
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}