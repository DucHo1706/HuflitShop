using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HuflitShopCore.Models;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HuflitShopCore.Controllers
{
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;
        private readonly PromotionService _promotionService;
        private readonly VnPayService _vnPayService;

        public OrderController(OrderService orderService, PromotionService promotionService, VnPayService vnPayService)
        {
            _orderService = orderService;
            _promotionService = promotionService;
            _vnPayService = vnPayService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout(string? buyNowVariantId, int buyNowQty = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Login");

            await _orderService.EnsurePaymentMethodsSeededAsync();

            var items = await _orderService.GetCheckoutItemsAsync(userId, buyNowVariantId, buyNowQty);
            if (items.Count == 0 && string.IsNullOrEmpty(buyNowVariantId))
            {
                return RedirectToAction("Cart", "Cart");
            }

            var total = items.Sum(c => (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity);
            ViewBag.Total = total;
            ViewBag.CartItems = items;
            ViewBag.BuyNowVariantId = buyNowVariantId;
            ViewBag.BuyNowQty = buyNowQty;

            var paymentMethods = await _orderService.GetPaymentMethodsAsync();
            ViewBag.PaymentMethods = paymentMethods;

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

            await _orderService.EnsurePaymentMethodsSeededAsync();

            if (string.IsNullOrEmpty(paymentMethodId))
            {
                paymentMethodId = "pm-cod";
            }

            var items = await _orderService.GetCheckoutItemsAsync(userId, buyNowVariantId, buyNowQty);
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
                ViewBag.BuyNowVariantId = buyNowVariantId;
                ViewBag.BuyNowQty = buyNowQty;
                
                var paymentMethods = await _orderService.GetPaymentMethodsAsync();
                ViewBag.PaymentMethods = paymentMethods;
                
                return View("Checkout", address);
            }

            Order order;
            try
            {
                var fullName = !string.IsNullOrWhiteSpace(shippingFullName) ? shippingFullName : (User.FindFirstValue("Name") ?? "");
                var phone = !string.IsNullOrWhiteSpace(shippingPhoneNumber) ? shippingPhoneNumber : (User.FindFirstValue("Phone") ?? "");

                order = await _orderService.CreateOrderAsync(
                    userId, 
                    address, 
                    paymentMethodId, 
                    appliedPromoCode, 
                    items, 
                    fullName, 
                    phone, 
                    buyNowVariantId);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Total = items.Sum(c => (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity);
                ViewBag.CartItems = items;
                ViewBag.BuyNowVariantId = buyNowVariantId;
                ViewBag.BuyNowQty = buyNowQty;

                var paymentMethods = await _orderService.GetPaymentMethodsAsync();
                ViewBag.PaymentMethods = paymentMethods;
                return View("Checkout", address);
            }

            if (paymentMethodId == "pm-vnpay")
            {
                string returnUrl = Url.Action("VnPayReturn", "Order", null, Request.Scheme) ?? "https://localhost:7107/Order/VnPayReturn";
                string remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                string paymentUrl = _vnPayService.CreatePaymentUrl(order, returnUrl, remoteIpAddress);
                return Redirect(paymentUrl);
            }

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

            var promo = await _promotionService.ValidatePromoCodeAsync(promoCode, orderTotal);
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

            decimal discount = _promotionService.CalculateDiscount(promo, orderTotal);

            return Json(new { 
                success = true, 
                discount = discount, 
                finalAmount = orderTotal - discount,
                message = "Áp dụng mã giảm giá thành công!" 
            });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAvailableVouchers(decimal orderTotal)
        {
            var now = DateTime.Now;
            var promotions = await _promotionService.GetAllPromotionsAsync();
            
            // Filter ongoing promotions
            var ongoing = promotions.Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now).ToList();

            var result = ongoing.Select(p => {
                bool isLimitReached = p.UsageLimit.HasValue && p.UsedCount >= p.UsageLimit.Value;
                bool isMinAmountMet = orderTotal >= p.MinOrderAmount;
                bool isApplicable = isMinAmountMet && !isLimitReached;

                decimal neededAmount = 0;
                string message = "";

                if (isLimitReached)
                {
                    message = "Hết lượt sử dụng";
                }
                else if (!isMinAmountMet)
                {
                    neededAmount = p.MinOrderAmount - orderTotal;
                    message = $"Mua thêm {neededAmount:N0}đ để áp dụng";
                }
                else
                {
                    message = "Đủ điều kiện";
                }

                // Description
                string description = "";
                if (string.Equals(p.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase))
                {
                    description = $"Giảm {p.DiscountValue:0.##}%" + (p.MaxDiscountAmount.HasValue ? $" (Tối đa {p.MaxDiscountAmount.Value:N0}đ)" : "");
                }
                else
                {
                    description = $"Giảm {p.DiscountValue:N0}đ";
                }
                
                description += $" cho đơn từ {p.MinOrderAmount:N0}đ";

                return new {
                    id = p.Id,
                    promoCode = p.PromoCode,
                    discountType = p.DiscountType,
                    discountValue = p.DiscountValue,
                    minOrderAmount = p.MinOrderAmount,
                    maxDiscountAmount = p.MaxDiscountAmount,
                    usageLimit = p.UsageLimit,
                    usedCount = p.UsedCount,
                    isApplicable = isApplicable,
                    neededAmount = neededAmount,
                    message = message,
                    description = description,
                    endDateFormatted = p.EndDate.ToString("dd/MM/yyyy HH:mm")
                };
            }).OrderByDescending(x => x.isApplicable)
              .ThenBy(x => x.neededAmount)
              .ToList();

            return Json(result);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> VnPayReturn()
        {
            var (orderId, success) = await _vnPayService.ProcessVnPayReturnAsync(Request.Query);
            return RedirectToAction("Success", new { id = orderId, paymentSuccess = success });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Success(string id, bool? paymentSuccess)
        {
            var order = await _orderService.GetOrderForSuccessAsync(id);
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
