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
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // NOTE: Home layout used by views expects ViewBag.Categories.
        public async Task<IActionResult> Product()
        {
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var products = await _context.Products
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> Category(string id)
        {
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var products = await _context.Products
                .Where(p => p.CategoryId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View("Product", products);
        }

        public async Task<IActionResult> Details(string id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCart(string id, string? productVariantId, int quantity = 1)
        {
            // Shop hiện tại: AddCart/BuyNow có thể gửi kèm productVariantId.
            // Nếu không có productVariantId thì fallback chọn biến thể còn hàng đầu tiên.

            if (!string.IsNullOrWhiteSpace(productVariantId))
            {
                var pv = await _context.ProductVariants
                    .FirstOrDefaultAsync(x => x.Id == productVariantId && x.IsActive && x.StockQuantity > 0);

                if (pv == null) return RedirectToAction("Index", "Home");

                await AddOrUpdateCartItemAsync(pv.Id, Math.Max(1, quantity));
                return RedirectToAction("Cart", "Cart");
            }

            // Fallback: Home/Index.cshtml truyền ProductId.
            var variant = await _context.ProductVariants
                .Where(pv => pv.ProductId == id && pv.IsActive && pv.StockQuantity > 0)
                .OrderBy(pv => pv.StockQuantity)
                .FirstOrDefaultAsync();

            if (variant == null)
                return RedirectToAction("Index", "Home");

            await AddOrUpdateCartItemAsync(variant.Id, Math.Max(1, quantity));
            return RedirectToAction("Cart", "Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(string id, string? productVariantId, int quantity = 1)
        {
            if (!string.IsNullOrWhiteSpace(productVariantId))
            {
                var pv = await _context.ProductVariants
                    .FirstOrDefaultAsync(x => x.Id == productVariantId && x.IsActive && x.StockQuantity > 0);

                if (pv == null) return RedirectToAction("Index", "Home");

                await AddOrUpdateCartItemAsync(pv.Id, Math.Max(1, quantity));
                return RedirectToAction("Checkout", "Order");
            }

            var variant = await _context.ProductVariants
                .Where(pv => pv.ProductId == id && pv.IsActive && pv.StockQuantity > 0)
                .OrderBy(pv => pv.StockQuantity)
                .FirstOrDefaultAsync();

            if (variant == null)
                return RedirectToAction("Index", "Home");

            await AddOrUpdateCartItemAsync(variant.Id, Math.Max(1, quantity));
            return RedirectToAction("Checkout", "Order");
        }

        // Backward compatibility: nếu view cũ chỉ gửi mỗi ProductId.
        // Lưu ý: action overload trùng [HttpPost] có thể gây AmbiguousMatch.
        // Vì vậy action này được đặt route riêng.
        [HttpPost("AddCartProductOnly")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> AddCartProductOnly(string id)
        {
            return AddCart(id, null, 1);
        }



        private async Task AddOrUpdateCartItemAsync(string productVariantId, int delta)
        {
            var isAuth = User.Identity != null && User.Identity.IsAuthenticated;
            var userId = isAuth ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

            string? guestCartId = Request.Cookies["GuestCartId"];
            if (string.IsNullOrEmpty(guestCartId))
            {
                guestCartId = System.Guid.NewGuid().ToString();
                Response.Cookies.Append("GuestCartId", guestCartId, new Microsoft.AspNetCore.Http.CookieOptions
                {
                    Expires = System.DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    IsEssential = true
                });
            }

            var cartKeyUserId = isAuth ? userId : null;
            var cartKeySessionId = isAuth ? null : guestCartId;

            var existing = await _context.Carts.FirstOrDefaultAsync(c =>
                c.ProductVariantId == productVariantId &&
                c.UserId == cartKeyUserId &&
                c.SessionId == cartKeySessionId);

            if (existing != null)
            {
                existing.Quantity += delta;
                _context.Carts.Update(existing);
            }
            else
            {
                var cart = new Cart
                {
                    Id = System.Guid.NewGuid().ToString(),
                    ProductVariantId = productVariantId,
                    Quantity = delta,
                    UserId = cartKeyUserId,
                    SessionId = cartKeySessionId,
                    CreatedAt = System.DateTime.UtcNow
                };

                _context.Carts.Add(cart);
            }

            await _context.SaveChangesAsync();
        }
    }
}

