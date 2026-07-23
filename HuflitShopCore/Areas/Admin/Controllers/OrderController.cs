using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;
        private readonly ShipmentService _shipmentService;

        public OrderController(OrderService orderService, ShipmentService shipmentService)
        {
            _orderService = orderService;
            _shipmentService = shipmentService;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string id, int status)
        {
            try
            {
                if (status == 4) await _shipmentService.CancelGhnOrderAsync(id);
                await _orderService.UpdateOrderStatusAsync(id, status);
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }

            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShipment(string id, CancellationToken cancellationToken)
        {
            try
            {
                await _shipmentService.CreateGhnOrderAsync(id, cancellationToken);
                TempData["SuccessMessage"] = "Đã tạo vận đơn GHN thành công.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncShipment(string id, CancellationToken cancellationToken)
        {
            try
            {
                await _shipmentService.SynchronizeGhnOrderAsync(id, cancellationToken);
                TempData["SuccessMessage"] = "Đã đồng bộ trạng thái mới nhất từ GHN.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
