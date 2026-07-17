using HuflitShopCore.Data;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HuflitShopCore.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;
        private readonly AIChatService _aiChatService;
        private readonly AppDbContext _context;

        public ChatHub(ChatService chatService, AppDbContext context, AIChatService aiChatService)
        {
            _chatService = chatService;
            _aiChatService = aiChatService;
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("Id")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Cho người dùng vào nhóm riêng của họ theo UserId
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);

                // Kiểm tra xem người dùng có phải là Admin hoặc Nhân viên không
                var isAdminOrStaff = _context.UserRoles.Any(ur => ur.UserId == userId &&
                    (ur.RoleId == "1" || ur.RoleId == "ROLE-ADMIN" || ur.RoleId == "2" || ur.RoleId == "ROLE-EMPLOYEE"));

                if (isAdminOrStaff)
                {
                    // Nếu là Admin/Nhân viên, đưa vào nhóm "Admins" để nhận tất cả tin nhắn từ khách
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                }
            }

            await base.OnConnectedAsync();
        }

        // Khách hàng gửi tin nhắn lên cho Admin
        public async Task SendMessageToAdmin(string messageContent)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("Id")?.Value;

            if (string.IsNullOrEmpty(userId)) return;

            // Lấy hoặc tạo phiên chat
            var session = await _chatService.GetOrCreateSessionAsync(userId);

            // Lưu tin nhắn vào DB
            var message = await _chatService.SaveMessageAsync(session.Id, "User", messageContent);

            // Tìm thông tin User để gửi kèm tên/ảnh đại diện
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            var customerName = user?.FullName ?? user?.UserName ?? "Khách hàng";
            var customerAvatar = user?.Avatar ?? "/Client/img/default-user.jpg";

            // Bắn tin nhắn đến nhóm Admins để cập nhật màn hình Admin Chat
            await Clients.Group("Admins").SendAsync("ReceiveAdminMessage", new
            {
                sessionId = session.Id,
                userId = userId,
                customerName = customerName,
                customerAvatar = customerAvatar,
                messageContent = message.MessageContent,
                sentAt = message.SentAt.ToString("HH:mm"),
                senderType = "User" // Để admin biết đây là tin từ khách
            });
        }

        // Admin gửi tin nhắn phản hồi cho Khách hàng cụ thể
        public async Task SendMessageToUser(string targetUserId, string messageContent)
        {
            var adminId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? Context.User?.FindFirst("Id")?.Value;

            if (string.IsNullOrEmpty(adminId)) return;

            // Lấy phiên chat của khách hàng mục tiêu
            var session = await _chatService.GetOrCreateSessionAsync(targetUserId);

            // Lưu tin nhắn Admin gửi vào DB
            var message = await _chatService.SaveMessageAsync(session.Id, "Admin", messageContent);

            // Bắn tin nhắn đến nhóm Admins để cập nhật cho các Admin khác đang online thấy đồng bộ
            await Clients.Group("Admins").SendAsync("ReceiveAdminMessage", new
            {
                sessionId = session.Id,
                userId = targetUserId,
                messageContent = message.MessageContent,
                sentAt = message.SentAt.ToString("HH:mm"),
                senderType = "Admin"
            });

            // Bắn tin nhắn trực tiếp đến nhóm của Khách hàng đó
            await Clients.Group(targetUserId).SendAsync("ReceiveUserMessage", new
            {
                messageContent = message.MessageContent,
                sentAt = message.SentAt.ToString("HH:mm"),
                isAdmin = true
            });
        }

        // Khách hàng gửi tin nhắn cho AI
        public async Task SendMessageToAI(string message)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("Id")?.Value;

            if (string.IsNullOrEmpty(userId)) return;

            var session = await _chatService.GetOrCreateSessionAsync(userId);

            // 1. Lưu tin nhắn của khách
            var userMsg = await _chatService.SaveMessageAsync(session.Id, "User", message);

            await Clients.Caller.SendAsync("ReceiveUserMessage", new ChatMessageDTO
            {
                MessageContent = userMsg.MessageContent,
                SentAt = userMsg.SentAt.ToString("HH:mm"),
                IsAdmin = false,
                SenderType = "User"
            });

            // 2. Gọi AI
            var aiReplyText = await _aiChatService.GetAIResponseAsync(message, userId);

            // 3. Lưu tin nhắn của AI và trả về cho UI
            var aiMsg = await _chatService.SaveMessageAsync(session.Id, "AI", aiReplyText);

            await Clients.Caller.SendAsync("ReceiveUserMessage", new ChatMessageDTO
            {
                MessageContent = aiMsg.MessageContent,
                SentAt = aiMsg.SentAt.ToString("HH:mm"),
                IsAdmin = true,
                SenderType = "AI"
            });
        }

        // Khách hàng tự lấy lịch sử chat của mình qua Hub
        public async Task<List<ChatMessageDTO>> GetChatHistory()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("Id")?.Value;

            if (string.IsNullOrEmpty(userId)) return new List<ChatMessageDTO>();

            var session = await _chatService.GetOrCreateSessionAsync(userId);
            // Chỉ lấy lịch sử chat giữa User và Admin, bỏ qua các tin nhắn của AI
            var messages = (await _chatService.GetSessionMessagesAsync(session.Id))
                           .Where(m => m.SenderType != "AI").ToList();

            return messages.Select(m => new ChatMessageDTO
            {
                SenderType = m.SenderType, // "User", "Admin", hoặc "AI"
                MessageContent = m.MessageContent,
                SentAt = m.SentAt.ToString("HH:mm")
            }).ToList();
        }
    }

    public class ChatMessageDTO
    {
        public string SenderType { get; set; } = string.Empty;
        public string MessageContent { get; set; } = string.Empty;
        public string SentAt { get; set; } = string.Empty;
        public bool IsAdmin { get; set; } // Giữ lại để tương thích với code JS cũ
    }
}
