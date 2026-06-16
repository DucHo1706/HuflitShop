using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HuflitShopCore.Data;
using HuflitShopCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HuflitShopCore.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public OrderController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private async Task EnsurePaymentMethodsSeededAsync()
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout(string? buyNowVariantId, int buyNowQty = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Login");

            await EnsurePaymentMethodsSeededAsync();

            List<Cart> items;
            if (!string.IsNullOrEmpty(buyNowVariantId))
            {
                var variant = await _context.ProductVariants
                    .Include(pv => pv.Product)
                    .Include(pv => pv.Size)
                    .Include(pv => pv.Color)
                    .FirstOrDefaultAsync(pv => pv.Id == buyNowVariantId && pv.IsActive && pv.StockQuantity > 0);

                if (variant == null)
                {
                    return RedirectToAction("Product", "Product");
                }

                items = new List<Cart>
                {
                    new Cart
                    {
                        Id = System.Guid.NewGuid().ToString(),
                        ProductVariantId = buyNowVariantId,
                        ProductVariant = variant,
                        Quantity = buyNowQty,
                        UserId = userId,
                        CreatedAt = System.DateTime.UtcNow
                    }
                };
                ViewBag.BuyNowVariantId = buyNowVariantId;
                ViewBag.BuyNowQty = buyNowQty;
            }
            else
            {
                items = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Include(c => c.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Include(c => c.ProductVariant)
                        .ThenInclude(pv => pv.Size)
                    .Include(c => c.ProductVariant)
                        .ThenInclude(pv => pv.Color)
                    .ToListAsync();

                if (items.Count == 0)
                {
                    return RedirectToAction("Cart", "Cart");
                }
            }

            var total = items.Sum(c => (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity);
            ViewBag.Total = total;
            ViewBag.CartItems = items;

            var paymentMethods = await _context.PaymentMethods.ToListAsync();
            ViewBag.PaymentMethods = paymentMethods;

            // Provide empty address to bind form.
            var address = new Address
            {
                UserId = userId
            };

            return View(address);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Address address, string paymentMethodId, string? shippingFullName, string? shippingPhoneNumber, string? appliedPromoCode, string? buyNowVariantId, int buyNowQty = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Login");

            await EnsurePaymentMethodsSeededAsync();

            if (string.IsNullOrEmpty(paymentMethodId))
            {
                paymentMethodId = "pm-cod";
            }

            List<Cart> items;
            if (!string.IsNullOrEmpty(buyNowVariantId))
            {
                var variant = await _context.ProductVariants
                    .Include(pv => pv.Product)
                    .Include(pv => pv.Size)
                    .Include(pv => pv.Color)
                    .FirstOrDefaultAsync(pv => pv.Id == buyNowVariantId && pv.IsActive && pv.StockQuantity > 0);

                if (variant == null)
                {
                    return RedirectToAction("Product", "Product");
                }

                items = new List<Cart>
                {
                    new Cart
                    {
                        Id = System.Guid.NewGuid().ToString(),
                        ProductVariantId = buyNowVariantId,
                        ProductVariant = variant,
                        Quantity = buyNowQty,
                        UserId = userId,
                        CreatedAt = System.DateTime.UtcNow
                    }
                };
                ViewBag.BuyNowVariantId = buyNowVariantId;
                ViewBag.BuyNowQty = buyNowQty;
            }
            else
            {
                items = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Include(c => c.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Include(c => c.ProductVariant)
                        .ThenInclude(pv => pv.Size)
                    .Include(c => c.ProductVariant)
                        .ThenInclude(pv => pv.Color)
                    .ToListAsync();
            }

            if (items.Count == 0)
            {
                return RedirectToAction("Cart", "Cart");
            }

            address.UserId = userId;
            ModelState.Remove("UserId");
            ModelState.Remove("Id");

            if (!ModelState.IsValid)
            {
                ViewBag.Total = items.Sum(c => (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity);
                ViewBag.CartItems = items;
                
                var paymentMethods = await _context.PaymentMethods.ToListAsync();
                ViewBag.PaymentMethods = paymentMethods;
                
                return View("Checkout", address);
            }

            var paymentMethod = await _context.PaymentMethods.FindAsync(paymentMethodId);
            if (paymentMethod == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy phương thức thanh toán hợp lệ.");
                ViewBag.Total = items.Sum(c => (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity);
                ViewBag.CartItems = items;
                var paymentMethods = await _context.PaymentMethods.ToListAsync();
                ViewBag.PaymentMethods = paymentMethods;
                return View("Checkout", address);
            }

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
                ShippingFullName = !string.IsNullOrWhiteSpace(shippingFullName) ? shippingFullName : (User.FindFirstValue("Name") ?? ""),
                ShippingPhoneNumber = !string.IsNullOrWhiteSpace(shippingPhoneNumber) ? shippingPhoneNumber : (User.FindFirstValue("Phone") ?? ""),
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

            // Clear cart after creating order
            if (string.IsNullOrEmpty(buyNowVariantId))
            {
                _context.Carts.RemoveRange(items);
                await _context.SaveChangesAsync();
            }

            // Nếu thanh toán online qua VNPay
            if (paymentMethodId == "pm-vnpay")
            {
                string tmnCode = _configuration["VNPAY:TmnCode"] ?? "AMYATERD";
                string hashSecret = _configuration["VNPAY:HashSecret"] ?? "NZPOHSDQOSQWZKNDDHPXPHRYNCPKNXPC";
                string baseUrl = _configuration["VNPAY:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
                string returnUrl = _configuration["VNPAY:ReturnUrl"] ?? (Url.Action("VnPayReturn", "Order", null, Request.Scheme) ?? "https://localhost:7107/Order/VnPayReturn");

                var vnpay = new Helpers.VnPayLibrary();
                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", tmnCode);
                vnpay.AddRequestData("vnp_Amount", ((long)(order.FinalAmount * 100)).ToString());
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {order.Id}");
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
                vnpay.AddRequestData("vnp_TxnRef", order.Id);

                string paymentUrl = vnpay.CreateRequestUrl(baseUrl, hashSecret);
                return Redirect(paymentUrl);
            }

            // Tiền mặt COD
            return RedirectToAction("Success", new { id = order.Id });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ApplyVoucher(string promoCode, decimal orderTotal)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
            }

            var promo = await _context.Promotions
                .FirstOrDefaultAsync(p => p.PromoCode == promoCode && p.IsActive);

            if (promo == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã bị khóa." });
            }

            if (promo.StartDate > DateTime.Now || promo.EndDate < DateTime.Now)
            {
                return Json(new { success = false, message = "Mã giảm giá đã hết hạn sử dụng." });
            }

            if (promo.MinOrderAmount > orderTotal)
            {
                return Json(new { success = false, message = $"Đơn hàng tối thiểu {promo.MinOrderAmount:N0}đ để sử dụng mã này." });
            }

            if (promo.UsageLimit.HasValue && promo.UsedCount >= promo.UsageLimit.Value)
            {
                return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng." });
            }

            decimal discount = 0;
            if (string.Equals(promo.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(promo.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
            {
                discount = orderTotal * (promo.DiscountValue / 100);
                if (promo.MaxDiscountAmount.HasValue && discount > promo.MaxDiscountAmount.Value)
                {
                    discount = promo.MaxDiscountAmount.Value;
                }
            }
            else
            {
                discount = promo.DiscountValue;
            }

            if (discount > orderTotal)
            {
                discount = orderTotal;
            }

            return Json(new { 
                success = true, 
                discount = discount, 
                finalAmount = orderTotal - discount,
                message = "Áp dụng mã giảm giá thành công!" 
            });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> VnPayReturn()
        {
            var hashSecret = _configuration["VNPAY:HashSecret"] ?? "NZPOHSDQOSQWZKNDDHPXPHRYNCPKNXPC";
            var vnpayData = Request.Query;
            var vnpay = new Helpers.VnPayLibrary();

            foreach (var key in vnpayData.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, vnpayData[key]!);
                }
            }

            string orderId = vnpay.GetResponseData("vnp_TxnRef");
            string responseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string secureHash = vnpayData["vnp_SecureHash"]!;

            bool checkSignature = vnpay.ValidateSignature(secureHash, hashSecret);

            if (checkSignature)
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    if (responseCode == "00")
                    {
                        // Thanh toán thành công
                        order.PaymentStatus = 1; // Đã thanh toán
                        _context.Orders.Update(order);
                        await _context.SaveChangesAsync();

                        return RedirectToAction("Success", new { id = order.Id, paymentSuccess = true });
                    }
                    else
                    {
                        // Thanh toán thất bại, cập nhật trạng thái đơn hủy
                        order.OrderStatus = 4; // Hủy đơn
                        _context.Orders.Update(order);
                        await _context.SaveChangesAsync();

                        return RedirectToAction("Success", new { id = order.Id, paymentSuccess = false });
                    }
                }
            }

            return RedirectToAction("Success", new { id = orderId, paymentSuccess = false });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Success(string id, bool? paymentSuccess)
        {
            var order = await _context.Orders
                .Include(o => o.PaymentMethod)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            ViewBag.OrderId = order.Id;
            ViewBag.FinalAmount = order.FinalAmount;
            ViewBag.ShippingCity = order.ShippingCity;
            ViewBag.ShippingDistrict = order.ShippingDistrict;
            ViewBag.ShippingAddress = order.ShippingAddress;
            ViewBag.PaymentMethodName = order.PaymentMethod?.MethodName ?? "Chưa xác định";
            ViewBag.PaymentSuccess = paymentSuccess;

            return View();
        }
    }
}

