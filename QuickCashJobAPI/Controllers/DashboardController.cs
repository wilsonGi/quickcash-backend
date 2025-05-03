using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Models.DTO;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;

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
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);


            if (user == null)
            {
                return NotFound();
            }

            bool isSubscriptionActive = bool.Parse(User.FindFirst("IsSubscriptionActive")?.Value ?? "false");
            // 👇 Count chats for this user (adjust field name if needed)
            var chatCount = await _db.ChatMessages
            .CountAsync(c => c.ReceiverId == userId && !c.IsRead);




            var dashboardDTO = new DashboardDTO
            {
                UserName = user.UserName,
                NumberOfTasksEmployed = user.NumberOfTasksEmployed,
                NumberOfTasksCompleted = user.NumberOfTasksCompleted,
                UserRating = user.UserRating,
                Location = user.Location,
                LastTaskDoneDate = user.LastTaskDoneDate,
                LastTaskEmployedDate = user.LastTaskEmployedDate,
                DateJoined = user.DateJoined,
                IsSubscriptionActive = user.IsSubscriptionActive, // Include in response
                IsApproved = user.IsApproved,
                TrialEndDate = user.TrialEndDate,
                ProfilePhoto = user.ProfilePhoto != null ? Convert.ToBase64String(user.ProfilePhoto) : null,
                ChatCount = chatCount // 👈 Include it here


            };

            return Ok(dashboardDTO);
        }
    }
}
