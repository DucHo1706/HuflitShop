using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;

        public OrderController(OrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<IActionResult> Index(bool pendingOnly = false)
        {
            ViewBag.PendingOnly = pendingOnly;
            var orders = pendingOnly 
                ? await _orderService.GetPendingOrdersAsync()
                : await _orderService.GetAllOrdersAsync();
                
            return View(orders);
        }

        public async Task<IActionResult> Details(string id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, int status)
        {
            await _orderService.UpdateOrderStatusAsync(id, status);
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}