using HuflitShopCore.Data;
using HuflitShopCore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Roles = "Admin")] // Uncomment if Authorization is fully set up
    public class ShopSettingController : Controller
    {
        private readonly AppDbContext _context;

        public ShopSettingController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/ShopSetting
        public async Task<IActionResult> Index()
        {
            var settings = await _context.ShopSettings.ToListAsync();
            return View(settings);
        }

        // POST: Admin/ShopSetting/UpdateWifiIp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWifiIp()
        {
            // Lấy IP từ Request hiện tại
            var currentIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(currentIp))
            {
                TempData["ErrorMessage"] = "Không thể xác định địa chỉ IP của bạn.";
                return RedirectToAction(nameof(Index));
            }

            // Tìm setting "AllowedWiFiIP"
            var setting = await _context.ShopSettings.FirstOrDefaultAsync(s => s.Key == "AllowedWiFiIP");
            
            if (setting == null)
            {
                setting = new ShopSetting
                {
                    Key = "AllowedWiFiIP",
                    Value = currentIp,
                    Description = "Địa chỉ IP public của WiFi cửa hàng để nhân viên check-in"
                };
                _context.ShopSettings.Add(setting);
            }
            else
            {
                setting.Value = currentIp;
                setting.UpdatedAt = System.DateTime.Now;
                _context.ShopSettings.Update(setting);
            }

            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Đã cập nhật IP Cửa Hàng thành công: {currentIp}";
            return RedirectToAction(nameof(Index));
        }
    }
}
