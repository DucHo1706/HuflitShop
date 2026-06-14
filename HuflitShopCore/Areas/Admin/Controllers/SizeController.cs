using HuflitShopCore.DTOs;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SizeController : Controller
    {
        private readonly SizeService _sizeService;

        public SizeController(SizeService sizeService)
        {
            _sizeService = sizeService;
        }

        public async Task<IActionResult> Index()
        {
            var sizes = await _sizeService.GetAllSizesAsync();
            return View(sizes);
        }

        public IActionResult Create()
        {
            return View(new SizeDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SizeDTO dto)
        {
            if (ModelState.IsValid)
            {
                await _sizeService.CreateSizeAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            return View(dto);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var size = await _sizeService.GetSizeByIdAsync(id);
            if (size == null) return NotFound();
            return View(size);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SizeDTO dto)
        {
            if (ModelState.IsValid)
            {
                await _sizeService.UpdateSizeAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _sizeService.DeleteSizeAsync(id);
            if (!result.Success)
                TempData["ErrorMessage"] = result.ErrorMsg;
            else
                TempData["SuccessMessage"] = "Xóa kích thước thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}