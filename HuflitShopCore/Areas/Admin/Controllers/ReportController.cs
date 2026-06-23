using System;
using System.Threading.Tasks;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize] // Ensure only logged-in admin/employees can access
    public class ReportController : Controller
    {
        private readonly ReportService _reportService;

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? year)
        {
            int selectedYear = year ?? DateTime.Now.Year;
            ViewBag.SelectedYear = selectedYear;

            // Fetch base orders exactly once to prevent N+1 database queries!
            var orders = await _reportService.GetBaseOrdersAsync(selectedYear);

            // Fetch statistics calculations using CPU-only memory operations
            var monthlyRevenue = _reportService.GetMonthlyRevenue(orders);
            var quarterlyRevenue = _reportService.GetQuarterlyRevenue(orders);
            var seasonalRevenue = _reportService.GetSeasonalRevenue(orders);
            var topProducts = _reportService.GetTopSellingProducts(orders, 5);
            
            // Highly optimized GroupBy query (1 DB query)
            var yearlyComparison = await _reportService.GetYearlyComparisonAsync();
            
            // If any order starts with "mock-", it means we are using mock data
            bool isMock = orders.Any(o => o.Id != null && o.Id.StartsWith("mock-"));

            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.QuarterlyRevenue = quarterlyRevenue;
            ViewBag.SeasonalRevenue = seasonalRevenue;
            ViewBag.TopProducts = topProducts;
            ViewBag.YearlyComparison = yearlyComparison;
            ViewBag.IsMockData = isMock;

            return View(selectedYear);
        }

        // Redirect old routes for safety
        [HttpGet]
        public IActionResult ReportByMonth(int? year) => RedirectToAction(nameof(Index), new { year });

        [HttpGet]
        public IActionResult ReportByYear(int? year) => RedirectToAction(nameof(Index), new { year });
    }
}
