using HuflitShopCore.DTOs;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ColorController : Controller
    {
        private readonly ColorService _colorService;

        public ColorController(ColorService colorService)
        {
            _colorService = colorService;
        }

        public async Task<IActionResult> Index()
        {
            var colors = await _colorService.GetAllColorsAsync();
            return View(colors);
        }

        public IActionResult Create()
        {
            return View(new ColorDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ColorDTO dto)
        {
            if (ModelState.IsValid)
            {
                await _colorService.CreateColorAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            return View(dto);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var color = await _colorService.GetColorByIdAsync(id);
            if (color == null) return NotFound();
            return View(color);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ColorDTO dto)
        {
            if (ModelState.IsValid)
            {
                await _colorService.UpdateColorAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _colorService.DeleteColorAsync(id);
            if (!result.Success)
                TempData["ErrorMessage"] = result.ErrorMsg;
            else
                TempData["SuccessMessage"] = "Xóa màu sắc thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}