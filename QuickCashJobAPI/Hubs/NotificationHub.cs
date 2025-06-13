using Microsoft.AspNetCore.SignalR;

namespace QuickCashJobAPI.Hubs
{
    public class NotificationHub : Hub
    {
        // Method to send notification to a specific user by userId
        public async Task SendNotification(string userId, string title, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", title, message);
        }
    }
}
