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

            var cartDtos = items.Select(c => new CartItemViewModel
            {
                CartId = c.Id,
                ProductVariantId = c.ProductVariantId,
                ProductName = c.ProductVariant?.Product?.ProductName ?? "",
                SizeName = c.ProductVariant?.Size?.SizeName ?? "",
                ColorName = c.ProductVariant?.Color?.ColorName ?? "",
                Price = c.ProductVariant?.Product?.CurrentPrice ?? 0,
                Quantity = c.Quantity,
                LineTotal = (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity
            }).ToList();

            ViewBag.Items = cartDtos;
            ViewBag.Total = cartDtos.Sum(x => x.LineTotal);
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
            public int Quantity { get; set; }
            public decimal LineTotal { get; set; }
        }
    }
}

