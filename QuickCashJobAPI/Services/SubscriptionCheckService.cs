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
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var now = DateTime.UtcNow;

                        // Fetch users whose trial period has expired
                        var expiredUsers = await dbContext.Users
                            .Where(u => u.TrialEndDate <= now && u.IsSubscriptionActive)
                            .ToListAsync();

                        if (expiredUsers.Any())
                        {
                            foreach (var user in expiredUsers)
                            {
                                user.IsSubscriptionActive = false;
                                user.IsApproved = false;
                                _logger.LogInformation($"Subscription expired for user: {user.Email}");
                            }

                            await dbContext.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error checking subscriptions: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Runs once every 24 hours
            }
        }
    }
}
