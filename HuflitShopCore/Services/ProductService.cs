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
            return await _context.Products
                .Include(p => p.Category)
                .Select(p => new ProductDTO
                {
                    Id = p.Id,
                    ProductName = p.ProductName,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.CategoryName : string.Empty,
                    CurrentPrice = p.CurrentPrice,
                    ManufactureYear = p.ManufactureYear,
                    Origin = p.Origin,
                    Trademark = p.Trademark,
                    Description = p.Description,
                    IsDeleted = p.IsDeleted
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

        public async Task<(List<Product> Products, int TotalProducts)> GetCatalogProductsAsync(
            string? categoryId, 
            string? search, 
            int page, 
            int pageSize,
            decimal? minPrice = null, 
            decimal? maxPrice = null, 
            string? trademark = null, 
            string? sizeId = null, 
            string? colorId = null, 
            string? sortBy = null)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .Where(p => !p.IsDeleted)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(categoryId))
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(search) || 
                                         (p.Trademark != null && p.Trademark.ToLower().Contains(search)));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.CurrentPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.CurrentPrice <= maxPrice.Value);
            }

            if (!string.IsNullOrEmpty(trademark))
            {
                query = query.Where(p => p.Trademark == trademark);
            }

            if (!string.IsNullOrEmpty(sizeId))
            {
                query = query.Where(p => p.ProductVariants.Any(pv => pv.SizeId == sizeId && pv.IsActive && pv.StockQuantity > 0));
            }

            if (!string.IsNullOrEmpty(colorId))
            {
                query = query.Where(p => p.ProductVariants.Any(pv => pv.ColorId == colorId && pv.IsActive && pv.StockQuantity > 0));
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "price_asc":
                        query = query.OrderBy(p => p.CurrentPrice);
                        break;
                    case "price_desc":
                        query = query.OrderByDescending(p => p.CurrentPrice);
                        break;
                    case "newest":
                        query = query.OrderByDescending(p => p.CreatedAt);
                        break;
                    case "popular":
                        query = query.OrderByDescending(p => p.ViewCount);
                        break;
                    default:
                        query = query.OrderByDescending(p => p.CreatedAt);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(p => p.CreatedAt);
            }

            int totalProducts = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (products, totalProducts);
        }

        public async Task<Product?> GetProductDetailsAsync(string id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants).ThenInclude(pv => pv.Size)
                .Include(p => p.ProductVariants).ThenInclude(pv => pv.Color)
                .Include(p => p.ProductImages)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<ProductVariant?> GetActiveVariantByIdAsync(string id)
        {
            return await _context.ProductVariants
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive && x.StockQuantity > 0);
        }

        public async Task<ProductVariant?> GetFirstActiveVariantByProductIdAsync(string productId)
        {
            return await _context.ProductVariants
                .Where(pv => pv.ProductId == productId && pv.IsActive && pv.StockQuantity > 0)
                .OrderBy(pv => pv.StockQuantity)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Product>> GetSuggestedProductsAsync(int limit = 4)
        {
            return await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.ViewCount)
                .Take(limit)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctTrademarksAsync()
        {
            return await _context.Products
                .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.Trademark))
                .Select(p => p.Trademark!)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<Size>> GetAllSizesAsync()
        {
            return await _context.Sizes.ToListAsync();
        }

        public async Task<List<Color>> GetAllColorsAsync()
        {
            return await _context.Colors.ToListAsync();
        }

        public async Task<List<Product>> GetSearchSuggestionsAsync(string term, int limit = 6)
        {
            if (string.IsNullOrWhiteSpace(term)) return new List<Product>();

            term = term.ToLower();
            return await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => !p.IsDeleted && p.ProductName.ToLower().Contains(term))
                .OrderByDescending(p => p.ViewCount)
                .Take(limit)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}