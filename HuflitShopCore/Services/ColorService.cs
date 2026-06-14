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
    public class ColorService
    {
        private readonly AppDbContext _context;

        public ColorService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ColorDTO>> GetAllColorsAsync()
        {
            var colors = await _context.Colors.ToListAsync();
            return colors.Select(c => new ColorDTO
            {
                Id = c.Id,
                ColorName = c.ColorName,
                HexCode = c.HexCode
            }).ToList();
        }

        public async Task<ColorDTO?> GetColorByIdAsync(string id)
        {
            var color = await _context.Colors.FindAsync(id);
            if (color == null) return null;
            return new ColorDTO { Id = color.Id, ColorName = color.ColorName, HexCode = color.HexCode };
        }

        public async Task<bool> CreateColorAsync(ColorDTO dto)
        {
            var color = new Color
            {
                Id = Guid.NewGuid().ToString(),
                ColorName = dto.ColorName,
                HexCode = dto.HexCode
            };
            _context.Colors.Add(color);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateColorAsync(ColorDTO dto)
        {
            var color = await _context.Colors.FindAsync(dto.Id);
            if (color == null) return false;
            
            color.ColorName = dto.ColorName;
            color.HexCode = dto.HexCode;

            _context.Colors.Update(color);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string ErrorMsg)> DeleteColorAsync(string id)
        {
            var color = await _context.Colors.FindAsync(id);
            if (color == null) return (false, "Không tìm thấy màu sắc.");

            _context.Colors.Remove(color);
            return await DbErrorHandler.ExecuteAsync(async () => {
                await _context.SaveChangesAsync();
            });
        }
    }
}