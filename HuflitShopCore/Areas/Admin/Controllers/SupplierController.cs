using HuflitShopCore.DTOs;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SupplierController : Controller
    {
        private readonly SupplierService _supplierService;

        public SupplierController(SupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        public async Task<IActionResult> Index()
        {
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            return View(suppliers);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new SupplierDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierDTO dto)
        {
            if (ModelState.IsValid)
            {
                await _supplierService.CreateSupplierAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SupplierDTO dto)
        {
            if (ModelState.IsValid)
            {
                await _supplierService.UpdateSupplierAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            return View(dto);
        }
        
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            await _supplierService.ToggleStatusAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}