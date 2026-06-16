using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using HuflitShopCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using HuflitShopCore.Helpers;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class SizeService
    {
        private readonly AppDbContext _context;

        public SizeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SizeDTO>> GetAllSizesAsync()
        {
            var sizes = await _context.Sizes.ToListAsync();
            return sizes.Select(s => new SizeDTO
            {
                Id = s.Id,
                SizeName = s.SizeName,
                SizeType = s.SizeType
            }).ToList();
        }

        public async Task<SizeDTO?> GetSizeByIdAsync(string id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size == null) return null;
            return new SizeDTO { Id = size.Id, SizeName = size.SizeName, SizeType = size.SizeType };
        }

        public async Task<bool> CreateSizeAsync(SizeDTO dto)
        {
            var size = new Size
            {
                Id = Guid.NewGuid().ToString(),
                SizeName = dto.SizeName,
                SizeType = dto.SizeType
            };
            _context.Sizes.Add(size);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateSizeAsync(SizeDTO dto)
        {
            var size = await _context.Sizes.FindAsync(dto.Id);
            if (size == null) return false;
            
            size.SizeName = dto.SizeName;
            size.SizeType = dto.SizeType;

            _context.Sizes.Update(size);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string ErrorMsg)> DeleteSizeAsync(string id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size == null) return (false, "Không tìm thấy kích thước.");

            _context.Sizes.Remove(size);
            return await DbErrorHandler.ExecuteAsync(async () => {
                await _context.SaveChangesAsync();
            });
        }

        public async Task<List<string>> GetExistingSizeTypesAsync()
        {
            return await _context.Sizes
                .Select(s => s.SizeType)
                .Distinct()
                .ToListAsync();
        }
    }
}