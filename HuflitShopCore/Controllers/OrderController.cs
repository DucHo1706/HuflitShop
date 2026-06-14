using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HuflitShopCore.Data;
using HuflitShopCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuflitShopCore.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Login");

            var items = await _context.Carts
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

            var total = items.Sum(c => (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity);
            ViewBag.Total = total;

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
        public async Task<IActionResult> PlaceOrder(Address address)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Login");

            if (!ModelState.IsValid)
            {
                // Recalculate total for re-render.
                var reItems = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Include(c => c.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .ToListAsync();
                ViewBag.Total = reItems.Sum(c => (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity);
                address.UserId = userId;
                return View("Checkout", address);
            }

            var items = await _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .Include(c => c.ProductVariant)
                    .ThenInclude(pv => pv.Size)
                .Include(c => c.ProductVariant)
                    .ThenInclude(pv => pv.Color)
                .ToListAsync();

            if (items.Count == 0)
                return RedirectToAction("Cart", "Cart");

            var paymentMethod = await _context.PaymentMethods.FirstOrDefaultAsync();
            if (paymentMethod == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy phương thức thanh toán.");
                ViewBag.Total = 0;
                return View("Checkout", address);
            }

            // Promotion not implemented in this minimal flow.
            var orderTotal = items.Sum(c => (c.ProductVariant?.Product?.CurrentPrice ?? 0) * c.Quantity);

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                PaymentMethodId = paymentMethod.Id,
                PromotionId = string.Empty,
                OrderDate = DateTime.UtcNow,
                OrderStatus = 0,
                PaymentStatus = 1,
                TotalAmount = orderTotal,
                DiscountAmount = 0,
                ShippingFee = 0,
                FinalAmount = orderTotal,
                ShippingFullName = User.FindFirstValue("Name") ?? "",
                ShippingPhoneNumber = User.FindFirstValue("Phone") ?? "",
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
            _context.Carts.RemoveRange(items);
            await _context.SaveChangesAsync();

            return RedirectToAction("Success", new { id = order.Id });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Success(string id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            ViewBag.OrderId = order.Id;
            ViewBag.FinalAmount = order.FinalAmount;
            ViewBag.ShippingCity = order.ShippingCity;
            ViewBag.ShippingDistrict = order.ShippingDistrict;
            ViewBag.ShippingAddress = order.ShippingAddress;

            return View();
        }
    }
}

