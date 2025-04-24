using Microsoft.AspNetCore.Identity;
using QuickCashJobAPI.Models;

namespace QuickCashJobAPI.Services
{
    public class UserService : IUserService
    {

        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }


        //MOMO IMTEGRATIOM
        public async Task ActivateSubscriptionAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsSubscriptionActive = true;
                await _userManager.UpdateAsync(user);
            }
        }


        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<bool> IsSubscriptionActiveAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsSubscriptionActive ?? false;
        }

        public async Task<bool> IsTrialExpiredAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user != null && user.TrialEndDate <= DateTime.UtcNow && !user.IsSubscriptionActive;
        }
    

        public Task UpdateUserLocationAsync(string userId, double latitude, double longitude)
        {
            throw new NotImplementedException();
        }

        /// EMDS


        //public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        //{
        //    return await _userManager.FindByIdAsync(userId);
        //}

        //public async Task UpdateUserLocationAsync(string userId, double latitude, double longitude)
        //{
        //    var user = await _userManager.FindByIdAsync(userId);
        //    if (user == null) throw new Exception("User not found");

        //    user.Latitude = latitude;
        //    user.Longitude = longitude;

        //    await _userManager.UpdateAsync(user);
        //}


        //public async Task<IEnumerable<Location>> GetOtherUsersLocationsAsync()
        //{
        //    // Assuming Location is a type containing the latitude and longitude
        //    return await _dbContext.Users
        //        .Select(u => new Location
        //        {
        //            Latitude = u.Latitude,
        //            Longitude = u.Longitude
        //        })
        //        .ToListAsync();
        //}

    }
}
