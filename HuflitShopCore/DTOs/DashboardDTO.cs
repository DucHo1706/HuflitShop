using System.Collections.Generic;

namespace HuflitShopCore.DTOs
{
    public class DashboardDTO
    {
        public bool IsAdmin { get; set; } // Xác định xem User có quyền xem Doanh thu không
        
        public decimal CurrentMonthRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        
        public List<decimal> MonthlyRevenueList { get; set; } = new List<decimal>(new decimal[12]);
        
        public double StockPercentage { get; set; }
        public int TotalReviews { get; set; }
        
        public double FiveStarPercentage { get; set; }
        public double FourStarPercentage { get; set; }
        public double ThreeStarPercentage { get; set; }
        public double TwoStarPercentage { get; set; }
        public double OneStarPercentage { get; set; }
        
        public bool HasActivePromotion { get; set; }
        public decimal ActivePromotionValue { get; set; }
        public string ActivePromotionType { get; set; } = string.Empty;
    }
}