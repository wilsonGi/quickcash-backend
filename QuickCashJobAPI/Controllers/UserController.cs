using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Services;
using System.Text.Encodings.Web;
using System.Text;
using QuickCashJobAPI.Models.DTO;
using System.Security.Claims;

namespace QuickCashJobAPI.Controllers
{

    [Authorize(Policy = "AdminPolicy")]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;
        private readonly IMTNMoMoService _nfcMoService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;
        private readonly IEmailSender _emailSender;

        public UserController(UserManager<ApplicationUser> userManager,
            IUserService userService,
             ApplicationDbContext context,
             IEmailSender emailSender,
            ILogger<UserController> logger, IMTNMoMoService nfcMoService)
        {
            _userManager = userManager;
            _userService = userService;
            _context = context;
            _logger = logger;
            _nfcMoService = nfcMoService;
            _emailSender = emailSender;
        }

       

        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = _userManager.Users.ToList();

            var userList = new List<object>();
            foreach (var user in users)
            {
                userList.Add(new
                {
                    user.Id,  // 👈 Include the Id field here
                    user.Email,
                    user.Name,
                    user.Location,
                    user.NumberOfTasksCompleted,
                    user.NumberOfTasksEmployed,
                    user.LastTaskDoneDate,
                    user.LastTaskEmployedDate,
                    user.UserRating,
                    user.PhoneNumber,
                    user.IsBlocked,
                    user.IsApproved,
                    user.TrialEndDate,
                    user.IsSubscriptionActive,
                    ProfilePhoto = user.ProfilePhoto != null ? Convert.ToBase64String(user.ProfilePhoto) : null,

                });
            }

