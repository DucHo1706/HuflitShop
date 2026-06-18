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

            // Fetch statistics from service
            var monthlyRevenue = await _reportService.GetMonthlyRevenueAsync(selectedYear);
            var quarterlyRevenue = await _reportService.GetQuarterlyRevenueAsync(selectedYear);
            var seasonalRevenue = await _reportService.GetSeasonalRevenueAsync(selectedYear);
            var topProducts = await _reportService.GetTopSellingProductsAsync(selectedYear, 5);
            var yearlyComparison = await _reportService.GetYearlyComparisonAsync();
            var isMock = await _reportService.IsUsingMockDataAsync();

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
