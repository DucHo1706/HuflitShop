using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using HuflitShopCore.Models;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class ProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        // Phương thức hiện có (hoặc tương tự) mà GetAllProductsAsync() sẽ gọi
        public async Task<List<ProductDTO>> GetAllProductsAsync()
        {
            // Đây là một placeholder. Triển khai thực tế sẽ ánh xạ Product sang ProductDTO
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted)
                .Select(p => new ProductDTO
                {
                    Id = p.Id,
                    ProductName = p.ProductName,
                    CategoryName = p.Category.CategoryName,
                    Trademark = p.Trademark
                    // ... các thuộc tính khác
                })
                .ToListAsync();
        }

        public async Task<ProductDTO?> GetProductByIdAsync(string id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return null;

            return new ProductDTO
            {
                Id = product.Id,
                ProductName = product.ProductName,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.CategoryName,
                CurrentPrice = product.CurrentPrice,
                ManufactureYear = product.ManufactureYear,
                Trademark = product.Trademark,
                Origin = product.Origin,
                Description = product.Description,
                IsDeleted = product.IsDeleted
            };
        }

        public async Task<bool> CreateProductAsync(ProductDTO dto)
        {
            var product = new Product
            {
                Id = Guid.NewGuid().ToString(),
                ProductName = dto.ProductName,
                CategoryId = dto.CategoryId,
                CurrentPrice = dto.CurrentPrice,
                ManufactureYear = dto.ManufactureYear,
                Trademark = dto.Trademark,
                Origin = dto.Origin,
                Description = dto.Description,
                IsDeleted = false // Mặc định sản phẩm mới không bị xóa
            };

            _context.Products.Add(product);
            _context.ProductPriceHistories.Add(new ProductPriceHistory
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = product.Id,
                OldPrice = 0,
                NewPrice = dto.CurrentPrice,
                EffectiveDate = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateProductAsync(ProductDTO dto)
        {
            var product = await _context.Products.FindAsync(dto.Id);
            if (product == null) return false;

            // Lưu lịch sử nếu giá thay đổi
            if (product.CurrentPrice != dto.CurrentPrice)
            {
                _context.ProductPriceHistories.Add(new ProductPriceHistory
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductId = product.Id,
                    OldPrice = product.CurrentPrice,
                    NewPrice = dto.CurrentPrice,
                    EffectiveDate = DateTime.Now
                });
                product.CurrentPrice = dto.CurrentPrice;
            }

            product.ProductName = dto.ProductName;
            product.CategoryId = dto.CategoryId;
            product.ManufactureYear = dto.ManufactureYear;
            product.Trademark = dto.Trademark;
            product.Origin = dto.Origin;
            product.Description = dto.Description;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> ToggleProductStatusAsync(string id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return "Không tìm thấy sản phẩm.";

            product.IsDeleted = !product.IsDeleted;
            await _context.SaveChangesAsync();
            return string.Empty;
        }

        public async Task<List<ProductSelect2DTO>> SearchProductsForSelect2Async(string searchTerm, int page, int pageSize)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(searchTerm) ||
                    p.Category.CategoryName.ToLower().Contains(searchTerm) ||
                    (p.Trademark != null && p.Trademark.ToLower().Contains(searchTerm)));
            }

            var products = await query
                .OrderBy(p => p.ProductName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductSelect2DTO
                {
                    Id = p.Id,
                    DisplayName = $"[{p.Category.CategoryName}] {p.ProductName} ({p.Trademark})"
                })
                .ToListAsync();

            return products;
        }

        public async Task<int> CountSearchProductsForSelect2Async(string searchTerm)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(searchTerm) ||
                    p.Category.CategoryName.ToLower().Contains(searchTerm) ||
                    (p.Trademark != null && p.Trademark.ToLower().Contains(searchTerm)));
            }

            return await query.CountAsync();
        }

        public async Task<List<ProductSelect2DTO>> GetLowStockProductsAsync(int threshold = 10)
        {
            // Lấy các sản phẩm có ít nhất 1 biến thể có tồn kho dưới ngưỡng (threshold)
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                .Where(p => !p.IsDeleted && p.ProductVariants.Any(v => v.StockQuantity <= threshold))
                .OrderBy(p => p.ProductName)
                .Take(12) // Chỉ lấy 12 sản phẩm gợi ý để tránh rối mắt
                .Select(p => new ProductSelect2DTO
                {
                    Id = p.Id,
                    DisplayName = p.ProductName
                })
                .ToListAsync();

            return products;
        }
    }
}