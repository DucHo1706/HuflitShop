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

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy 8 sản phẩm mới nhất, bao gồm ảnh Thumbnail và tổng số lượng tồn từ các biến thể
            var products = await _context.Products
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
                        .Select(i => "https://res.cloudinary.com/Tên_Cloud_Của_Bạn/image/upload/v" + i.AssetVersion + "/" + i.PublicId + ".jpg")
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