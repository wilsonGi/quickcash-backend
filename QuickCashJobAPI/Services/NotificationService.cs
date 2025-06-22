using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.SignalR;
using QuickCashJobAPI.Hubs; // ✅ Correct namespace for NotificationHub

namespace QuickCashJobAPI.Services
{
    public class NotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IHttpClientFactory httpClientFactory, IConfiguration config,
            IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendCombinedNotificationAsync(string userId, string fcmToken, string title, string body)
        {
            if (!string.IsNullOrWhiteSpace(fcmToken))
            {
                await SendNotificationAsync(fcmToken, title, body);
            }
            await SendSignalRNotificationAsync(userId, title, body);
        }

        public async Task SendSignalRNotificationAsync(string userId, string title, string body)
        {
            try
            {
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", title, body);
                _logger.LogInformation("📡 SignalR notification sent to user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending SignalR notification to user {UserId}", userId);
            }
        }

        public async Task SendNotificationAsync(string fcmToken, string title, string body)
        {
            if (string.IsNullOrWhiteSpace(fcmToken)) return;

            var message = new Message()
            {
                Token = fcmToken,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = title,
                    Body = body
                },
                Android = new AndroidConfig
                {
                    Priority = Priority.High
                },
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        ContentAvailable = true
                    }
                }
            };

            try
            {
                var result = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("✅ FCM notification sent: {Result}", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending FCM notification");
            }
        }
    }
}
