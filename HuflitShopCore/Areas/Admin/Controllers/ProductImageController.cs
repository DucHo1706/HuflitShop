using HuflitShopCore.DTOs;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductImageController : Controller
    {
        private readonly ProductImageService _imageService;
        private readonly ProductService _productService;
        private readonly PhotoService _photoService;

        public ProductImageController(
            ProductImageService imageService, 
            ProductService productService, 
            PhotoService photoService)
        {
            _imageService = imageService;
            _productService = productService;
            _photoService = photoService;
        }

        public async Task<IActionResult> Index(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return RedirectToAction("Index", "Products");
            
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null) return NotFound();

            ViewBag.Product = product;
            var images = await _imageService.GetImagesByProductIdAsync(productId);
            return View(images);
        }

        [HttpGet]
        public async Task<IActionResult> Create(string productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null) return NotFound();

            var dto = new ProductImageDTO { ProductId = productId, ProductName = product.ProductName };
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductImageDTO dto)
        {
            if (ModelState.IsValid && dto.ImageFile != null)
            {
                var uploadResult = await _photoService.AddPhotoAsync(dto.ImageFile);
                if (uploadResult.Error != null)
                {
                    ModelState.AddModelError("", "Lỗi tải ảnh lên Cloudinary: " + uploadResult.Error.Message);
                    return View(dto);
                }

                dto.PublicId = uploadResult.PublicId; 
                dto.AssetVersion = uploadResult.Version;
                
                await _imageService.CreateImageAsync(dto);
                // Đổi luồng: Trở về trang Chi tiết sản phẩm sau khi tải ảnh thành công
                return RedirectToAction("Details", "Products", new { id = dto.ProductId });
            }

            ModelState.AddModelError("", "Vui lòng chọn hình ảnh.");
            var product = await _productService.GetProductByIdAsync(dto.ProductId);
            dto.ProductName = product?.ProductName ?? string.Empty;
            
            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id, string productId)
        {
            var image = await _imageService.GetImageByIdAsync(id);
            if (image != null)
            {
                // Xóa ảnh trên Cloudinary
                if (!string.IsNullOrEmpty(image.PublicId))
                {
                    await _photoService.DeletePhotoAsync(image.PublicId);
                }
                
                // Xóa dưới database
                await _imageService.DeleteImageAsync(id);
            }
            
            // Đổi luồng: Trở về trang Chi tiết sản phẩm sau khi xóa ảnh
            return RedirectToAction("Details", "Products", new { id = productId });
        }
    }
}