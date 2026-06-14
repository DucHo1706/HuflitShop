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
            var categories = await _context.Categories.ToListAsync();
            
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

        public async Task<bool> CreateCategoryAsync(CategoryDTO dto)
        {
            var category = new Category
            {
                Id = Guid.NewGuid().ToString(),
                CategoryName = dto.CategoryName,
                ParentId = string.IsNullOrEmpty(dto.ParentId) ? null : dto.ParentId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCategoryAsync(CategoryDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Id)) return false;

            var category = await _context.Categories.FindAsync(dto.Id);
            if (category == null) return false;

            category.CategoryName = dto.CategoryName;
            category.ParentId = string.IsNullOrEmpty(dto.ParentId) ? null : dto.ParentId;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
            return true;
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
    }
}