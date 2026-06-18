using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using HuflitShopCore.Models;
using Microsoft.EntityFrameworkCore;

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
                Content = r.GetCommentText(),
                AdminReply = r.GetAdminReply(),
                HelpfulVotes = r.GetHelpfulVotes(),
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

        public async Task<List<Reviews>> GetReviewsByProductIdAsync(string productId)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.ProductVariant).ThenInclude(pv => pv.Size)
                .Include(r => r.ProductVariant).ThenInclude(pv => pv.Color)
                .Where(r => r.ProductVariant.ProductId == productId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> AddReviewAsync(string userId, string productId, int ratingStars, string comment)
        {
            var variant = await _context.ProductVariants
                .Where(pv => pv.ProductId == productId)
                .FirstOrDefaultAsync();

            if (variant == null) return false;

            var review = new Reviews
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                ProductVariantId = variant.Id,
                RatingStars = Math.Clamp(ratingStars, 1, 5),
                ReviewComment = comment ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Reviews.Add(review);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> AddReplyAsync(string reviewId, string replyText)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return false;

            var commentOnly = review.GetCommentText();
            var votesCount = review.GetHelpfulVotes();

            var newComment = commentOnly;
            if (!string.IsNullOrEmpty(replyText))
            {
                newComment += "|||REPLY:" + replyText.Trim();
            }
            if (votesCount > 0)
            {
                newComment += "|||VOTES:" + votesCount;
            }

            review.ReviewComment = newComment;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> VoteHelpfulAsync(string reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return 0;

            var commentOnly = review.GetCommentText();
            var replyText = review.GetAdminReply();
            var newVotes = review.GetHelpfulVotes() + 1;

            var newComment = commentOnly;
            if (!string.IsNullOrEmpty(replyText))
            {
                newComment += "|||REPLY:" + replyText;
            }
            newComment += "|||VOTES:" + newVotes;

            review.ReviewComment = newComment;
            await _context.SaveChangesAsync();
            return newVotes;
        }
    }
}