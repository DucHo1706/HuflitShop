using HuflitShopCore.Data;
using HuflitShopCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class CartService
    {
        private readonly AppDbContext _context;

        public CartService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Cart>> GetCartItemsAsync(string? userId, string? guestCartId)
        {
            return await _context.Carts
                .Include(c => c.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .Include(c => c.ProductVariant)
                    .ThenInclude(pv => pv.Size)
                .Include(c => c.ProductVariant)
                    .ThenInclude(pv => pv.Color)
                .Where(c => c.UserId == userId && c.SessionId == guestCartId)
                .OrderByDescending(c => c.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetCartCountAsync(string? userId, string? guestCartId)
        {
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(guestCartId))
            {
                return 0;
            }

            return await _context.Carts
                .Where(c => c.UserId == userId && c.SessionId == guestCartId)
                .SumAsync(c => c.Quantity);
        }

        public async Task AddOrUpdateCartItemAsync(string? userId, string? guestCartId, string productVariantId, int quantity)
        {
            var variant = await _context.ProductVariants
                .Include(pv => pv.Product)
                .FirstOrDefaultAsync(pv => pv.Id == productVariantId);

            if (variant == null) throw new InvalidOperationException("Biến thể sản phẩm không tồn tại.");

            var existing = await _context.Carts.FirstOrDefaultAsync(c =>
                c.ProductVariantId == productVariantId &&
                c.UserId == userId &&
                c.SessionId == guestCartId);

            int targetQuantity = quantity;
            if (existing != null)
            {
                targetQuantity += existing.Quantity;
            }

            if (variant.StockQuantity < targetQuantity)
            {
                throw new InvalidOperationException($"Không thể thêm số lượng đã chọn. Kho chỉ còn {variant.StockQuantity} sản phẩm (Giỏ hàng của bạn đang có {existing?.Quantity ?? 0} sản phẩm).");
            }

            if (existing != null)
            {
                existing.Quantity = targetQuantity;
                _context.Carts.Update(existing);
            }
            else
            {
                var cart = new Cart
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductVariantId = productVariantId,
                    Quantity = quantity,
                    UserId = userId,
                    SessionId = guestCartId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateQuantityAsync(string cartId, int quantity)
        {
            var item = await _context.Carts
                .Include(c => c.ProductVariant)
                .FirstOrDefaultAsync(c => c.Id == cartId);
            if (item == null) return false;

            var variant = item.ProductVariant;
            if (variant != null && variant.StockQuantity < quantity)
            {
                throw new InvalidOperationException($"Số lượng yêu cầu ({quantity}) vượt quá tồn kho hiện tại ({variant.StockQuantity}).");
            }

            item.Quantity = Math.Max(1, quantity);
            _context.Carts.Update(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveCartItemAsync(string cartId)
        {
            var item = await _context.Carts.FirstOrDefaultAsync(c => c.Id == cartId);
            if (item == null) return false;

            _context.Carts.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ClearCartAsync(string userId)
        {
            var items = await _context.Carts.Where(c => c.UserId == userId).ToListAsync();
            if (items.Any())
            {
                _context.Carts.RemoveRange(items);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Promotion>> GetActiveAutoPromotionsAsync()
        {
            var now = DateTime.Now;
            return await _context.Promotions
                .Where(p => p.IsActive && p.IsAutoApply && p.StartDate <= now && p.EndDate >= now)
                .ToListAsync();
        }
    }
}
