using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class ReviewService
    {
        private readonly AppDbContext _context;

        public ReviewService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ReviewDTO>> GetAllReviewsAsync()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.ProductVariant).ThenInclude(pv => pv.Product)
                .ToListAsync();

            return reviews.Select(r => new ReviewDTO
            {
                Id = r.Id,
                CustomerName = r.User?.FullName ?? "Khách hàng",
                ProductName = r.ProductVariant?.Product?.ProductName ?? "Sản phẩm",
                Rate = r.RatingStars,
                Content = r.ReviewComment ?? string.Empty,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<bool> DeleteReviewAsync(string id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return false;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}