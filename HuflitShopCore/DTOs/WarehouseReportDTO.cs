using System;
using System.Collections.Generic;

namespace HuflitShopCore.DTOs
{
    public class WarehouseReportDTO
    {
        // KPI Cards
        public int BeginningStockQty { get; set; }
        public decimal BeginningStockValue { get; set; }
        public double BeginningStockValueMoM { get; set; } // % change Vs previous month

        public int ImportStockQty { get; set; }
        public decimal ImportStockValue { get; set; }
        public double ImportStockValueMoM { get; set; }

        public int ExportStockQty { get; set; }
        public decimal ExportStockValue { get; set; } // COGS
        public double ExportStockValueMoM { get; set; }

        public int EndingStockQty { get; set; }
        public decimal EndingStockValue { get; set; }
        public double EndingStockValueMoM { get; set; }

        public decimal Revenue { get; set; }
        public double RevenueMoM { get; set; }

        public decimal AverageExportPrice { get; set; } // Đơn giá BQ xuất kho
        public double AverageExportPriceMoM { get; set; }

        public decimal GrossProfit { get; set; }
        public double GrossProfitMoM { get; set; }

        public double GrossProfitMargin { get; set; } // Tỉ suất LNG (%)
        public double GrossProfitMarginMoM { get; set; }

        // Doanh thu gộp & ròng (MỚI)
        public decimal GrossRevenue { get; set; }          // Σ(PurchasedPrice × Qty) — trước giảm giá
        public decimal NetRevenue { get; set; }            // GrossRevenue - TotalDiscount — sau giảm giá
        public decimal TotalDiscount { get; set; }         // Tổng giảm giá phân bổ
        public decimal TotalShippingRevenue { get; set; }  // Tổng ship thu từ khách

        // Filter selection history for UI
        public int SelectedYear { get; set; }
        public int SelectedMonth { get; set; }
        public string SelectedSupplierId { get; set; } = "All";
        public string SelectedCategoryId { get; set; } = "All";
        public string SelectedProductId { get; set; } = "All";

        // Lists for charts & tables
        public List<MonthlyTrendItem> MonthlyTrends { get; set; } = new();
        public List<TopProductStockItem> TopStockProducts { get; set; } = new();
        public List<SupplierPerformanceItem> SupplierPerformances { get; set; } = new();
        public List<ProductSummaryItem> ProductSummaries { get; set; } = new();
        public List<TopProductRevenueItem> TopRevenueProducts { get; set; } = new();
        public List<ProductProfitItem> ProductProfits { get; set; } = new(); // Lãi/lỗ từng SP (MỚI)
    }

    public class MonthlyTrendItem
    {
        public string MonthLabel { get; set; } = string.Empty;
        public int ExportQty { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopProductStockItem
    {
        public string ProductName { get; set; } = string.Empty;
        public int EndingStockQty { get; set; }
        public decimal EndingStockValue { get; set; }
    }

    public class SupplierPerformanceItem
    {
        public string SupplierName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal GrossProfit { get; set; }
        public double GrossProfitMargin { get; set; }
    }

    public class ProductSummaryItem
    {
        public string ProductName { get; set; } = string.Empty;
        public int BeginningQty { get; set; }
        public decimal BeginningValue { get; set; }
        public int ImportQty { get; set; }
        public decimal ImportValue { get; set; }
        public int ExportQty { get; set; }
        public decimal ExportValue { get; set; }
        public int EndingQty { get; set; }
        public decimal EndingValue { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopProductRevenueItem
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class ProductProfitItem
    {
        public string ProductName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal GrossRevenue { get; set; }        // Doanh thu gộp (giá bán × SL)
        public decimal DiscountAllocated { get; set; }   // Giảm giá phân bổ
        public decimal NetRevenue { get; set; }           // Doanh thu ròng (gộp - giảm)
        public decimal COGS { get; set; }                 // Giá vốn hàng bán (FIFO)
        public decimal GrossProfit { get; set; }          // Lãi gộp = Net - COGS
        public double ProfitMargin { get; set; }          // Tỉ suất lãi gộp (%)
    }
}
