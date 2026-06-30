using HuflitShopCore.DTOs;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly ProductVariantService _variantService;
        private readonly SizeService _sizeService;
        private readonly ColorService _colorService;
        private readonly ProductImageService _imageService;

        public ProductsController(ProductService productService, CategoryService categoryService, ProductVariantService variantService, SizeService sizeService, ColorService colorService, ProductImageService imageService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _variantService = variantService;
            _sizeService = sizeService;
            _colorService = colorService;
            _imageService = imageService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllProductsAsync();
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "CategoryName");
            return View(new ProductDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDTO dto)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _productService.CreateProductAsync(dto, userId);
                return RedirectToAction(nameof(Index));
            }
            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "CategoryName", dto.CategoryId);
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "CategoryName", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductDTO dto)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _productService.UpdateProductAsync(dto, userId);
                return RedirectToAction(nameof(Index));
            }
            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "CategoryName", dto.CategoryId);
            return View(dto);
        }

        public async Task<IActionResult> Details(string id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            var category = await _categoryService.GetCategoryByIdAsync(product.CategoryId);
            product.CategoryName = category?.CategoryName;
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            string errorMessage = await _productService.ToggleProductStatusAsync(id);
            if (string.IsNullOrEmpty(errorMessage))
            {
                TempData["SuccessMessage"] = "Cập nhật trạng thái sản phẩm thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            return RedirectToAction(nameof(Index));
        }

        #region Partial Views for AJAX Loading

        [HttpGet]
        public async Task<IActionResult> VariantsPartial(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return BadRequest();

            ViewBag.ProductId = productId;
            ViewBag.Variants = await _variantService.GetVariantsByProductIdAsync(productId);
            
            var sizes = await _sizeService.GetAllSizesAsync();
            var colors = await _colorService.GetAllColorsAsync();
            ViewBag.SizeList = new SelectList(sizes, "Id", "SizeName");
            ViewBag.ColorList = new SelectList(colors, "Id", "ColorName");

            return PartialView("_ProductVariantsPartial");
        }

        [HttpGet]
        public async Task<IActionResult> ImagesPartial(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return BadRequest();
            ViewBag.ProductId = productId;
            ViewBag.Images = await _imageService.GetImagesByProductIdAsync(productId);
            return PartialView("_ProductImagesPartial");
        }

        [HttpGet]
        public async Task<IActionResult> PriceHistoryPartial(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return BadRequest();
            ViewBag.ProductId = productId;
            var history = await _productService.GetProductPriceHistoryAsync(productId);
            return PartialView("_ProductPriceHistoryPartial", history);
        }

        #endregion
    }
}
