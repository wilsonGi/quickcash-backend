using QuickCashJobAPI.Models;

namespace QuickCashJobAPI.Services
{
    public interface IUserService
    {
        Task<ApplicationUser> GetUserByIdAsync(string userId);
        Task UpdateUserLocationAsync(string userId, double latitude, double longitude);
        Task ActivateSubscriptionAsync(string userId);
        Task<bool> IsSubscriptionActiveAsync(string userId);
        Task<bool> IsTrialExpiredAsync(string userId);

        public interface IUserService
        {
            //Task<IEnumerable<Location>> GetOtherUsersLocationsAsync();
        }

    }
}