            return Ok(userList);
        }

        [HttpGet("GetActiveUsers")]
        public async Task<IActionResult> GetActiveUsers()
        {
            var today = DateTime.UtcNow;

            var activeUsers = _userManager.Users
                .Where(user => user.TrialEndDate > today)
                .ToList();

            var userList = activeUsers.Select(user => new
            {
                user.Id,
                user.Email,
                user.Name,
                user.Location,
                user.NumberOfTasksCompleted,
                user.NumberOfTasksEmployed,
                user.LastTaskDoneDate,
                user.LastTaskEmployedDate,
                user.UserRating,
                user.PhoneNumber,
                user.IsBlocked,
                user.IsApproved,
                user.TrialEndDate,
                user.IsSubscriptionActive,
            }).ToList();

            return Ok(userList);
        }


        [HttpGet("profile-photo/{userId}")]
        public async Task<IActionResult> GetProfilePhoto(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.ProfilePhoto == null) return NotFound();

            return File(user.ProfilePhoto, "image/jpeg");
        }



        [Authorize]
        [HttpGet("ExpiredSubscribers")]
        public async Task<IActionResult> GetExpiredSubscribers()
        {
            var expiredUsers = await _context.Users
                .Where(u => u.TrialEndDate <= DateTime.UtcNow)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Name,
                    u.TrialEndDate,
                    //u.NationalIdNo
                })
                .ToListAsync();

            return Ok(expiredUsers);
        }




        [HttpPost("ApproveUser/{userId}")]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    Console.WriteLine($"ApproveUser failed: No user found with ID {userId}.");
                    return NotFound("User not found.");
                }

                // Check if the user is already approved
                if (user.IsApproved)
                {
                    Console.WriteLine($"User {user.Email} is already approved.");
                    return BadRequest("User is already approved.");
                }

                // Set default values for critical fields if they're null or default
                user.IsApproved = true;
                user.IsSubscriptionActive = true;

                // Ensure that LastTaskDoneDate, LastTaskEmployedDate, DateJoined are populated if they're still default
                user.LastTaskDoneDate = user.LastTaskDoneDate == default
                    ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                    : DateTime.SpecifyKind(user.LastTaskDoneDate, DateTimeKind.Utc);

                user.LastTaskEmployedDate = user.LastTaskEmployedDate == default
                    ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                    : DateTime.SpecifyKind(user.LastTaskEmployedDate, DateTimeKind.Utc);

                user.DateJoined = user.DateJoined == default
                    ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                    : DateTime.SpecifyKind(user.DateJoined, DateTimeKind.Utc);
                user.TrialEndDate = DateTime.SpecifyKind(user.TrialEndDate, DateTimeKind.Utc);


                // Set default values for Name and Location if they're null or empty
                if (string.IsNullOrEmpty(user.Name))
                {
                    user.Name = "User"; // Or some fallback value
                }

                if (string.IsNullOrEmpty(user.Location))
                {
                    user.Location = "Unknown"; // Or some fallback value
                }

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    Console.WriteLine($"ApproveUser failed: UpdateAsync errors - {string.Join("; ", result.Errors.Select(e => e.Description))}");
                    return BadRequest(new { message = "Failed to approve user.", errors = result.Errors });
                }

                try
                {
                    // Generate email confirmation token
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = QueryHelpers.AddQueryString(
                        $"{Request.Scheme}://{Request.Host}/api/Account/ConfirmEmail",
                        new Dictionary<string, string?>
                        {
                    { "userId", user.Id },
                    { "code", code }
                        });

                    // Send approval email
                    await _emailSender.SendEmailAsync(user.Email, "You have been approved!",
                        $"Dear {user.UserName},<br><br>Congratulations! You have been approved and now have full access to the system.<br>" +
                        $"To confirm your email and activate your account, please <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>click here</a>.<br><br>" +
                        $"Welcome to the Splxit Creativity Arena and Rewards System!<br><br>Thank you!");

                    Console.WriteLine($"Approval email sent successfully to {user.Email}.");
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"ApproveUser warning: Failed to send approval email to {user.Email}. Error: {emailEx.Message}");
                    // Still approve the user even if email fails
                }

                Console.WriteLine($"User {user.Email} approved successfully.");
                return Ok(new { message = "User approved successfully, and a confirmation email has been sent (if possible)." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical error in ApproveUser: {ex.Message} - {ex.StackTrace}");
                return StatusCode(500, new { message = $"Unexpected error: {ex.Message}", stackTrace = ex.StackTrace });
            }
        }


        [HttpPost("DisapproveUser/{userId}")]
        public async Task<IActionResult> DisapproveUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Ensure all necessary date fields are populated
            user.LastTaskDoneDate = user.LastTaskDoneDate == default
                ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                : DateTime.SpecifyKind(user.LastTaskDoneDate, DateTimeKind.Utc);

            user.LastTaskEmployedDate = user.LastTaskEmployedDate == default
                ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                : DateTime.SpecifyKind(user.LastTaskEmployedDate, DateTimeKind.Utc);

            user.DateJoined = user.DateJoined == default
                ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                : DateTime.SpecifyKind(user.DateJoined, DateTimeKind.Utc);
            user.TrialEndDate = DateTime.SpecifyKind(user.TrialEndDate, DateTimeKind.Utc);


            user.IsApproved = false;
            user.IsDeleted = true;
            user.IsSubscriptionActive = false;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest("Failed to disapprove user.");
            }

            // Send disapproval email
            await _emailSender.SendEmailAsync(user.Email, "Your account has been disapproved",
                $"Dear {user.UserName},<br><br>We regret to inform you that your account approval request has been declined.<br>" +
                $"If you believe this is an error or need further clarification, please contact our support team.<br><br>" +
                $"Thank you for your understanding.");

            return Ok("User disapproved successfully, and a notification email has been sent.");
        }


        [HttpPost("BlockUser/{userId}")]
        public async Task<IActionResult> BlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            // Ensure critical date fields are populated
            user.LastTaskDoneDate = user.LastTaskDoneDate == default
                ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                : DateTime.SpecifyKind(user.LastTaskDoneDate, DateTimeKind.Utc);

            user.LastTaskEmployedDate = user.LastTaskEmployedDate == default
                ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                : DateTime.SpecifyKind(user.LastTaskEmployedDate, DateTimeKind.Utc);

            user.DateJoined = user.DateJoined == default
                ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                : DateTime.SpecifyKind(user.DateJoined, DateTimeKind.Utc);
            user.TrialEndDate = DateTime.SpecifyKind(user.TrialEndDate, DateTimeKind.Utc);


            user.IsBlocked = true;
            user.IsSubscriptionActive = false;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok("User blocked successfully.");
            }

            return BadRequest("Failed to block user.");
        }


        [HttpPost("UnblockUser/{userId}")]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            // Ensure critical date fields are populated
            user.LastTaskDoneDate = user.LastTaskDoneDate == default
                 ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                 : DateTime.SpecifyKind(user.LastTaskDoneDate, DateTimeKind.Utc);

            user.LastTaskEmployedDate = user.LastTaskEmployedDate == default
                ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                : DateTime.SpecifyKind(user.LastTaskEmployedDate, DateTimeKind.Utc);

            user.DateJoined = user.DateJoined == default
                ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                : DateTime.SpecifyKind(user.DateJoined, DateTimeKind.Utc);
            user.TrialEndDate = DateTime.SpecifyKind(user.TrialEndDate, DateTimeKind.Utc);


            user.IsBlocked = false;
            user.LockoutEnd = null;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok("User unblocked successfully.");
            }

            return BadRequest("Failed to unblock user.");
        }


        [HttpDelete("DeleteUser/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            // Ensure critical date fields are populated
            user.LastTaskDoneDate = user.LastTaskDoneDate == default
                 ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                 : DateTime.SpecifyKind(user.LastTaskDoneDate, DateTimeKind.Utc);

            user.LastTaskEmployedDate = user.LastTaskEmployedDate == default
                ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                : DateTime.SpecifyKind(user.LastTaskEmployedDate, DateTimeKind.Utc);

            user.DateJoined = user.DateJoined == default
                ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                : DateTime.SpecifyKind(user.DateJoined, DateTimeKind.Utc);
            user.TrialEndDate = DateTime.SpecifyKind(user.TrialEndDate, DateTimeKind.Utc);


            user.IsDeleted = true;
            user.IsSubscriptionActive = false;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("User soft-deleted successfully.");
        }



        //Mamual Subscriptiom Remewal With Buttom Click
        [HttpPost("renew-subscription/{userId}")]
        public async Task<IActionResult> RenewSubscription(string userId) // Change int to string
        {
            var user = await _context.Users.FindAsync(userId); // userId is now a string
            if (user == null)
            {
                _logger.LogError($"User not found: {userId}");
                return NotFound("User not found.");
            }

            // Extend the TrialEndDate by 30 days
            if (user.TrialEndDate >= DateTime.MaxValue.AddDays(-30))
            {
                user.TrialEndDate = DateTime.MaxValue;
            }
            else
            {
                user.TrialEndDate = user.TrialEndDate.AddDays(30);
            }

            user.IsSubscriptionActive = true; // Mark as subscribed
            user.IsApproved = true;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Subscription extended for User ID: {userId}. New TrialEndDate: {user.TrialEndDate}");
            return Ok(new { message = "Subscription extended by 30 days.", trialEndDate = user.TrialEndDate });
        }

    }
}
