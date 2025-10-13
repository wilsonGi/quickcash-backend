using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;

namespace QuickCashJobAPI.Services
{
    public class SubscriptionCheckService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionCheckService> _logger;

        public SubscriptionCheckService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionCheckService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Subscription Check Service is running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckExpiredUsersAsync();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Run every hour
            }
        }

        private async Task CheckExpiredUsersAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.UtcNow;

                // Fetch all users whose trial has expired
                var expiredUsers = await dbContext.Users
                    .Where(u => u.TrialEndDate <= now) // timestamptz is UTC safe
                    .Where(u => !dbContext.UserRoles
                        .Join(dbContext.Roles,
                              ur => ur.RoleId,
                              r => r.Id,
                              (ur, r) => new { ur.UserId, r.Name })
                        .Any(x => x.UserId == u.Id && x.Name == SD.Role_Admin)) // skip admins
                    .ToListAsync();

                if (!expiredUsers.Any())
                {
                    _logger.LogInformation("No expired users found at {Time}", now);
                    return;
                }

                foreach (var user in expiredUsers)
                {
                    if (user.IsSubscriptionActive || user.IsApproved) // only update if needed
                    {
                        user.IsSubscriptionActive = false;
                       
                        _logger.LogInformation("Marked expired user inactive: {Email}", user.Email);
                    }
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expired subscriptions");
            }
        }
    }
}
