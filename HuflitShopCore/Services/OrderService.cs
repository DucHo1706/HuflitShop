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
                .Include(o => o.OrderDetails) // Tải chi tiết đơn hàng (sản phẩm) để tìm kiếm/lọc nâng cao
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(o => MapToDTO(o, true)).ToList();
        }

        public async Task<List<OrderDTO>> GetPendingOrdersAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderDetails) // Tải chi tiết đơn hàng (sản phẩm) để tìm kiếm/lọc nâng cao
                .Where(o => o.OrderStatus == 0) // Lọc đơn "Chờ duyệt"
                .OrderBy(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(o => MapToDTO(o, true)).ToList();
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
                    if (firstImg.PublicId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                        firstImg.PublicId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        imgUrl = firstImg.PublicId;
                    }
                    else
                    {
                        imgUrl = string.IsNullOrEmpty(firstImg.AssetVersion)
                            ? firstImg.PublicId
                            : "https://res.cloudinary.com/" + cloudName + "/image/upload/v" + firstImg.AssetVersion + "/" + firstImg.PublicId + ".jpg";
                    }
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

            int oldStatus = order.OrderStatus;
            if (oldStatus == status) return true;

            order.OrderStatus = status;
            // Nếu đơn hoàn thành (3), ta đánh dấu luôn là đã thanh toán (1)
            if (status == 3) order.PaymentStatus = 1;
            
            _context.Orders.Update(order);

            // Nếu trạng thái chuyển thành Hủy (4) và trước đó không phải là Hủy (4)
            if (status == 4 && oldStatus != 4)
            {
                var details = await _context.OrderDetails
                    .Include(od => od.OrderDetailLots)
                    .Where(od => od.OrderId == id).ToListAsync();
                foreach (var detail in details)
                {
                    var pv = await _context.ProductVariants.FindAsync(detail.ProductVariantId);
                    if (pv != null)
                    {
                        pv.StockQuantity += detail.Quantity;
                        _context.ProductVariants.Update(pv);

                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            Id = Guid.NewGuid().ToString(),
                            ProductVariantId = detail.ProductVariantId,
                            TransactionType = "IN",
                            QuantityChange = detail.Quantity,
                            RemainingStock = pv.StockQuantity,
                            TransactionDate = DateTime.Now,
                            ReferenceId = id,
                            Note = $"Hoàn kho do hủy đơn hàng: {id}"
                        });

                        // Hoàn lại số lượng cho các lô FIFO
                        if (detail.OrderDetailLots != null)
                        {
                            foreach (var odl in detail.OrderDetailLots)
                            {
                                var lot = await _context.InventoryLots.FindAsync(odl.InventoryLotId);
                                if (lot != null)
                                {
                                    lot.RemainingQuantity += odl.Quantity;
                                }
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private OrderDTO MapToDTO(Models.Order o, bool includeDetails = false)
        {
            var dto = new OrderDTO
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

            if (includeDetails && o.OrderDetails != null)
            {
                dto.OrderDetails = o.OrderDetails.Select(od => new OrderDetailDTO
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
            }

            return dto;
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
            if (!await _context.Promotions.AnyAsync(p => p.PromoCode == "QUENGIORHANG"))
            {
                _context.Promotions.Add(new Promotion
                {
                    Id = Guid.NewGuid().ToString(),
                    PromoCode = "QUENGIORHANG",
                    DiscountType = "Percent",
                    DiscountValue = 10,
                    MinOrderAmount = 0,
                    StartDate = DateTime.Now.AddDays(-1),
                    EndDate = DateTime.Now.AddDays(30),
                    IsActive = true
                });
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

        public async Task<(bool Success, string Message, decimal Discount)> ValidateAndCalculateVoucherAsync(string promoCode, string userId, string? buyNowVariantId, int buyNowQty = 1)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
            {
                return (false, "Vui lòng nhập mã giảm giá.", 0);
            }

            var promo = await _context.Promotions
                .FirstOrDefaultAsync(p => p.PromoCode == promoCode && p.IsActive);

            if (promo == null)
            {
                return (false, "Mã giảm giá không tồn tại hoặc đã bị khóa.", 0);
            }

            var now = DateTime.Now;
            if (promo.StartDate > now || promo.EndDate < now)
            {
                return (false, "Mã giảm giá đã hết hạn sử dụng.", 0);
            }

            if (promo.UsageLimit.HasValue && promo.UsedCount >= promo.UsageLimit.Value)
            {
                return (false, "Mã giảm giá đã hết lượt sử dụng.", 0);
            }

            if (promo.IsAutoApply)
            {
                return (false, "Mã giảm giá này được áp dụng tự động, không cần nhập thủ công.", 0);
            }

            var items = await GetCheckoutItemsAsync(userId, buyNowVariantId, buyNowQty);
            if (items == null || items.Count == 0)
            {
                return (false, "Không có sản phẩm nào để áp dụng mã giảm giá.", 0);
            }

            // Tính khuyến mãi tự động trước để lấy giá trị hàng thực tế sau giảm giá trực tiếp
            var (itemTotalAfterDirect, comboDiscount, comboDetails) = await CalculateAutoPromotionsAsync(items);
            var orderTotalForManualPromo = Math.Max(0, itemTotalAfterDirect - comboDiscount);

            if (promo.MinOrderAmount > orderTotalForManualPromo)
            {
                return (false, $"Đơn hàng tối thiểu {promo.MinOrderAmount:N0}đ để sử dụng mã này.", 0);
            }

            // Tính đơn giá sau giảm giá tự động của từng sản phẩm trong checkout
            var autoPromos = await _context.Promotions
                .Where(p => p.IsActive && p.IsAutoApply && p.StartDate <= now && p.EndDate >= now)
                .ToListAsync();

            var itemsWithFinalPrice = items.Select(c => {
                var basePrice = c.ProductVariant?.Product?.CurrentPrice ?? 0;
                decimal directDiscount = 0;
                var prodPromo = autoPromos.FirstOrDefault(p => p.ApplicableProductId == c.ProductVariant?.ProductId);
                if (prodPromo != null)
                {
                    if (string.Equals(prodPromo.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase) || 
                        string.Equals(prodPromo.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
                    {
                        directDiscount = basePrice * (prodPromo.DiscountValue / 100);
                        if (prodPromo.MaxDiscountAmount.HasValue && directDiscount > prodPromo.MaxDiscountAmount.Value)
                        {
                            directDiscount = prodPromo.MaxDiscountAmount.Value;
                        }
                    }
                    else
                    {
                        directDiscount = prodPromo.DiscountValue;
                    }
                }
                var finalPrice = Math.Max(0, basePrice - directDiscount);
                return new { Item = c, FinalPrice = finalPrice };
            }).ToList();

            // 1. Kiểm tra ApplicableProductId (áp dụng riêng cho một sản phẩm)
            if (!string.IsNullOrEmpty(promo.ApplicableProductId))
            {
                var matchingItems = itemsWithFinalPrice.Where(it => it.Item.ProductVariant?.ProductId == promo.ApplicableProductId).ToList();
                if (!matchingItems.Any())
                {
                    var targetProduct = await _context.Products.FindAsync(promo.ApplicableProductId);
                    var prodName = targetProduct?.ProductName ?? "sản phẩm quy định";
                    return (false, $"Mã giảm giá này chỉ áp dụng cho sản phẩm: {prodName}.", 0);
                }

                decimal eligibleSubTotal = matchingItems.Sum(it => it.FinalPrice * it.Item.Quantity);
                decimal discount = 0;

                if (string.Equals(promo.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(promo.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
                {
                    discount = eligibleSubTotal * (promo.DiscountValue / 100);
                    if (promo.MaxDiscountAmount.HasValue && discount > promo.MaxDiscountAmount.Value)
                    {
                        discount = promo.MaxDiscountAmount.Value;
                    }
                }
                else
                {
                    discount = promo.DiscountValue;
                }

                if (discount > eligibleSubTotal)
                {
                    discount = eligibleSubTotal;
                }

                return (true, "Áp dụng mã giảm giá thành công!", Math.Round(discount, 2));
            }

            // 2. Kiểm tra ComboProductIds (áp dụng khi mua combo nhiều sản phẩm)
            if (!string.IsNullOrEmpty(promo.ComboProductIds))
            {
                var comboProductIds = promo.ComboProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();

                bool isComboSatisfied = comboProductIds.All(pid => items.Any(it => it.ProductVariant?.ProductId == pid));
                if (!isComboSatisfied)
                {
                    var comboProducts = await _context.Products.Where(p => comboProductIds.Contains(p.Id)).ToListAsync();
                    var prodNames = string.Join(", ", comboProducts.Select(p => p.ProductName));
                    return (false, $"Mã này chỉ áp dụng khi mua combo các sản phẩm: {prodNames}.", 0);
                }

                var matchingItems = itemsWithFinalPrice.Where(it => comboProductIds.Contains(it.Item.ProductVariant?.ProductId ?? "")).ToList();
                decimal eligibleSubTotal = matchingItems.Sum(it => it.FinalPrice * it.Item.Quantity);
                decimal discount = 0;

                if (string.Equals(promo.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(promo.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
                {
                    discount = eligibleSubTotal * (promo.DiscountValue / 100);
                    if (promo.MaxDiscountAmount.HasValue && discount > promo.MaxDiscountAmount.Value)
                    {
                        discount = promo.MaxDiscountAmount.Value;
                    }
                }
                else
                {
                    discount = promo.DiscountValue;
                }

                if (discount > eligibleSubTotal)
                {
                    discount = eligibleSubTotal;
                }

                return (true, "Áp dụng mã giảm giá thành công!", Math.Round(discount, 2));
            }

            // 3. Voucher thông thường (áp dụng trên toàn bộ đơn hàng)
            {
                decimal discount = 0;

                if (string.Equals(promo.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(promo.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
                {
                    discount = orderTotalForManualPromo * (promo.DiscountValue / 100);
                    if (promo.MaxDiscountAmount.HasValue && discount > promo.MaxDiscountAmount.Value)
                    {
                        discount = promo.MaxDiscountAmount.Value;
                    }
                }
                else
                {
                    discount = promo.DiscountValue;
                }

                if (discount > orderTotalForManualPromo)
                {
                    discount = orderTotalForManualPromo;
                }

                return (true, "Áp dụng mã giảm giá thành công!", Math.Round(discount, 2));
            }
        }

        public async Task<List<PaymentMethod>> GetPaymentMethodsAsync()
        {
            return await _context.PaymentMethods.AsNoTracking().ToListAsync();
        }

        public async Task<Order> CreateOrderAsync(string userId, Address address, string paymentMethodId, string? appliedPromoCode, List<Cart> items, string? shippingFullName, string? shippingPhoneNumber, string? buyNowVariantId, decimal shippingFee = 40000m, string shippingCarrier = "Tiêu chuẩn")
        {
            var paymentMethod = await _context.PaymentMethods.FindAsync(paymentMethodId);
            if (paymentMethod == null) throw new InvalidOperationException("Phương thức thanh toán không hợp lệ.");

            var orderOriginalTotal = items.Sum(c => (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity);
            
            // 1. Tính các khuyến mãi tự động (giảm trực tiếp trên sản phẩm & combo)
            var (itemTotalAfterDirect, comboDiscount, comboDetails) = await CalculateAutoPromotionsAsync(items);
            var autoDiscountAmount = orderOriginalTotal - itemTotalAfterDirect + comboDiscount;
            var orderTotalForManualPromo = Math.Max(0, itemTotalAfterDirect - comboDiscount);

            decimal manualDiscountAmount = 0;
            string? promotionId = null;

            // 2. Tính mã voucher thủ công (nếu khách hàng áp dụng)
            if (!string.IsNullOrWhiteSpace(appliedPromoCode))
            {
                int qty = 1;
                if (!string.IsNullOrEmpty(buyNowVariantId) && items.Count > 0)
                {
                    qty = items[0].Quantity;
                }

                var (voucherSuccess, voucherMessage, voucherDiscount) = await ValidateAndCalculateVoucherAsync(appliedPromoCode, userId, buyNowVariantId, qty);
                if (voucherSuccess)
                {
                    manualDiscountAmount = voucherDiscount;
                    var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.PromoCode == appliedPromoCode && p.IsActive);
                    if (promo != null)
                    {
                        promotionId = promo.Id;
                        promo.UsedCount += 1;
                        _context.Promotions.Update(promo);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Không thể áp dụng mã giảm giá: {voucherMessage}");
                }
            }

            var discountAmount = autoDiscountAmount + manualDiscountAmount;
            var orderTotal = orderOriginalTotal;

            // Check stock of all items first
            foreach (var c in items)
            {
                var pv = await _context.ProductVariants
                    .Include(x => x.Product)
                    .Include(x => x.Size)
                    .Include(x => x.Color)
                    .FirstOrDefaultAsync(x => x.Id == c.ProductVariantId);

                if (pv == null || !pv.IsActive)
                {
                    throw new InvalidOperationException("Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");
                }

                if (pv.StockQuantity < c.Quantity)
                {
                    throw new InvalidOperationException($"Sản phẩm '{pv.Product?.ProductName} - Màu: {pv.Color?.ColorName} - Size: {pv.Size?.SizeName}' chỉ còn {pv.StockQuantity} sản phẩm trong kho. Vui lòng giảm số lượng.");
                }
            }

            var finalAmount = orderTotal - discountAmount + shippingFee;

            // Phân bổ giảm giá xuống từng SP theo tỉ lệ giá trị
            var discountAllocations = new Dictionary<string, decimal>();
            if (discountAmount > 0 && orderTotal > 0)
            {
                decimal allocatedSum = 0;
                string? lastVariantId = null;
                foreach (var c in items)
                {
                    decimal itemTotal = (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity;
                    decimal ratio = itemTotal / orderTotal;
                    decimal allocated = Math.Round(discountAmount * ratio, 2);
                    discountAllocations[c.ProductVariantId] = allocated;
                    allocatedSum += allocated;
                    lastVariantId = c.ProductVariantId;
                }
                // Điều chỉnh sai số làm tròn vào item cuối
                if (lastVariantId != null && allocatedSum != discountAmount)
                {
                    discountAllocations[lastVariantId] += (discountAmount - allocatedSum);
                }
            }

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
                ShippingFee = shippingFee,
                FinalAmount = finalAmount,
                ShippingFullName = !string.IsNullOrWhiteSpace(shippingFullName) ? shippingFullName : "",
                ShippingPhoneNumber = !string.IsNullOrWhiteSpace(shippingPhoneNumber) ? shippingPhoneNumber : "",
                ShippingAddress = $"{address.SpecificAddress} | Vận chuyển: {shippingCarrier}",
                ShippingCity = address.City,
                ShippingDistrict = address.District,
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var c in items)
            {
                var pv = await _context.ProductVariants
                    .Include(x => x.Product)
                    .Include(x => x.Size)
                    .Include(x => x.Color)
                    .FirstOrDefaultAsync(x => x.Id == c.ProductVariantId);

                if (pv == null) throw new InvalidOperationException("Biến thể sản phẩm không tồn tại.");

                var product = pv.Product;

                // Trừ tồn kho
                pv.StockQuantity -= c.Quantity;
                _context.ProductVariants.Update(pv);

                // Ghi nhận lịch sử giao dịch kho
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductVariantId = c.ProductVariantId,
                    TransactionType = "OUT",
                    QuantityChange = -c.Quantity,
                    RemainingStock = pv.StockQuantity,
                    TransactionDate = DateTime.Now,
                    ReferenceId = order.Id,
                    Note = $"Xuất kho bán hàng cho Đơn hàng: {order.Id}"
                });

                // FIFO: Lấy các lô cũ nhất còn hàng
                var lots = await _context.InventoryLots
                    .Where(l => l.ProductVariantId == c.ProductVariantId && l.RemainingQuantity > 0)
                    .OrderBy(l => l.ReceivedDate)
                    .ThenBy(l => l.Id)
                    .ToListAsync();

                int qtyToDeduct = c.Quantity;
                decimal totalCostFromLots = 0;
                var lotDeductions = new List<(string LotId, int Qty, decimal UnitCost)>();

                foreach (var lot in lots)
                {
                    if (qtyToDeduct <= 0) break;
                    int deduct = Math.Min(lot.RemainingQuantity, qtyToDeduct);
                    lot.RemainingQuantity -= deduct;
                    totalCostFromLots += deduct * lot.UnitCost;
                    qtyToDeduct -= deduct;
                    lotDeductions.Add((lot.Id, deduct, lot.UnitCost));
                }

                // Giá vốn: FIFO nếu có lô, fallback MAC cho dữ liệu cũ
                decimal fifoCost = lotDeductions.Any()
                    ? Math.Round(totalCostFromLots / c.Quantity, 2)
                    : pv.AverageCostPrice;

                var detail = new OrderDetail
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    ProductVariantId = c.ProductVariantId,
                    Quantity = c.Quantity,
                    PurchasedPrice = product?.CurrentPrice ?? 0,
                    CostPrice = fifoCost,
                    DiscountAllocation = discountAllocations.GetValueOrDefault(c.ProductVariantId, 0),
                    ProductNameSnapshot = product?.ProductName ?? "",
                    SizeNameSnapshot = pv.Size?.SizeName ?? "",
                    ColorNameSnapshot = pv.Color?.ColorName ?? ""
                };

                _context.OrderDetails.Add(detail);

                // Lưu chi tiết lô xuất FIFO
                foreach (var (lotId, qty, unitCost) in lotDeductions)
                {
                    _context.OrderDetailLots.Add(new OrderDetailLot
                    {
                        Id = Guid.NewGuid().ToString(),
                        OrderDetailId = detail.Id,
                        InventoryLotId = lotId,
                        Quantity = qty,
                        UnitCost = unitCost
                    });
                }
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

            return await UpdateOrderStatusAsync(orderId, 4);
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

        public async Task<Order?> GetOrderByTrackingNumberAsync(string trackingNumber)
        {
            return await _context.Orders
                .Include(o => o.PaymentMethod)
                .FirstOrDefaultAsync(o => o.ShippingAddress.Contains($"Tracking: {trackingNumber}"));
        }

        public async Task<(decimal ItemTotal, decimal ComboDiscount, List<string> ComboDetails)> CalculateAutoPromotionsAsync(List<Cart> items)
        {
            var now = DateTime.Now;
            var autoPromos = await _context.Promotions
                .Where(p => p.IsActive && p.IsAutoApply && p.StartDate <= now && p.EndDate >= now)
                .ToListAsync();

            decimal itemTotal = 0;
            // 1. Giảm trực tiếp
            var itemsWithDiscount = items.Select(c => {
                var basePrice = c.ProductVariant?.Product?.CurrentPrice ?? 0;
                decimal directDiscount = 0;
                var prodPromo = autoPromos.FirstOrDefault(p => p.ApplicableProductId == c.ProductVariant?.ProductId);
                if (prodPromo != null)
                {
                    if (string.Equals(prodPromo.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase) || 
                        string.Equals(prodPromo.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
                    {
                        directDiscount = basePrice * (prodPromo.DiscountValue / 100);
                        if (prodPromo.MaxDiscountAmount.HasValue && directDiscount > prodPromo.MaxDiscountAmount.Value)
                        {
                            directDiscount = prodPromo.MaxDiscountAmount.Value;
                        }
                    }
                    else
                    {
                        directDiscount = prodPromo.DiscountValue;
                    }
                }
                var finalPrice = Math.Max(0, basePrice - directDiscount);
                itemTotal += finalPrice * c.Quantity;
                return new { Item = c, FinalPrice = finalPrice };
            }).ToList();

            // 2. Giảm Combo
            decimal totalComboDiscount = 0;
            var comboDetails = new List<string>();

            var comboPromos = autoPromos.Where(p => !string.IsNullOrEmpty(p.ComboProductIds)).ToList();
            foreach (var promo in comboPromos)
            {
                var comboProductIds = promo.ComboProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();

                if (comboProductIds.Count > 0)
                {
                    bool isComboSatisfied = comboProductIds.All(pid => items.Any(it => it.ProductVariant?.ProductId == pid));
                    if (isComboSatisfied)
                    {
                        var satisfiedCounts = comboProductIds.Select(pid => {
                            return items.Where(it => it.ProductVariant?.ProductId == pid).Sum(it => it.Quantity);
                        }).ToList();

                        int comboCount = satisfiedCounts.Min();
                        if (comboCount > 0)
                        {
                            decimal discountVal = 0;
                            if (string.Equals(promo.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase) || 
                                string.Equals(promo.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
                            {
                                decimal comboSubTotal = itemsWithDiscount.Where(it => comboProductIds.Contains(it.Item.ProductVariant?.ProductId ?? ""))
                                    .Sum(it => it.FinalPrice * it.Item.Quantity);

                                discountVal = comboSubTotal * (promo.DiscountValue / 100);
                                if (promo.MaxDiscountAmount.HasValue && discountVal > promo.MaxDiscountAmount.Value)
                                {
                                    discountVal = promo.MaxDiscountAmount.Value;
                                }
                            }
                            else
                            {
                                discountVal = promo.DiscountValue * comboCount;
                            }

                            totalComboDiscount += discountVal;
                            comboDetails.Add($"{promo.PromoCode} (Giảm combo {discountVal:N0}đ)");
                        }
                    }
                }
            }

            return (itemTotal, totalComboDiscount, comboDetails);
        }
    }
}