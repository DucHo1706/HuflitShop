using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public HomeController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var cloudName = _configuration["Cloudinary:CloudName"] ?? _configuration["CloudinarySettings:CloudName"] ?? "dsamboqwp";

            var now = System.DateTime.Now;
            ViewBag.ActiveAutoPromos = await _context.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive && p.IsAutoApply && p.StartDate <= now && p.EndDate >= now && !string.IsNullOrEmpty(p.ApplicableProductId))
                .ToListAsync();

            // Lấy 8 sản phẩm mới nhất, bao gồm ảnh Thumbnail và tổng số lượng tồn từ các biến thể
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsDeleted == false)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .Select(p => new ProductListDTO
                {
                    Id = p.Id,
                    Name = p.ProductName,
                    Price = p.CurrentPrice,
                    ImageDefault = _context.ProductImages
                        .Where(i => i.ProductId == p.Id)
                        .OrderBy(i => i.ImageOrder)
                        .Select(i => i.PublicId.StartsWith("http://") || i.PublicId.StartsWith("https://")
                            ? i.PublicId
                            : ((i.AssetVersion == null || i.AssetVersion == "")
                                ? i.PublicId
                                : "https://res.cloudinary.com/" + cloudName + "/image/upload/v" + i.AssetVersion + "/" + i.PublicId + ".jpg"))
                        .FirstOrDefault() ?? "",
                    Count = _context.ProductVariants
                        .Where(pv => pv.ProductId == p.Id)
                        .Sum(pv => pv.StockQuantity)
                })
                .ToListAsync();

            return View(products);
        }
    }
}