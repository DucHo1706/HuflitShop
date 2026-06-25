using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PromotionsController : Controller
    {
        private readonly PromotionService _promotionService;
        private readonly AppDbContext _context;

        public PromotionsController(PromotionService promotionService, AppDbContext context)
        {
            _promotionService = promotionService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var promotions = await _promotionService.GetAllPromotionsAsync();
            return View(promotions);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var products = await _context.Products.Where(p => !p.IsDeleted).OrderBy(p => p.ProductName).ToListAsync();
            ViewBag.Products = products;
            return View(new PromotionDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromotionDTO dto)
        {
            if (ModelState.IsValid)
            {
                await _promotionService.CreatePromotionAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            
            var products = await _context.Products.Where(p => !p.IsDeleted).OrderBy(p => p.ProductName).ToListAsync();
            ViewBag.Products = products;
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var promotion = await _promotionService.GetPromotionByIdAsync(id);
            if (promotion == null) return NotFound();

            var products = await _context.Products.Where(p => !p.IsDeleted).OrderBy(p => p.ProductName).ToListAsync();
            ViewBag.Products = products;

            return View(promotion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PromotionDTO dto)
        {
            if (ModelState.IsValid)
            {
                await _promotionService.UpdatePromotionAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            
            var products = await _context.Products.Where(p => !p.IsDeleted).OrderBy(p => p.ProductName).ToListAsync();
            ViewBag.Products = products;
            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _promotionService.DeletePromotionAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}