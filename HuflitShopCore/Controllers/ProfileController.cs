using System;
using System.Security.Claims;
using System.Threading.Tasks;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HuflitShopCore.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserService _userService;
        private readonly OrderService _orderService;
        private readonly CartService _cartService;
        private readonly ProductService _productService;

        public ProfileController(UserService userService, OrderService orderService, CartService cartService, ProductService productService)
        {
            _userService = userService;
            _orderService = orderService;
            _cartService = cartService;
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            return await Index();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound();

            var orders = await _orderService.GetOrdersByUserIdAsync(userId);

            ViewBag.Orders = orders;
            return View("Index", user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber, int? gender, DateTime? dateOfBirth)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var result = await _userService.UpdateProfileAsync(userId, fullName, phoneNumber, gender, dateOfBirth);
            if (result)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình cập nhật thông tin.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var result = await _orderService.CancelOrderByCustomerAsync(id, userId);
            if (result)
            {
                TempData["SuccessMessage"] = "Hủy đơn hàng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng này (Đơn hàng có thể đã được duyệt hoặc không tồn tại).";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetail(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null || order.UserId != userId)
            {
                return NotFound();
            }

            // Lấy 4 sản phẩm gợi ý cho phần "Có thể bạn cũng thích"
            var suggestedProducts = await _productService.GetSuggestedProductsAsync(4);
            ViewBag.SuggestedProducts = suggestedProducts;

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReOrder(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null || order.UserId != userId)
            {
                return NotFound();
            }

            int addedCount = 0;
            foreach (var detail in order.OrderDetails)
            {
                if (!string.IsNullOrEmpty(detail.ProductVariantId))
                {
                    // Thêm trực tiếp vào giỏ của khách hàng
                    await _cartService.AddOrUpdateCartItemAsync(userId, null, detail.ProductVariantId, detail.Quantity);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                TempData["SuccessMessage"] = $"Đã thêm lại {addedCount} sản phẩm vào giỏ hàng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không có sản phẩm hợp lệ nào trong đơn hàng để thêm lại.";
            }

            return RedirectToAction("Cart", "Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRefund(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var result = await _orderService.RequestOrderRefundAsync(id, userId);
            if (result)
            {
                TempData["SuccessMessage"] = "Gửi yêu cầu hoàn tiền thành công! Cửa hàng sẽ kiểm tra và phản hồi trong thời gian sớm nhất.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể gửi yêu cầu hoàn tiền (Đơn hàng phải ở trạng thái đã hoàn thành và chưa từng hoàn tiền).";
            }

            return RedirectToAction("OrderDetail", new { id = id });
        }
    }
}
