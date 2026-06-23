using HuflitShopCore.Data;
using HuflitShopCore.Models;
using HuflitShopCore.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using HuflitShopCore.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class CategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryDTO>> GetAllCategoriesAsync()
        {
            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            
            return categories.Select(c => new CategoryDTO
            {
                Id = c.Id,
                CategoryName = c.CategoryName,
                ParentId = c.ParentId,
                ParentName = categories.FirstOrDefault(p => p.Id == c.ParentId)?.CategoryName
            }).ToList();
        }

        public async Task<CategoryDTO?> GetCategoryByIdAsync(string id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return null;

            return new CategoryDTO
            {
                Id = category.Id,
                CategoryName = category.CategoryName,
                ParentId = category.ParentId
            };
        }

        public async Task<(bool Success, string ErrorMsg)> CreateCategoryAsync(CategoryDTO dto)
        {
            if (!string.IsNullOrEmpty(dto.ParentId))
            {
                var parent = await _context.Categories.FindAsync(dto.ParentId);
                if (parent == null)
                {
                    return (false, "Danh mục cha không tồn tại.");
                }
                if (!string.IsNullOrEmpty(parent.ParentId))
                {
                    return (false, "Danh mục cha được chọn phải là danh mục gốc (không thuộc danh mục khác).");
                }
            }

            var category = new Category
            {
                Id = Guid.NewGuid().ToString(),
                CategoryName = dto.CategoryName,
                ParentId = string.IsNullOrEmpty(dto.ParentId) ? null : dto.ParentId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Success, string ErrorMsg)> UpdateCategoryAsync(CategoryDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Id)) return (false, "ID danh mục không hợp lệ.");

            var category = await _context.Categories.FindAsync(dto.Id);
            if (category == null) return (false, "Không tìm thấy danh mục.");

            if (!string.IsNullOrEmpty(dto.ParentId))
            {
                if (dto.ParentId == dto.Id)
                {
                    return (false, "Một danh mục không thể chọn chính nó làm danh mục cha.");
                }

                var parent = await _context.Categories.FindAsync(dto.ParentId);
                if (parent == null)
                {
                    return (false, "Danh mục cha không tồn tại.");
                }
                if (!string.IsNullOrEmpty(parent.ParentId))
                {
                    return (false, "Danh mục cha được chọn phải là danh mục gốc (không thuộc danh mục khác).");
                }

                // Nếu danh mục hiện tại đang có danh mục con, không được gán danh mục cha (để tránh sâu hơn 2 cấp)
                var hasChildren = await _context.Categories.AnyAsync(c => c.ParentId == dto.Id);
                if (hasChildren)
                {
                    return (false, "Danh mục này đang có danh mục con, không thể gán danh mục cha cho nó.");
                }
            }

            category.CategoryName = dto.CategoryName;
            category.ParentId = string.IsNullOrEmpty(dto.ParentId) ? null : dto.ParentId;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Success, string ErrorMsg)> DeleteCategoryAsync(string id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return (false, "Không tìm thấy danh mục.");

            var hasChildren = await _context.Categories.AnyAsync(c => c.ParentId == id);
            if (hasChildren) return (false, "Không thể xóa vì danh mục này đang có danh mục con.");

            _context.Categories.Remove(category);
            return await DbErrorHandler.ExecuteAsync(async () => {
                await _context.SaveChangesAsync();
            });
        }

        public async Task<List<Category>> GetCategoriesForCatalogAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }
    }
}