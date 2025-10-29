using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Models.DTO;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;
using System;

namespace QuickCashJobAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public DashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        [Authorize]
        [HttpGet("UserDetails")]
        public async Task<IActionResult> GetUserDetails()
        {
            // ✅ Safely get user ID from JWT claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Invalid token: user ID missing." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            // ✅ Get subscription status from claim (fallback to user property)
            bool isSubscriptionActive = bool.TryParse(
                User.FindFirst("IsSubscriptionActive")?.Value, out var activeFromClaim)
                ? activeFromClaim
                : user.IsSubscriptionActive;

            // ✅ Count unread chat messages (adjust field name if needed)
            var chatCount = await _db.ChatMessages
                .CountAsync(c => c.ReceiverId == userId && !c.IsRead);

            // ✅ Prepare dashboard data safely
            var dashboardDTO = new DashboardDTO
            {
                UserName = user.UserName,
                NumberOfTasksEmployed = user.NumberOfTasksEmployed,
                NumberOfTasksCompleted = user.NumberOfTasksCompleted,
                UserRating = user.UserRating,
                Location = user.Location ?? "Not specified",
                PhoneNumber = user.PhoneNumber ?? "Not available",
                LastTaskDoneDate = user.LastTaskDoneDate,
                LastTaskEmployedDate = user.LastTaskEmployedDate,
                DateJoined = user.DateJoined,
                IsSubscriptionActive = isSubscriptionActive,
                IsApproved = user.IsApproved,
                TrialEndDate = user.TrialEndDate,
                ProfilePhoto = user.ProfilePhoto != null
                    ? Convert.ToBase64String(user.ProfilePhoto)
                    : null,
                ChatCount = chatCount
            };

            return Ok(dashboardDTO);
        }
    }
}
