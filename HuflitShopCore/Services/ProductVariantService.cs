using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using HuflitShopCore.Helpers;
using HuflitShopCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class ProductVariantService
    {
        private readonly AppDbContext _context;

        public ProductVariantService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProductVariantDTO>> GetVariantsByProductIdAsync(string productId)
        {
            var variants = await _context.ProductVariants
                .Include(pv => pv.Size)
                .Include(pv => pv.Color)
                .Include(pv => pv.Product)
                .Where(pv => pv.ProductId == productId)
                .OrderBy(pv => pv.Size.SizeName)
                .ThenBy(pv => pv.Color.ColorName)
                .ToListAsync();

            return variants.Select(pv => new ProductVariantDTO
            {
                Id = pv.Id,
                ProductId = pv.ProductId,
                ProductName = pv.Product?.ProductName,
                SizeId = pv.SizeId,
                SizeName = pv.Size?.SizeName,
                ColorId = pv.ColorId,
                ColorName = pv.Color?.ColorName,
                ColorHexCode = pv.Color?.HexCode,
                StockQuantity = pv.StockQuantity,
                AdditionalPrice = pv.AdditionalPrice,
                IsActive = pv.IsActive
            }).ToList();
        }

        public async Task<List<ProductVariantDTO>> GetAllVariantsAsync()
        {
            var variants = await _context.ProductVariants
                .Include(pv => pv.Size)
                .Include(pv => pv.Color)
                .Include(pv => pv.Product)
                .ToListAsync();

            return variants.Select(pv => new ProductVariantDTO
            {
                Id = pv.Id,
                ProductId = pv.ProductId,
                ProductName = $"{pv.Product?.ProductName} - {pv.Color?.ColorName} - {pv.Size?.SizeName}",
                SizeId = pv.SizeId,
                ColorId = pv.ColorId,
                StockQuantity = pv.StockQuantity, 
                AdditionalPrice = pv.AdditionalPrice,
                IsActive = pv.IsActive
            }).ToList();
        }

        public async Task<List<ProductVariantDTO>> GetAllVariantsForStockReceiptAsync()
        {
            var variants = await _context.ProductVariants
                .Include(pv => pv.Product)
                .Include(pv => pv.Color)
                .Include(pv => pv.Size)
                .Where(pv => !pv.Product.IsDeleted) // Chỉ lọc các sản phẩm đã bị xóa mềm
                .OrderBy(pv => pv.Product.ProductName)
                .ThenBy(pv => pv.Color.ColorName)
                .ThenBy(pv => pv.Size.SizeName)
                .ToListAsync();

            return variants.Select(pv => new ProductVariantDTO
            {
                Id = pv.Id,
                // Định dạng tên để hiển thị trạng thái, giúp người dùng dễ nhận biết
                ProductName = $"{pv.Product.ProductName} - {pv.Color.ColorName} - {pv.Size.SizeName}" + (pv.IsActive ? "" : " (Ngừng bán)"),
                ProductId = pv.ProductId,
                SizeId = pv.SizeId,
                ColorId = pv.ColorId,
                StockQuantity = pv.StockQuantity,
                AdditionalPrice = pv.AdditionalPrice,
                IsActive = pv.IsActive
            }).ToList();
        }

        public async Task<ProductVariantDTO?> GetVariantByIdAsync(string id)
        {
            var pv = await _context.ProductVariants.FindAsync(id);
            if (pv == null) return null;

            return new ProductVariantDTO
            {
                Id = pv.Id,
                ProductId = pv.ProductId,
                SizeId = pv.SizeId,
                ColorId = pv.ColorId,
                StockQuantity = pv.StockQuantity,
                AdditionalPrice = pv.AdditionalPrice,
                IsActive = pv.IsActive
            };
        }

        public async Task<bool> CreateVariantAsync(ProductVariantDTO dto)
        {
            // Kiểm tra tránh trùng lặp màu và size trong cùng 1 sản phẩm
            var exists = await _context.ProductVariants
                .AnyAsync(pv => pv.ProductId == dto.ProductId && pv.SizeId == dto.SizeId && pv.ColorId == dto.ColorId);
            if (exists) return false;

            var variant = new ProductVariant
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = dto.ProductId,
                SizeId = dto.SizeId,
                ColorId = dto.ColorId,
                StockQuantity = dto.StockQuantity,
                AdditionalPrice = dto.AdditionalPrice,
                IsActive = true
            };

            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, int CreatedCount)> CreateBulkVariantsAsync(string productId, List<ProductVariantDTO> dtos)
        {
            // Chỉ xử lý những mục được người dùng tick chọn
            var variantsToCreate = dtos?.Where(d => d.IsSelected).ToList();
            if (variantsToCreate == null || !variantsToCreate.Any())
            {
                return (true, 0); // Không có gì để tạo, không phải lỗi
            }

            // Lấy các cặp Size-Color đã tồn tại của sản phẩm để tránh tạo trùng
            var existingCombinationsList = await _context.ProductVariants
                .Where(pv => pv.ProductId == productId)
                .Select(pv => new { pv.SizeId, pv.ColorId })
                .ToListAsync();
            var existingCombinations = existingCombinationsList.ToHashSet();

            int createdCount = 0;
            var newVariants = new List<ProductVariant>();

            foreach (var dto in variantsToCreate)
            {
                // Nếu cặp Size-Color này chưa tồn tại thì mới tạo
                if (!existingCombinations.Contains(new { dto.SizeId, dto.ColorId }))
                {
                    newVariants.Add(new ProductVariant
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProductId = productId,
                        SizeId = dto.SizeId,
                        ColorId = dto.ColorId,
                        AdditionalPrice = dto.AdditionalPrice,
                        StockQuantity = 0, // Tồn kho ban đầu luôn là 0
                        IsActive = true
                    });
                    createdCount++;
                }
            }

            if (createdCount > 0)
            {
                _context.ProductVariants.AddRange(newVariants);
                await _context.SaveChangesAsync();
            }

            return (true, createdCount);
        }

        public async Task<bool> UpdateVariantAsync(ProductVariantDTO dto)
        {
            var variant = await _context.ProductVariants.FindAsync(dto.Id);
            if (variant == null) return false;

            variant.AdditionalPrice = dto.AdditionalPrice;
            _context.ProductVariants.Update(variant);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> ToggleStatusAsync(string id)
        {
            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant == null) return "Không tìm thấy phân loại.";

            // Quy tắc nghiệp vụ: Không cho phép "Ngừng bán" nếu vẫn còn tồn kho.
            if (variant.IsActive && variant.StockQuantity > 0)
            {
                return "Không thể ngừng bán phân loại này vì vẫn còn tồn kho (> 0).";
            }

            variant.IsActive = !variant.IsActive; // Đảo ngược trạng thái
            await _context.SaveChangesAsync();
            return string.Empty; // Trả về chuỗi rỗng nếu thành công
        }

        public async Task<(bool Success, string ErrorMsg)> DeleteVariantAsync(string id)
        {
            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant == null) return (false, "Không tìm thấy phân loại.");

            if (variant.StockQuantity > 0)
            {
                return (false, "Không thể xóa phân loại này vì số lượng tồn kho đang lớn hơn 0!");
            }

            _context.ProductVariants.Remove(variant);
            return await DbErrorHandler.ExecuteAsync(async () => await _context.SaveChangesAsync());
        }
    }
}