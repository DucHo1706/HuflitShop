using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using HuflitShopCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class PromotionService
    {
        private readonly AppDbContext _context;

        public PromotionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PromotionDTO>> GetAllPromotionsAsync()
        {
            var promotions = await _context.Promotions.OrderByDescending(p => p.StartDate).ToListAsync();
            return promotions.Select(p => new PromotionDTO
            {
                Id = p.Id,
                PromoCode = p.PromoCode,
                DiscountType = p.DiscountType,
                DiscountValue = p.DiscountValue,
                MinOrderAmount = p.MinOrderAmount,
                MaxDiscountAmount = p.MaxDiscountAmount,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                UsageLimit = p.UsageLimit,
                UsedCount = p.UsedCount,
                IsActive = p.IsActive,
                ApplicableProductId = p.ApplicableProductId,
                ComboProductIds = p.ComboProductIds,
                IsAutoApply = p.IsAutoApply
            }).ToList();
        }

        public async Task<PromotionDTO?> GetPromotionByIdAsync(string id)
        {
            var p = await _context.Promotions.FindAsync(id);
            if (p == null) return null;

            return new PromotionDTO
            {
                Id = p.Id,
                PromoCode = p.PromoCode,
                DiscountType = p.DiscountType,
                DiscountValue = p.DiscountValue,
                MinOrderAmount = p.MinOrderAmount,
                MaxDiscountAmount = p.MaxDiscountAmount,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                UsageLimit = p.UsageLimit,
                UsedCount = p.UsedCount,
                IsActive = p.IsActive,
                ApplicableProductId = p.ApplicableProductId,
                ComboProductIds = p.ComboProductIds,
                IsAutoApply = p.IsAutoApply
            };
        }

        public async Task<bool> CreatePromotionAsync(PromotionDTO dto)
        {
            var promotion = new Promotion
            {
                Id = Guid.NewGuid().ToString(),
                PromoCode = dto.PromoCode,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MinOrderAmount = dto.MinOrderAmount,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                UsageLimit = dto.UsageLimit,
                UsedCount = dto.UsedCount,
                IsActive = dto.IsActive,
                ApplicableProductId = dto.ApplicableProductId,
                ComboProductIds = dto.ComboProductIds,
                IsAutoApply = dto.IsAutoApply
            };

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePromotionAsync(PromotionDTO dto)
        {
            var p = await _context.Promotions.FindAsync(dto.Id);
            if (p == null) return false;

            p.PromoCode = dto.PromoCode;
            p.DiscountType = dto.DiscountType;
            p.DiscountValue = dto.DiscountValue;
            p.MinOrderAmount = dto.MinOrderAmount;
            p.MaxDiscountAmount = dto.MaxDiscountAmount;
            p.StartDate = dto.StartDate;
            p.EndDate = dto.EndDate;
            p.UsageLimit = dto.UsageLimit;
            p.IsActive = dto.IsActive;
            p.ApplicableProductId = dto.ApplicableProductId;
            p.ComboProductIds = dto.ComboProductIds;
            p.IsAutoApply = dto.IsAutoApply;

            _context.Promotions.Update(p);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePromotionAsync(string id)
        {
            var p = await _context.Promotions.FindAsync(id);
            if (p == null) return false;

            _context.Promotions.Remove(p);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Promotion?> ValidatePromoCodeAsync(string promoCode, decimal orderTotal)
        {
            if (string.IsNullOrWhiteSpace(promoCode)) return null;

            return await _context.Promotions
                .FirstOrDefaultAsync(p => p.PromoCode == promoCode && p.IsActive);
        }

        public decimal CalculateDiscount(Promotion promo, decimal orderTotal)
        {
            decimal discount = 0;
            if (string.Equals(promo.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(promo.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
            {
                discount = orderTotal * (promo.DiscountValue / 100);
                if (promo.MaxDiscountAmount.HasValue && discount > promo.MaxDiscountAmount.Value)
                {
                    discount = promo.MaxDiscountAmount.Value;
                }
            }
            else
            {
                discount = promo.DiscountValue;
            }

            if (discount > orderTotal)
            {
                discount = orderTotal;
            }

            return discount;
        }
    }
}