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
        private readonly GrabExpressService _grabExpressService;

        public OrderController(OrderService orderService, PromotionService promotionService, VnPayService vnPayService, GrabExpressService grabExpressService)
        {
            _orderService = orderService;
            _promotionService = promotionService;
            _vnPayService = vnPayService;
            _grabExpressService = grabExpressService;
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

            var (itemTotalAfterDirect, comboDiscount, comboDetails) = await _orderService.CalculateAutoPromotionsAsync(items);
            var finalProductTotal = Math.Max(0, itemTotalAfterDirect - comboDiscount);
            var originalTotal = items.Sum(c => (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity);

            // Mặc định phí ship ban đầu là 25k (tiêu chuẩn ở HCM) hoặc tính lại qua AJAX khi chọn địa chỉ
            decimal shippingFee = 25000m;

            ViewBag.OriginalTotal = originalTotal;
            ViewBag.AutoDiscount = originalTotal - finalProductTotal;
            ViewBag.Total = finalProductTotal;
            ViewBag.ShippingFee = shippingFee;
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
        public async Task<IActionResult> GetShippingFee(string city, string district, string specificAddress, decimal orderTotal)
        {
            if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(district))
            {
                return Json(new { success = false, message = "Vui lòng chọn tỉnh/thành phố và quận/huyện." });
            }

            var quotes = await _grabExpressService.GetQuotesAsync(city, district, specificAddress ?? "", orderTotal);
            return Json(new { success = true, quotes = quotes });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Address address, string paymentMethodId, string? shippingFullName, string? shippingPhoneNumber, string? appliedPromoCode, string? buyNowVariantId, decimal shippingFee, string shippingCarrier, int buyNowQty = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Login");

            await _orderService.EnsurePaymentMethodsSeededAsync();

            if (string.IsNullOrEmpty(paymentMethodId))
            {
                paymentMethodId = "pm-cod";
            }

            if (string.IsNullOrEmpty(shippingCarrier))
            {
                shippingCarrier = "Tiêu chuẩn";
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
                var (itemTotalAfterDirect, comboDiscount, comboDetails) = await _orderService.CalculateAutoPromotionsAsync(items);
                var finalProductTotal = Math.Max(0, itemTotalAfterDirect - comboDiscount);
                var originalTotal = items.Sum(c => (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity);

                ViewBag.OriginalTotal = originalTotal;
                ViewBag.AutoDiscount = originalTotal - finalProductTotal;
                ViewBag.Total = finalProductTotal;
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
                    buyNowVariantId,
                    shippingFee,
                    shippingCarrier);

                // Book GrabExpress hoặc Ahamove hỏa tốc nếu được chọn
                if (shippingCarrier.Contains("GrabExpress", StringComparison.OrdinalIgnoreCase) || 
                    shippingCarrier.Contains("Ahamove", StringComparison.OrdinalIgnoreCase))
                {
                    var bookResult = await _grabExpressService.BookDeliveryAsync(
                        shippingCarrier, 
                        fullName, 
                        phone, 
                        $"{address.SpecificAddress}, {address.District}, {address.City}");

                    if (bookResult.Success)
                    {
                        // Lưu thông tin vận đơn bổ sung vào địa chỉ ship
                        order.ShippingAddress += $" | Tracking: {bookResult.TrackingNumber} | Driver: {bookResult.DriverName} ({bookResult.DriverPhone}) - {bookResult.LicensePlate} | Link: {bookResult.TrackingUrl}";
                        await _orderService.UpdateOrderAsync(order);
                    }
                }
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
        public async Task<IActionResult> ApplyVoucher(string promoCode, decimal orderTotal, string? buyNowVariantId, int buyNowQty = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để sử dụng mã giảm giá." });
            }

            var result = await _orderService.ValidateAndCalculateVoucherAsync(promoCode, userId, buyNowVariantId, buyNowQty);
            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { 
                success = true, 
                discount = result.Discount, 
                finalAmount = orderTotal - result.Discount,
                message = result.Message 
            });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAvailableVouchers(decimal orderTotal, string? buyNowVariantId, int buyNowQty = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new List<VoucherAvailabilityItem>());
            }

            var now = DateTime.Now;
            var promotions = await _promotionService.GetAllPromotionsAsync();
            
            // Lọc các khuyến mãi đang diễn ra và KHÔNG tự động áp dụng (voucher nhập tay)
            var ongoing = promotions.Where(p => p.IsActive && !p.IsAutoApply && p.StartDate <= now && p.EndDate >= now).ToList();

            var resultList = new List<VoucherAvailabilityItem>();

            foreach (var p in ongoing)
            {
                var validationResult = await _orderService.ValidateAndCalculateVoucherAsync(p.PromoCode, userId, buyNowVariantId, buyNowQty);
                
                bool isApplicable = validationResult.Success;
                string message = isApplicable ? "Đủ điều kiện" : validationResult.Message;

                decimal neededAmount = 0;
                if (!isApplicable && validationResult.Message.Contains("tối thiểu"))
                {
                    neededAmount = p.MinOrderAmount - orderTotal;
                }

                // Description
                string description = "";
                if (string.Equals(p.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
                {
                    description = $"Giảm {p.DiscountValue:0.##}%" + (p.MaxDiscountAmount.HasValue ? $" (Tối đa {p.MaxDiscountAmount.Value:N0}đ)" : "");
                }
                else
                {
                    description = $"Giảm {p.DiscountValue:N0}đ";
                }
                
                description += $" cho đơn từ {p.MinOrderAmount:N0}đ";

                resultList.Add(new VoucherAvailabilityItem
                {
                    Id = p.Id,
                    PromoCode = p.PromoCode,
                    DiscountType = p.DiscountType,
                    DiscountValue = p.DiscountValue,
                    MinOrderAmount = p.MinOrderAmount,
                    MaxDiscountAmount = p.MaxDiscountAmount,
                    UsageLimit = p.UsageLimit,
                    UsedCount = p.UsedCount,
                    IsApplicable = isApplicable,
                    NeededAmount = neededAmount,
                    Message = message,
                    Description = description,
                    EndDateFormatted = p.EndDate.ToString("dd/MM/yyyy HH:mm")
                });
            }

            var result = resultList
                .OrderByDescending(x => x.IsApplicable)
                .ThenBy(x => x.NeededAmount)
                .ToList();

            return Json(result);
        }

        private class VoucherAvailabilityItem
        {
            public string Id { get; set; } = string.Empty;
            public string PromoCode { get; set; } = string.Empty;
            public string DiscountType { get; set; } = string.Empty;
            public decimal DiscountValue { get; set; }
            public decimal MinOrderAmount { get; set; }
            public decimal? MaxDiscountAmount { get; set; }
            public int? UsageLimit { get; set; }
            public int UsedCount { get; set; }
            public bool IsApplicable { get; set; }
            public decimal NeededAmount { get; set; }
            public string Message { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string EndDateFormatted { get; set; } = string.Empty;
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

        [HttpGet]
        public async Task<IActionResult> Tracking(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var order = await _orderService.GetOrderByTrackingNumberAsync(id);
            
            string driverName = "Nguyễn Văn Hùng";
            string driverPhone = "0903829103";
            string licensePlate = "59-K1 829.12";
            string cleanAddress = "828 Sư Vạn Hạnh, Phường 13, Quận 10";
            string district = "Quận 10";
            string city = "Hồ Chí Minh";
            string carrierName = "GrabExpress Hỏa Tốc";

            if (order != null)
            {
                district = order.ShippingDistrict;
                city = order.ShippingCity;

                string originalAddress = order.ShippingAddress ?? "";
                cleanAddress = originalAddress;
                
                if (originalAddress.Contains(" | "))
                {
                    var parts = originalAddress.Split('|');
                    cleanAddress = parts[0].Trim();
                    foreach (var part in parts)
                    {
                        var trimmed = part.Trim();
                        if (trimmed.StartsWith("Vận chuyển:"))
                        {
                            carrierName = trimmed.Replace("Vận chuyển:", "").Trim();
                        }
                        else if (trimmed.StartsWith("Driver:"))
                        {
                            var driverInfo = trimmed.Replace("Driver:", "").Trim();
                            var phoneIndex = driverInfo.IndexOf('(');
                            var plateIndex = driverInfo.IndexOf(" - ");
                            if (phoneIndex != -1 && plateIndex != -1)
                            {
                                driverName = driverInfo.Substring(0, phoneIndex).Trim();
                                driverPhone = driverInfo.Substring(phoneIndex + 1, plateIndex - phoneIndex - 2).Trim();
                                licensePlate = driverInfo.Substring(plateIndex + 3).Trim();
                            }
                            else
                            {
                                driverName = driverInfo;
                            }
                        }
                    }
                }
            }

            ViewBag.TrackingNumber = id;
            ViewBag.DriverName = driverName;
            ViewBag.DriverPhone = driverPhone;
            ViewBag.LicensePlate = licensePlate;
            ViewBag.Address = cleanAddress;
            ViewBag.District = district;
            ViewBag.City = city;
            ViewBag.CarrierName = carrierName;

            return View();
        }
    }
}
