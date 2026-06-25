using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;
        private readonly IServiceProvider _serviceProvider;

        public OrderController(OrderService orderService, IServiceProvider serviceProvider)
        {
            _orderService = orderService;
            _serviceProvider = serviceProvider;
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

            // Giả lập giao hàng tự động cho GrabExpress/Ahamove khi Admin chuyển sang "Đang giao" (2)
            if (status == 2)
            {
                // Chạy ngầm mô phỏng giao hàng mà không chặn luồng chính của Admin
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Giả lập thời gian tài xế di chuyển (30 giây)
                        await Task.Delay(30000);

                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();
                            var order = await orderService.GetOrderByIdAsync(id);
                            
                            // Chỉ tự động hoàn thành nếu đơn hàng đang ở trạng thái Đang giao (2)
                            if (order != null && order.OrderStatus == 2)
                            {
                                await orderService.UpdateOrderStatusAsync(id, 3); // 3: Hoàn thành
                            }
                        }
                    }
                    catch
                    {
                        // Nuốt lỗi nếu có để tránh crash tiến trình chạy ngầm
                    }
                });
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}