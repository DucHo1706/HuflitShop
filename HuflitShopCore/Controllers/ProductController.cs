using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HuflitShopCore.Data;
using HuflitShopCore.Models;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuflitShopCore.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly CartService _cartService;
        private readonly ReviewService _reviewService;

        public ProductController(ProductService productService, CategoryService categoryService, CartService cartService, ReviewService reviewService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _cartService = cartService;
            _reviewService = reviewService;
        }

        // NOTE: Home layout used by views expects ViewBag.Categories.
        public async Task<IActionResult> Product(
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? trademark = null,
            string? sizeId = null,
            string? colorId = null,
            string? sortBy = null,
            int page = 1)
        {
            int pageSize = 12;
            await PopulateFilterViewBagAsync(minPrice, maxPrice, trademark, sizeId, colorId, sortBy);

            var (products, totalProducts) = await _productService.GetCatalogProductsAsync(null, null, page, pageSize, minPrice, maxPrice, trademark, sizeId, colorId, sortBy);
            int totalPages = (int)System.Math.Ceiling((double)totalProducts / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Action = "Product";

            return View(products);
        }

        public async Task<IActionResult> Category(
            string id,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? trademark = null,
            string? sizeId = null,
            string? colorId = null,
            string? sortBy = null,
            int page = 1)
        {
            int pageSize = 12;
            await PopulateFilterViewBagAsync(minPrice, maxPrice, trademark, sizeId, colorId, sortBy);

            var (products, totalProducts) = await _productService.GetCatalogProductsAsync(id, null, page, pageSize, minPrice, maxPrice, trademark, sizeId, colorId, sortBy);
            int totalPages = (int)System.Math.Ceiling((double)totalProducts / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.CategoryId = id;
            ViewBag.Action = "Category";

            return View("Product", products);
        }

        public async Task<IActionResult> Search(
            string search,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? trademark = null,
            string? sizeId = null,
            string? colorId = null,
            string? sortBy = null,
            int page = 1)
        {
            int pageSize = 12;
            await PopulateFilterViewBagAsync(minPrice, maxPrice, trademark, sizeId, colorId, sortBy);

            ViewData["GetProduct"] = search;

            var (products, totalProducts) = await _productService.GetCatalogProductsAsync(null, search, page, pageSize, minPrice, maxPrice, trademark, sizeId, colorId, sortBy);
            int totalPages = (int)System.Math.Ceiling((double)totalProducts / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Action = "Search";

            return View("Product", products);
        }

        public async Task<IActionResult> Details(string id)
        {
            var product = await _productService.GetProductDetailsAsync(id);

            if (product == null) return NotFound();

            var reviews = await _reviewService.GetReviewsByProductIdAsync(id);
            ViewBag.Reviews = reviews;
            ViewBag.AverageStars = reviews.Any() ? Math.Round(reviews.Average(r => r.RatingStars), 1) : 0;
            ViewBag.TotalReviews = reviews.Count;

            return View(product);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostReview(string productId, int ratingStars, string reviewComment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            if (ratingStars < 1 || ratingStars > 5)
            {
                TempData["ErrorMessage"] = "Số sao đánh giá phải từ 1 đến 5.";
                return RedirectToAction("Details", new { id = productId });
            }

            var result = await _reviewService.AddReviewAsync(userId, productId, ratingStars, reviewComment);
            if (result)
            {
                TempData["SuccessMessage"] = "Đăng đánh giá thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hoặc sản phẩm chưa có phân loại hàng để đánh giá.";
            }

            return RedirectToAction("Details", new { id = productId });
        }

        [HttpPost]
        public async Task<IActionResult> VoteReview(string reviewId)
        {
            if (string.IsNullOrEmpty(reviewId)) return BadRequest("Review ID is required.");
            var newVotes = await _reviewService.VoteHelpfulAsync(reviewId);
            return Json(new { success = newVotes > 0, votes = newVotes });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCart(string id, string? productVariantId, int quantity = 1)
        {
            // Shop hiện tại: AddCart/BuyNow có thể gửi kèm productVariantId.
            // Nếu không có productVariantId thì fallback chọn biến thể còn hàng đầu tiên.

            if (!string.IsNullOrWhiteSpace(productVariantId))
            {
                var pv = await _productService.GetActiveVariantByIdAsync(productVariantId);

                if (pv == null) return RedirectToAction("Index", "Home");

                await AddOrUpdateCartItemAsync(pv.Id, Math.Max(1, quantity));
                return RedirectToAction("Cart", "Cart");
            }

            // Fallback: Home/Index.cshtml truyền ProductId.
            var variant = await _productService.GetFirstActiveVariantByProductIdAsync(id);

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
                var pv = await _productService.GetActiveVariantByIdAsync(productVariantId);

                if (pv == null) return RedirectToAction("Index", "Home");

                return RedirectToAction("Checkout", "Order", new { buyNowVariantId = pv.Id, buyNowQty = Math.Max(1, quantity) });
            }

            var variant = await _productService.GetFirstActiveVariantByProductIdAsync(id);

            if (variant == null)
                return RedirectToAction("Index", "Home");

            return RedirectToAction("Checkout", "Order", new { buyNowVariantId = variant.Id, buyNowQty = Math.Max(1, quantity) });
        }

        [HttpGet]
        public async Task<IActionResult> BuyNow(string id)
        {
            var variant = await _productService.GetFirstActiveVariantByProductIdAsync(id);

            if (variant == null)
                return RedirectToAction("Index", "Home");

            return RedirectToAction("Checkout", "Order", new { buyNowVariantId = variant.Id, buyNowQty = 1 });
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

            await _cartService.AddOrUpdateCartItemAsync(cartKeyUserId, cartKeySessionId, productVariantId, delta);
        }
        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string term)
        {
            var products = await _productService.GetSearchSuggestionsAsync(term);
            var result = products.Select(p => {
                var defaultImage = p.ProductImages?.OrderBy(i => i.ImageOrder).FirstOrDefault();
                var imageUrl = HuflitShopCore.Helpers.ImageRouteHelper.Resolve(defaultImage?.PublicId);
                return new {
                    id = p.Id,
                    name = p.ProductName,
                    price = p.CurrentPrice.ToString("N0") + "₫",
                    imageUrl = imageUrl
                };
            }).ToList();

            return Json(result);
        }

        private async Task PopulateFilterViewBagAsync(
            decimal? minPrice, 
            decimal? maxPrice, 
            string? trademark, 
            string? sizeId, 
            string? colorId, 
            string? sortBy)
        {
            ViewBag.Categories = await _categoryService.GetCategoriesForCatalogAsync();
            ViewBag.Trademarks = await _productService.GetDistinctTrademarksAsync();
            ViewBag.Sizes = await _productService.GetAllSizesAsync();
            ViewBag.Colors = await _productService.GetAllColorsAsync();

            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Trademark = trademark;
            ViewBag.SizeId = sizeId;
            ViewBag.ColorId = colorId;
            ViewBag.SortBy = sortBy;
        }
    }
}

