using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Services;
using System.Security.Claims;

namespace QuickCashJobAPI.Controllers
{
    [Route("api/chat")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;


        public ChatController(ApplicationDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessage message)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (senderId == null) return Unauthorized();

            // Overwrite whatever the client sent (or didn't send)
            message.SenderId = senderId;
            message.Timestamp = DateTime.UtcNow;

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // Send FCM Notification to Receiver
            var receiver = await _context.Users.FindAsync(message.ReceiverId);
            if (receiver != null && !string.IsNullOrEmpty(receiver.FcmToken))
            {
                await _notificationService.SendNotificationAsync(
                    receiver.FcmToken,
                    "📨 New Message",
                    "You have a new message from someone"
                );
            }

            return Ok(message);
        }


        [HttpGet("conversation/{userId}")]
        public async Task<IActionResult> GetConversation(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var messages = await _context.ChatMessages
                .Where(m =>
                    (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                    (m.SenderId == userId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return Ok(messages);
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetUserConversations()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get all users the current user has chatted with
            var userIds = await _context.ChatMessages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            // For each user, get their name and the number of unread messages they sent to the current user
            var conversations = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new
                {
                    userId = u.Id,
                    userName = u.Name,
                    unreadCount = _context.ChatMessages
                        .Count(m =>
                            m.SenderId == u.Id &&
                            m.ReceiverId == currentUserId &&
                            !m.IsRead)
                })
                .ToListAsync();

            return Ok(conversations);
        }



        [HttpGet("unread/{userId}")]
        public async Task<IActionResult> GetUnreadMessageCount(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId != userId) return Unauthorized();

            // Count unread messages for the current user
            var unreadMessageCount = await _context.ChatMessages
                .Where(m => m.ReceiverId == currentUserId && m.IsRead == false)
                .CountAsync();

            return Ok(new { UnreadMessageCount = unreadMessageCount });
        }


        [HttpPost("markasread/{senderId}")]
        public async Task<IActionResult> MarkMessagesAsRead(string senderId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var unreadMessages = await _context.ChatMessages
                .Where(m =>
                    m.SenderId == senderId &&
                    m.ReceiverId == currentUserId &&
                    !m.IsRead)
                .ToListAsync();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { MarkedAsRead = unreadMessages.Count });
        }

    }
}
