using HuflitShopCore.DTOs;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StockReceiptController : Controller
    {
        private readonly StockReceiptService _stockReceiptService;
        private readonly SupplierService _supplierService;
        private readonly ProductVariantService _productVariantService;
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;

        public StockReceiptController(
            StockReceiptService stockReceiptService, 
            SupplierService supplierService,
            ProductVariantService productVariantService,
            ProductService productService,
            CategoryService categoryService)
        {
            _stockReceiptService = stockReceiptService;
            _supplierService = supplierService;
            _productVariantService = productVariantService;
            _productService = productService;
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index()
        {
            var receipts = await _stockReceiptService.GetAllReceiptsAsync();
            return View(receipts);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Lấy danh sách nhà cung cấp và sản phẩm để hiển thị trên form
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            ViewBag.SupplierList = new SelectList(suppliers.Where(s => s.IsActive), "Id", "Name");

            // Lấy dữ liệu cho phần "Cá nhân hóa"
            ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.LowStockProducts = await _productService.GetLowStockProductsAsync(10); // Ngưỡng là 10

            // Sử dụng View của bước trước, vì nó đã có đủ các trường cần thiết
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockReceiptDTO dto)
        {
            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(dto.SupplierId))
            {
                ModelState.AddModelError("SupplierId", "Vui lòng chọn nhà cung cấp.");
            }

            if (ModelState.IsValid)
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var (success, receiptId, errorMessage) = await _stockReceiptService.CreateReceiptWithBulkDetailsAsync(dto, userId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Tạo phiếu nhập kho thành công. Bạn có thể thêm các sản phẩm khác.";
                    return RedirectToAction(nameof(Details), new { id = receiptId });
                }
                else
                {
                    ModelState.AddModelError("", errorMessage);
                }
            }

            // Nếu có lỗi, tải lại danh sách và hiển thị lại form
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            ViewBag.SupplierList = new SelectList(suppliers.Where(s => s.IsActive), "Id", "Name", dto.SupplierId);
            return View(dto);
        }

        [HttpGet]
        public async Task<JsonResult> GetVariantsByProduct(string productId)
        {
            var variants = await _productVariantService.GetVariantsByProductIdAsync(productId);
            var result = variants.Select(v => new
            {
                id = v.Id,
                color = v.ColorName,
                size = v.SizeName,
                stock = v.StockQuantity,
                isActive = v.IsActive
            });
            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> SearchProducts(string term, int page)
        {
            int pageSize = 10; // Số lượng sản phẩm mỗi trang
            var products = await _productService.SearchProductsForSelect2Async(term, page, pageSize);
            
            // Select2 yêu cầu định dạng { id: "...", text: "..." }
            // ProductSelect2DTO đã có Id và DisplayName, phù hợp với yêu cầu của Select2
            var formattedProducts = products.Select(p => new { id = p.Id, text = p.DisplayName });

            var totalCount = await _productService.CountSearchProductsForSelect2Async(term);
            var more = (page * pageSize) < totalCount;

            return Json(new { results = formattedProducts, pagination = new { more = more } });
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var receipt = await _stockReceiptService.GetReceiptByIdAsync(id);
            if (receipt == null) return NotFound();

            receipt.Details = await _stockReceiptService.GetDetailsByReceiptIdAsync(id);
            return View(receipt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBulkDetails(List<StockReceiptDetailDTO> details, string receiptId)
        {
            if (details != null && details.Any())
            {
                var success = await _stockReceiptService.AddBulkReceiptDetailsAsync(details, receiptId);
                if (success)
                {
                    TempData["SuccessMessage"] = "Thêm sản phẩm vào phiếu nhập thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm sản phẩm.";
                }
            }
            return RedirectToAction(nameof(Details), new { id = receiptId });
        }
    }
}