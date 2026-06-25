using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using HuflitShopCore.Models;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuflitShopCore.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cartService;

        public CartController(CartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<IActionResult> Cart()
        {
            var isAuth = User.Identity != null && User.Identity.IsAuthenticated;
            var userId = isAuth ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

            var guestCartId = isAuth ? null : Request.Cookies["GuestCartId"];

            var items = await _cartService.GetCartItemsAsync(userId, guestCartId);
            var autoPromos = await _cartService.GetActiveAutoPromotionsAsync();

            var cartDtos = items.Select(c => {
                var basePrice = c.ProductVariant?.Product?.CurrentPrice ?? 0;
                
                // 1. Tính toán giảm giá trực tiếp theo sản phẩm
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

                return new CartItemViewModel
                {
                    CartId = c.Id,
                    ProductVariantId = c.ProductVariantId,
                    ProductName = c.ProductVariant?.Product?.ProductName ?? "",
                    SizeName = c.ProductVariant?.Size?.SizeName ?? "",
                    ColorName = c.ProductVariant?.Color?.ColorName ?? "",
                    OriginalPrice = basePrice,
                    DiscountAmount = directDiscount,
                    Price = finalPrice,
                    Quantity = c.Quantity,
                    LineTotal = finalPrice * c.Quantity
                };
            }).ToList();

            // 2. Tính toán giảm giá theo Combo
            decimal totalComboDiscount = 0;
            var comboDetailsList = new List<string>();

            var comboPromos = autoPromos.Where(p => !string.IsNullOrEmpty(p.ComboProductIds)).ToList();
            foreach (var promo in comboPromos)
            {
                var comboProductIds = promo.ComboProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();

                if (comboProductIds.Count > 0)
                {
                    // Kiểm tra xem tất cả các sản phẩm của combo có mặt trong giỏ hàng hay không
                    bool isComboSatisfied = comboProductIds.All(pid => cartDtos.Any(dto => {
                        var item = items.First(it => it.ProductVariantId == dto.ProductVariantId);
                        return item.ProductVariant?.ProductId == pid;
                    }));

                    if (isComboSatisfied)
                    {
                        var satisfiedCounts = comboProductIds.Select(pid => {
                            var matchingDtos = cartDtos.Where(dto => {
                                var item = items.First(it => it.ProductVariantId == dto.ProductVariantId);
                                return item.ProductVariant?.ProductId == pid;
                            });
                            return matchingDtos.Sum(d => d.Quantity);
                        }).ToList();

                        int comboCount = satisfiedCounts.Min();
                        if (comboCount > 0)
                        {
                            decimal discountVal = 0;
                            if (string.Equals(promo.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase) || 
                                string.Equals(promo.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
                            {
                                decimal comboSubTotal = cartDtos.Where(dto => {
                                    var item = items.First(it => it.ProductVariantId == dto.ProductVariantId);
                                    return comboProductIds.Contains(item.ProductVariant?.ProductId ?? "");
                                }).Sum(dto => dto.Price * dto.Quantity);

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
                            comboDetailsList.Add($"{promo.PromoCode} (Giảm combo {discountVal:N0}đ)");
                        }
                    }
                }
            }

            var subTotal = cartDtos.Sum(x => x.LineTotal);
            var finalTotal = Math.Max(0, subTotal - totalComboDiscount);

            ViewBag.Items = cartDtos;
            ViewBag.SubTotal = subTotal;
            ViewBag.ComboDiscount = totalComboDiscount;
            ViewBag.ComboDetails = comboDetailsList;
            ViewBag.Total = finalTotal;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(string cartId, int quantity)
        {
            await _cartService.UpdateQuantityAsync(cartId, quantity);
            return RedirectToAction("Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(string cartId)
        {
            await _cartService.RemoveCartItemAsync(cartId);
            return RedirectToAction("Cart");
        }

        public class CartItemViewModel
        {
            public string CartId { get; set; } = string.Empty;
            public string ProductVariantId { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string SizeName { get; set; } = string.Empty;
            public string ColorName { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public decimal OriginalPrice { get; set; }
            public decimal DiscountAmount { get; set; }
            public int Quantity { get; set; }
            public decimal LineTotal { get; set; }
        }
    }
}

