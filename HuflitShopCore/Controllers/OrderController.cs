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
        private readonly GhnService _ghnService;
        private readonly GeoapifyService _geoapifyService;

        public OrderController(
            OrderService orderService,
            PromotionService promotionService,
            VnPayService vnPayService,
            GhnService ghnService,
            GeoapifyService geoapifyService)
        {
            _orderService = orderService;
            _promotionService = promotionService;
            _vnPayService = vnPayService;
            _ghnService = ghnService;
            _geoapifyService = geoapifyService;
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

            decimal shippingFee = 0m;

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
        [HttpGet]
        public async Task<IActionResult> GetProvinces(CancellationToken cancellationToken)
        {
            try { return Json(new { success = true, data = await _ghnService.GetProvincesAsync(cancellationToken) }); }
            catch (GhnApiException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetDistricts(int provinceId, CancellationToken cancellationToken)
        {
            try { return Json(new { success = true, data = await _ghnService.GetDistrictsAsync(provinceId, cancellationToken) }); }
            catch (GhnApiException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetWards(int districtId, CancellationToken cancellationToken)
        {
            try { return Json(new { success = true, data = await _ghnService.GetWardsAsync(districtId, cancellationToken) }); }
            catch (GhnApiException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [Authorize]
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> SuggestAddresses(
            string query,
            string? ward,
            string? district,
            string? province,
            CancellationToken cancellationToken)
        {
            if (!_geoapifyService.IsConfigured)
                return Json(new { success = true, configured = false, data = Array.Empty<object>() });

            var suggestions = await _geoapifyService.SuggestAsync(
                query ?? string.Empty,
                ward,
                district,
                province,
                cancellationToken);

            return Json(new { success = true, configured = true, data = suggestions });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GetShippingFee(int districtId, string wardCode, string? buyNowVariantId, int buyNowQty = 1, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            if (districtId <= 0 || string.IsNullOrWhiteSpace(wardCode))
                return Json(new { success = false, message = "Vui lòng chọn đầy đủ địa chỉ nhận hàng." });

            try
            {
                var items = await _orderService.GetCheckoutItemsAsync(userId, buyNowVariantId, buyNowQty);
                var package = _ghnService.BuildPackage(items);
                var insuranceValue = items.Sum(x => (x.ProductVariant?.Product?.CurrentPrice ?? 0) * x.Quantity);
                var quotes = await _ghnService.GetQuotesAsync(districtId, wardCode, package, insuranceValue, cancellationToken);
                return Json(new
                {
                    success = true,
                    quotes = quotes.Select(x => new
                    {
                        x.ServiceId,
                        x.ServiceTypeId,
                        x.ServiceName,
                        x.ShippingFee,
                        expectedDeliveryTime = x.ExpectedDeliveryTime?.ToString("dd/MM/yyyy")
                    })
                });
            }
            catch (GhnApiException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Address address, string paymentMethodId, string? shippingFullName, string? shippingPhoneNumber, string? appliedPromoCode, string? buyNowVariantId, int shippingServiceId, int shippingServiceTypeId, int buyNowQty = 1, CancellationToken cancellationToken = default)
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

            if (address.ProvinceId <= 0)
                ModelState.AddModelError(nameof(Address.ProvinceId), "Vui lòng chọn Tỉnh/Thành phố.");
            if (address.DistrictId <= 0)
                ModelState.AddModelError(nameof(Address.DistrictId), "Vui lòng chọn Quận/Huyện.");
            if (string.IsNullOrWhiteSpace(address.Ward) || string.IsNullOrWhiteSpace(address.WardCode))
                ModelState.AddModelError(nameof(Address.WardCode), "Vui lòng chọn Phường/Xã.");
            if (shippingServiceId <= 0 || shippingServiceTypeId <= 0)
                ModelState.AddModelError(string.Empty, "Vui lòng chọn dịch vụ giao hàng GHN.");

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
                var destination = await _ghnService.ResolveDestinationAsync(address.ProvinceId, address.DistrictId, address.WardCode, cancellationToken);
                address.City = destination.ProvinceName;
                address.District = destination.DistrictName;
                address.Ward = destination.WardName;
                var package = _ghnService.BuildPackage(items);
                var insuranceValue = items.Sum(x => (x.ProductVariant?.Product?.CurrentPrice ?? 0) * x.Quantity);
                var quotes = await _ghnService.GetQuotesAsync(address.DistrictId, address.WardCode, package, insuranceValue, cancellationToken);
                var selectedQuote = quotes.FirstOrDefault(x => x.ServiceId == shippingServiceId && x.ServiceTypeId == shippingServiceTypeId)
                    ?? throw new InvalidOperationException("Dịch vụ GHN đã chọn không còn khả dụng. Vui lòng chọn lại.");

                order = await _orderService.CreateOrderAsync(
                    userId, 
                    address, 
                    paymentMethodId, 
                    appliedPromoCode, 
                    items, 
                    fullName, 
                    phone, 
                    buyNowVariantId,
                    selectedQuote);
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
            ViewBag.ShippingWard = order.ShippingWard;
            ViewBag.ShippingAddress = order.ShippingAddress;
            ViewBag.CarrierOrderCode = order.Shipment?.CarrierOrderCode;
            ViewBag.ShippingStatus = order.Shipment?.Status;
            ViewBag.PaymentMethodName = order.PaymentMethod?.MethodName ?? "Chưa xác định";
            ViewBag.PaymentSuccess = paymentSuccess;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Tracking(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var order = await _orderService.GetOrderByTrackingNumberAsync(id);
            if (order == null) return NotFound();
            return Redirect("https://donhang.ghn.vn/?order_code=" + Uri.EscapeDataString(id));
        }
    }
}
