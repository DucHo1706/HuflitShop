using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using HuflitShopCore.Models; // Chứa class Entity Image
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class ProductImageService
    {
        private readonly AppDbContext _context;
        private readonly string _cloudName;

        public ProductImageService(AppDbContext context, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _context = context;
            _cloudName = config["Cloudinary:CloudName"] ?? config["CloudinarySettings:CloudName"] ?? "dsamboqwp";
        }

        public async Task<List<ProductImageDTO>> GetImagesByProductIdAsync(string productId)
        {
            var images = await _context.ProductImages 
                .Where(i => i.ProductId == productId)
                .ToListAsync();

            return images.Select(i => new ProductImageDTO
            {
                Id = i.Id,
                ProductId = i.ProductId,
                PublicId = i.PublicId,
                ImageUrl = (i.PublicId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || i.PublicId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    ? i.PublicId
                    : (string.IsNullOrEmpty(i.AssetVersion) ? i.PublicId : $"https://res.cloudinary.com/{_cloudName}/image/upload/v{i.AssetVersion}/{i.PublicId}.jpg")
            }).ToList();
        }

        public async Task<ProductImageDTO?> GetImageByIdAsync(string id)
        {
            var img = await _context.ProductImages.FindAsync(id);
            if (img == null) return null;

            return new ProductImageDTO
            {
                Id = img.Id,
                ProductId = img.ProductId,
                PublicId = img.PublicId,
                ImageUrl = (img.PublicId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || img.PublicId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    ? img.PublicId
                    : (string.IsNullOrEmpty(img.AssetVersion) ? img.PublicId : $"https://res.cloudinary.com/{_cloudName}/image/upload/v{img.AssetVersion}/{img.PublicId}.jpg")
            };
        }

        public async Task<bool> CreateImageAsync(ProductImageDTO dto)
        {
            var newImage = new ProductImage 
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = dto.ProductId,
                PublicId = dto.PublicId,
                AssetVersion = dto.AssetVersion
            };

            _context.ProductImages.Add(newImage);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteImageAsync(string id)
        {
            var img = await _context.ProductImages.FindAsync(id);
            if (img == null) return false;

            _context.ProductImages.Remove(img);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}