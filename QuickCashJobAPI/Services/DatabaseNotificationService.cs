using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace QuickCashJobAPI.Services
{
    public class DatabaseNotificationService
    {
        private readonly ApplicationDbContext _db;

        public DatabaseNotificationService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Create notifications for multiple users
        public async Task CreateNotificationsAsync(IEnumerable<string> userIds, string title, string message, int? jobId = null, int? chatMessageId = null)
        {
            var notifications = userIds.Select(uid => new Notification
            {
                UserId = uid,
                Title = title,
                Message = message,
                JobId = jobId,
                ChatMessageId = chatMessageId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            }).ToList();

            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync();
        }
    }
}
