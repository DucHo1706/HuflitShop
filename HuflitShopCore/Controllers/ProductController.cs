using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HuflitShopCore.Data;
using HuflitShopCore.Models;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HuflitShopCore.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly CartService _cartService;
        private readonly ReviewService _reviewService;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public ProductController(ProductService productService, CategoryService categoryService, CartService cartService, ReviewService reviewService, AppDbContext context, IConfiguration configuration)
        {
            _productService = productService;
            _categoryService = categoryService;
            _cartService = cartService;
            _reviewService = reviewService;
            _context = context;
            _configuration = configuration;
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

            var now = System.DateTime.Now;
            ViewBag.ActiveAutoPromos = await _context.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive && p.IsAutoApply && p.StartDate <= now && p.EndDate >= now && !string.IsNullOrEmpty(p.ApplicableProductId))
                .ToListAsync();

            // Ghi nhận lượt xem sản phẩm ngay lập tức cho người dùng đã đăng nhập (mặc định 1s)
            var isAuth = User.Identity != null && User.Identity.IsAuthenticated;
            if (isAuth)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                    var userAgentStr = Request.Headers["User-Agent"].ToString() ?? "Unknown";
                    var encodedUserAgent = $"{userAgentStr} | Duration: 1s";

                    var log = new ProductViewsLog
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProductId = id,
                        UserId = userId,
                        IpAddress = ip,
                        UserAgent = encodedUserAgent,
                        ViewedAt = DateTime.Now
                    };

                    _context.ProductViewsLogs.Add(log);
                    await _context.SaveChangesAsync();
                }
            }

            // Lấy tối đa 8 sản phẩm liên quan (cùng danh mục, loại trừ chính nó)
            var relatedProducts = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            // Nếu số lượng sản phẩm cùng danh mục ít hơn 8, bù thêm các sản phẩm mới nhất từ các danh mục khác
            if (relatedProducts.Count < 8)
            {
                var existingIds = relatedProducts.Select(rp => rp.Id).Append(id).ToList();
                var additionalProducts = await _context.Products
                    .Include(p => p.ProductImages)
                    .Where(p => !p.IsDeleted && !existingIds.Contains(p.Id))
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8 - relatedProducts.Count)
                    .ToListAsync();
                relatedProducts.AddRange(additionalProducts);
            }
            ViewBag.RelatedProducts = relatedProducts;

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
            try
            {
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
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Details", new { id = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(string id, string? productVariantId, int quantity = 1)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(productVariantId))
                {
                    var pv = await _productService.GetActiveVariantByIdAsync(productVariantId);

                    if (pv == null) return RedirectToAction("Index", "Home");

                    if (pv.StockQuantity < quantity)
                    {
                        throw new System.InvalidOperationException($"Sản phẩm chỉ còn {pv.StockQuantity} sản phẩm trong kho.");
                    }

                    return RedirectToAction("Checkout", "Order", new { buyNowVariantId = pv.Id, buyNowQty = Math.Max(1, quantity) });
                }

                var variant = await _productService.GetFirstActiveVariantByProductIdAsync(id);

                if (variant == null)
                    return RedirectToAction("Index", "Home");

                if (variant.StockQuantity < quantity)
                {
                    throw new System.InvalidOperationException($"Sản phẩm chỉ còn {variant.StockQuantity} sản phẩm trong kho.");
                }

                return RedirectToAction("Checkout", "Order", new { buyNowVariantId = variant.Id, buyNowQty = Math.Max(1, quantity) });
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Details", new { id = id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> BuyNow(string id)
        {
            try
            {
                var variant = await _productService.GetFirstActiveVariantByProductIdAsync(id);

                if (variant == null)
                    return RedirectToAction("Index", "Home");

                return RedirectToAction("Checkout", "Order", new { buyNowVariantId = variant.Id, buyNowQty = 1 });
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Details", new { id = id });
            }
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

            var now = System.DateTime.Now;
            ViewBag.ActiveAutoPromos = await _context.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive && p.IsAutoApply && p.StartDate <= now && p.EndDate >= now && !string.IsNullOrEmpty(p.ApplicableProductId))
                .ToListAsync();
        }

        [HttpPost]
        public async Task<IActionResult> TrackBehavior(string productId, int durationSeconds)
        {
            var isAuth = User.Identity != null && User.Identity.IsAuthenticated;
            var userId = isAuth ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Không xác thực" });
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var userAgentStr = Request.Headers["User-Agent"].ToString() ?? "Unknown";
            
            // Mã hóa thời gian xem vào cột UserAgent: "UserAgent | Duration: Xs"
            var encodedUserAgent = $"{userAgentStr} | Duration: {durationSeconds}s";

            var log = new ProductViewsLog
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = productId,
                UserId = userId,
                IpAddress = ip,
                UserAgent = encodedUserAgent,
                ViewedAt = DateTime.Now
            };

            _context.ProductViewsLogs.Add(log);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonalizedRecommendations()
        {
            var isAuth = User.Identity != null && User.Identity.IsAuthenticated;
            var userId = isAuth ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

            var cloudName = _configuration["Cloudinary:CloudName"] ?? _configuration["CloudinarySettings:CloudName"] ?? "dsamboqwp";

            if (string.IsNullOrEmpty(userId))
            {
                // Khách vãng lai: Gợi ý các sản phẩm mới nhất (8 sản phẩm)
                var guestProducts = await _context.Products
                    .Include(p => p.ProductImages)
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync();
                
                var guestResult = guestProducts.Select(p => {
                    var firstImg = p.ProductImages?.OrderBy(img => img.ImageOrder).FirstOrDefault();
                    string? imgUrl = "/Client/img/default-product.jpg";
                    if (firstImg != null)
                    {
                        imgUrl = (firstImg.PublicId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                                  firstImg.PublicId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                            ? firstImg.PublicId
                            : (string.IsNullOrEmpty(firstImg.AssetVersion)
                                ? firstImg.PublicId
                                : "https://res.cloudinary.com/" + cloudName + "/image/upload/v" + firstImg.AssetVersion + "/" + firstImg.PublicId + ".jpg");
                    }
                    return new
                    {
                        id = p.Id,
                        name = p.ProductName,
                        price = p.CurrentPrice,
                        imageUrl = HuflitShopCore.Helpers.ImageRouteHelper.Resolve(imgUrl)
                    };
                });
                return Json(guestResult);
            }

            // Lấy toàn bộ log xem sản phẩm của User này kèm Category
            var logs = await _context.ProductViewsLogs
                .Include(l => l.Product)
                .Where(l => l.UserId == userId && l.Product != null && !l.Product.IsDeleted)
                .ToListAsync();

            List<Product> recommendedProducts = new List<Product>();

            if (logs.Any())
            {
                // Tính tổng thời lượng xem theo từng CategoryId
                var categoryDurations = new Dictionary<string, int>();
                foreach (var log in logs)
                {
                    var catId = log.Product.CategoryId;
                    int duration = 5; // Mặc định 5s
                    if (!string.IsNullOrEmpty(log.UserAgent) && log.UserAgent.Contains("Duration:"))
                    {
                        var parts = log.UserAgent.Split('|');
                        if (parts.Length > 1)
                        {
                            var durStr = parts[1].Replace("Duration:", "").Replace("s", "").Trim();
                            int.TryParse(durStr, out duration);
                        }
                    }

                    if (categoryDurations.ContainsKey(catId))
                    {
                        categoryDurations[catId] += duration;
                    }
                    else
                    {
                        categoryDurations[catId] = duration;
                    }
                }

                // Sắp xếp các danh mục theo tổng thời lượng xem giảm dần
                var sortedCategories = categoryDurations.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();

                // Lấy sản phẩm từ các danh mục này theo thứ tự ưu tiên
                foreach (var catId in sortedCategories)
                {
                    if (recommendedProducts.Count >= 8) break;

                    var existingIds = recommendedProducts.Select(rp => rp.Id).ToList();
                    var catProducts = await _context.Products
                        .Include(p => p.ProductImages)
                        .Where(p => p.CategoryId == catId && !p.IsDeleted && !existingIds.Contains(p.Id))
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(8 - recommendedProducts.Count)
                        .ToListAsync();

                    recommendedProducts.AddRange(catProducts);
                }
            }

            // Nếu không có log hoặc số sản phẩm gợi ý ít hơn 8, bù thêm các sản phẩm mới nhất từ các danh mục khác
            if (recommendedProducts.Count < 8)
            {
                var existingIds = recommendedProducts.Select(rp => rp.Id).ToList();
                var additionalProducts = await _context.Products
                    .Include(p => p.ProductImages)
                    .Where(p => !p.IsDeleted && !existingIds.Contains(p.Id))
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8 - recommendedProducts.Count)
                    .ToListAsync();
                recommendedProducts.AddRange(additionalProducts);
            }

            var result = recommendedProducts.Select(p => {
                var firstImg = p.ProductImages?.OrderBy(img => img.ImageOrder).FirstOrDefault();
                string? imgUrl = "/Client/img/default-product.jpg";
                if (firstImg != null)
                {
                    imgUrl = (firstImg.PublicId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                              firstImg.PublicId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        ? firstImg.PublicId
                        : (string.IsNullOrEmpty(firstImg.AssetVersion)
                            ? firstImg.PublicId
                            : "https://res.cloudinary.com/" + cloudName + "/image/upload/v" + firstImg.AssetVersion + "/" + firstImg.PublicId + ".jpg");
                }

                return new
                {
                    id = p.Id,
                    name = p.ProductName,
                    price = p.CurrentPrice,
                    imageUrl = HuflitShopCore.Helpers.ImageRouteHelper.Resolve(imgUrl)
                };
            });

            return Json(result);
        }
    }
}

