using HuflitShopCore.Data;
using HuflitShopCore.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class ReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardDTO> GetDashboardDataAsync(string roleId)
        {
            var dto = new DashboardDTO();
            dto.IsAdmin = (roleId == "1" || roleId == "Admin"); 

            int currentYear = DateTime.Now.Year;
            int currentMonth = DateTime.Now.Month;

            // Khởi tạo mảng doanh thu 12 tháng bằng 0
            dto.MonthlyRevenueList = new List<decimal>(new decimal[12]);

            // Lấy danh sách các đơn hàng đã Hoàn thành (OrderStatus = 3) trong năm nay
            var completedOrders = await _context.Orders
                .Where(o => o.OrderStatus == 3 && o.OrderDate.Year == currentYear)
                .Include(o => o.OrderDetails)
                .ToListAsync();

            dto.TotalRevenue = completedOrders.Sum(o => o.FinalAmount);
            dto.CurrentMonthRevenue = completedOrders.Where(o => o.OrderDate.Month == currentMonth).Sum(o => o.FinalAmount);

            // Tính doanh thu theo từng tháng
            for (int i = 1; i <= 12; i++)
            {
                dto.MonthlyRevenueList[i - 1] = completedOrders.Where(o => o.OrderDate.Month == i).Sum(o => o.FinalAmount);
            }

            // Tính Chi phí (Giá nhập * Số lượng bán ra) và Lợi nhuận
            dto.TotalCost = completedOrders.SelectMany(o => o.OrderDetails).Sum(od => od.PurchasedPrice * od.Quantity);
            dto.TotalProfit = dto.TotalRevenue - dto.TotalCost;

            // Tồn kho (Giả lập: Tính theo tổng số lượng tồn kho)
            var totalStock = await _context.ProductVariants.SumAsync(pv => pv.StockQuantity);
            dto.StockPercentage = totalStock > 0 ? 100 : 0; 

            // Khuyến mãi đang áp dụng (Lấy khuyến mãi ngon nhất)
            var activePromo = await _context.Promotions
                .Where(p => p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now)
                .OrderByDescending(p => p.DiscountValue)
                .FirstOrDefaultAsync();

            if (activePromo != null)
            {
                dto.HasActivePromotion = true;
                dto.ActivePromotionValue = activePromo.DiscountValue;
                dto.ActivePromotionType = activePromo.DiscountType;
            }

            // Cập nhật tính toán Review thực tế
            var reviews = await _context.Reviews.ToListAsync();
            dto.TotalReviews = reviews.Count;
            
            if (dto.TotalReviews > 0)
            {
                // Cập nhật lại số liệu hiển thị tiến độ
                dto.FiveStarPercentage = Math.Round((double)reviews.Count(r => r.RatingStars == 5) / dto.TotalReviews * 100, 1);
                dto.FourStarPercentage = Math.Round((double)reviews.Count(r => r.RatingStars == 4) / dto.TotalReviews * 100, 1);
                dto.ThreeStarPercentage = Math.Round((double)reviews.Count(r => r.RatingStars == 3) / dto.TotalReviews * 100, 1);
                dto.TwoStarPercentage = Math.Round((double)reviews.Count(r => r.RatingStars == 2) / dto.TotalReviews * 100, 1);
                dto.OneStarPercentage = Math.Round((double)reviews.Count(r => r.RatingStars == 1) / dto.TotalReviews * 100, 1);
            }

            return dto;
        }
    }
}