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
            ViewBag.ParentList = new SelectList(categories, "Id", "CategoryName");
            return View(new CategoryDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryDTO dto)
        {
            if (ModelState.IsValid)
            {
                await _categoryService.CreateCategoryAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.ParentList = new SelectList(categories, "Id", "CategoryName", dto.ParentId);
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null) return NotFound();

            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.ParentList = new SelectList(categories.Where(c => c.Id != id), "Id", "CategoryName", category.ParentId);
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryDTO dto)
        {
            if (ModelState.IsValid)
            {
                await _categoryService.UpdateCategoryAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.ParentList = new SelectList(categories.Where(c => c.Id != dto.Id), "Id", "CategoryName", dto.ParentId);
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