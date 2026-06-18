using HuflitShopCore.Data;
using HuflitShopCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuflitShopCore.Services
{
    public class ChatService
    {
        private readonly AppDbContext _context;

        public ChatService(AppDbContext context)
        {
            _context = context;
        }

        // Lấy hoặc tạo mới phiên chat cho người dùng
        public async Task<ChatSession> GetOrCreateSessionAsync(string userId)
        {
            var session = await _context.ChatSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

            if (session == null)
            {
                session = new ChatSession
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    SessionStartedAt = DateTime.Now,
                    IsActive = true
                };

                _context.ChatSessions.Add(session);
                await _context.SaveChangesAsync();

                // Nạp lại thông tin User để hiển thị
                session = await _context.ChatSessions
                    .Include(s => s.User)
                    .FirstAsync(s => s.Id == session.Id);
            }

            return session;
        }

        // Lưu tin nhắn mới vào phiên chat
        public async Task<ChatMessage> SaveMessageAsync(string sessionId, string senderType, string content)
        {
            var message = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = sessionId,
                SenderType = senderType, // "User" hoặc "Admin"
                MessageContent = content,
                SentAt = DateTime.Now
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        // Lấy lịch sử tin nhắn của một phiên chat
        public async Task<List<ChatMessage>> GetSessionMessagesAsync(string sessionId)
        {
            return await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        // Lấy tất cả phiên chat đang hoạt động kèm thông tin khách hàng và tin nhắn cuối
        public async Task<List<ChatSessionSummaryDTO>> GetActiveSessionsAsync()
        {
            var sessions = await _context.ChatSessions
                .Include(s => s.User)
                .Include(s => s.ChatMessages)
                .Where(s => s.IsActive)
                .ToListAsync();

            var summaries = sessions.Select(s => {
                var lastMsg = s.ChatMessages.OrderByDescending(m => m.SentAt).FirstOrDefault();
                return new ChatSessionSummaryDTO
                {
                    SessionId = s.Id,
                    UserId = s.UserId,
                    CustomerName = s.User?.FullName ?? s.User?.UserName ?? "Khách hàng",
                    CustomerAvatar = s.User?.Avatar ?? "/Client/img/default-user.jpg",
                    LastMessageContent = lastMsg?.MessageContent ?? "Bắt đầu cuộc trò chuyện...",
                    LastMessageTime = lastMsg?.SentAt ?? s.SessionStartedAt,
                    SenderType = lastMsg?.SenderType ?? "System"
                };
            })
            .OrderByDescending(s => s.LastMessageTime)
            .ToList();

            return summaries;
        }
    }

    // DTO phục vụ hiển thị danh sách các cuộc chat bên Admin
    public class ChatSessionSummaryDTO
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAvatar { get; set; } = string.Empty;
        public string LastMessageContent { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
        public string SenderType { get; set; } = string.Empty; // "User", "Admin", hoặc "System"
    }
}
