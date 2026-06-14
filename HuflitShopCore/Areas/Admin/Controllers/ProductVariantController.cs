using HuflitShopCore.DTOs;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductVariantController : Controller
    {
        private readonly ProductVariantService _variantService;
        private readonly ProductService _productService;
        private readonly SizeService _sizeService;
        private readonly ColorService _colorService;

        public ProductVariantController(
            ProductVariantService variantService, 
            ProductService productService, 
            SizeService sizeService, 
            ColorService colorService)
        {
            _variantService = variantService;
            _productService = productService;
            _sizeService = sizeService;
            _colorService = colorService;
        }

        // Xem danh sách biến thể của 1 sản phẩm cụ thể
        public async Task<IActionResult> Index(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return RedirectToAction("Index", "Products");
            
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null) return NotFound();

            ViewBag.Product = product;
            var variants = await _variantService.GetVariantsByProductIdAsync(productId);
            
            return View(variants);
        }

        [HttpGet]
        public async Task<IActionResult> Create(string productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null) return NotFound();

            await LoadDropdownDataAsync();
            
            var dto = new ProductVariantDTO { ProductId = productId };
            ViewBag.ProductName = product.ProductName;
            
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductVariantDTO dto)
        {
            if (ModelState.IsValid)
            {
                var success = await _variantService.CreateVariantAsync(dto);
                if (success) 
                    // Đổi luồng: Về lại trang Chi tiết sản phẩm sau khi tạo
                    return RedirectToAction("Details", "Products", new { id = dto.ProductId });
                
                ModelState.AddModelError("", "Biến thể với Kích thước và Màu sắc này đã tồn tại trong sản phẩm.");
            }

            var product = await _productService.GetProductByIdAsync(dto.ProductId);
            ViewBag.ProductName = product?.ProductName;
            await LoadDropdownDataAsync(dto.SizeId, dto.ColorId);
            
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBulk(string productId, List<ProductVariantDTO> variants)
        {
            if (string.IsNullOrEmpty(productId))
            {
                TempData["ErrorMessage"] = "ID sản phẩm không hợp lệ.";
                return RedirectToAction("Index", "Products");
            }

            var (success, createdCount) = await _variantService.CreateBulkVariantsAsync(productId, variants);

            if (success)
            {
                if (createdCount > 0)
                {
                    TempData["SuccessMessage"] = $"Đã tạo thành công {createdCount} phân loại mới.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Không có phân loại mới nào được tạo (có thể chúng đã tồn tại).";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình tạo hàng loạt phân loại.";
            }

            return RedirectToAction("Details", "Products", new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductVariantDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Id))
            {
                TempData["ErrorMessage"] = "ID của phân loại không hợp lệ.";
                return RedirectToAction("Details", "Products", new { id = dto.ProductId });
            }

            var success = await _variantService.UpdateVariantAsync(dto);
            if (success)
            {
                TempData["SuccessMessage"] = "Cập nhật giá cộng thêm thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Cập nhật thất bại, không tìm thấy phân loại.";
            }
            return RedirectToAction("Details", "Products", new { id = dto.ProductId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id, string productId)
        {
            string errorMessage = await _variantService.ToggleStatusAsync(id);
            if (string.IsNullOrEmpty(errorMessage))
            {
                TempData["SuccessMessage"] = "Thay đổi trạng thái kinh doanh thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id, string productId)
        {
            var result = await _variantService.DeleteVariantAsync(id);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMsg; // Nhận lỗi động từ Service
            }
            else
            {
                TempData["SuccessMessage"] = "Xóa phân loại thành công!";
            }
            // Đổi luồng: Về lại trang Chi tiết sản phẩm sau khi xóa
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        // Helper: Tải dữ liệu cho Dropdown
        private async Task LoadDropdownDataAsync(string? selectedSize = null, string? selectedColor = null)
        {
            var sizes = await _sizeService.GetAllSizesAsync();
            var colors = await _colorService.GetAllColorsAsync();

            ViewBag.SizeList = new SelectList(sizes, "Id", "SizeName", selectedSize);
            ViewBag.ColorList = new SelectList(colors, "Id", "ColorName", selectedColor);
        }
    }
}