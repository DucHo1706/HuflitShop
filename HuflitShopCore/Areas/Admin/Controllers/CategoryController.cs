using HuflitShopCore.DTOs;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly CategoryService _categoryService;

        public CategoryController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var rootCategories = categories.Where(c => string.IsNullOrEmpty(c.ParentId));
            ViewBag.ParentList = new SelectList(rootCategories, "Id", "CategoryName");
            return View(new CategoryDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryDTO dto)
        {
            if (ModelState.IsValid)
            {
                var (success, errorMsg) = await _categoryService.CreateCategoryAsync(dto);
                if (success)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, errorMsg);
            }
            var categories = await _categoryService.GetAllCategoriesAsync();
            var rootCategories = categories.Where(c => string.IsNullOrEmpty(c.ParentId));
            ViewBag.ParentList = new SelectList(rootCategories, "Id", "CategoryName", dto.ParentId);
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null) return NotFound();

            var categories = await _categoryService.GetAllCategoriesAsync();
            var rootCategories = categories.Where(c => string.IsNullOrEmpty(c.ParentId) && c.Id != id);
            ViewBag.ParentList = new SelectList(rootCategories, "Id", "CategoryName", category.ParentId);
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryDTO dto)
        {
            if (ModelState.IsValid)
            {
                var (success, errorMsg) = await _categoryService.UpdateCategoryAsync(dto);
                if (success)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, errorMsg);
            }
            var categories = await _categoryService.GetAllCategoriesAsync();
            var rootCategories = categories.Where(c => string.IsNullOrEmpty(c.ParentId) && c.Id != dto.Id);
            ViewBag.ParentList = new SelectList(rootCategories, "Id", "CategoryName", dto.ParentId);
            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            if (!result.Success)
                TempData["ErrorMessage"] = result.ErrorMsg;
            else
                TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}