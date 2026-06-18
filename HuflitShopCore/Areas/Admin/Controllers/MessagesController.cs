using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MessagesController : Controller
    {
        private readonly ChatService _chatService;

        public MessagesController(ChatService chatService)
        {
            _chatService = chatService;
        }

        // Trang quản lý chat của Admin
        [HttpGet]
        public async Task<IActionResult> Index(string? userId)
        {
            // Lấy danh sách toàn bộ các phiên chat đang hoạt động
            var sessions = await _chatService.GetActiveSessionsAsync();
            ViewBag.Sessions = sessions;

            // Nếu admin click chat trực tiếp từ trang hồ sơ khách hàng (truyền userId)
            if (!string.IsNullOrEmpty(userId))
            {
                var targetSession = await _chatService.GetOrCreateSessionAsync(userId);
                ViewBag.ActiveSessionId = targetSession.Id;
                ViewBag.ActiveUserId = userId;
                ViewBag.ActiveCustomerName = targetSession.User?.FullName ?? targetSession.User?.UserName ?? "Khách hàng";
            }
            else if (sessions.Any())
            {
                // Mặc định chọn phiên chat đầu tiên có hoạt động mới nhất
                var firstSession = sessions.First();
                ViewBag.ActiveSessionId = firstSession.SessionId;
                ViewBag.ActiveUserId = firstSession.UserId;
                ViewBag.ActiveCustomerName = firstSession.CustomerName;
            }

            return View("Chat");
        }

        // Trang Chat (trùng với Index hoặc route cấu hình)
        [HttpGet]
        public async Task<IActionResult> Chat(string? userId)
        {
            return await Index(userId);
        }

        // API lấy lịch sử cuộc trò chuyện
        [HttpGet]
        public async Task<IActionResult> GetChatHistory(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return BadRequest("SessionId không hợp lệ.");

            var messages = await _chatService.GetSessionMessagesAsync(sessionId);
            var result = messages.Select(m => new
            {
                senderType = m.SenderType,
                messageContent = m.MessageContent,
                sentAt = m.SentAt.ToString("HH:mm")
            });

            return Json(result);
        }
    }
}
