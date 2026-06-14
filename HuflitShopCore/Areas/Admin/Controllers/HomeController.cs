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
            // Tùy theo cách bạn lưu trữ Identity, nếu có Claim "RoleId" thì lấy, không thì lấy ClaimTypes.Role mặc định.
            string roleId = User.FindFirstValue("RoleId") ?? User.FindFirstValue(ClaimTypes.Role) ?? "1"; 
            
            var dashboardData = await _reportService.GetDashboardDataAsync(roleId);
            
            return View(dashboardData);
        }

        public IActionResult Instruction()
        {
            return View();
        }
    }
}