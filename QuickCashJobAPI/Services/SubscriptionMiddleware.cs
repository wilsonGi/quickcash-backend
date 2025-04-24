using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace QuickCashJobAPI.Services
{
    public class SubscriptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public SubscriptionMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the user is an admin
            var isAdmin = context.User.IsInRole("Admin");

            if (!isAdmin)
            {
                // Only proceed with subscription check if the user is not an admin
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId != null)//If there is a user with such id
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                        // Check if the trial is expired for regular users
                        if (await userService.IsTrialExpiredAsync(userId))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsync("Trial expired. Please subscribe.");
                            return;
                        }
                    }
                }
            }

            // Proceed with the next middleware
            await _next(context);
        }
    }
}
