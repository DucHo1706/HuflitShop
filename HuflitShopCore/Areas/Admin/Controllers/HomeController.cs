using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly ReportService _reportService;

        public HomeController(ReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<IActionResult> Index()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var dashboardData = await _reportService.GetDashboardDataAsync(userId);
            
            return View(dashboardData);
        }

        public IActionResult Instruction()
        {
            return View();
        }
    }
}