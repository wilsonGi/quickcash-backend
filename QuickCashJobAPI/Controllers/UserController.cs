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
                    user.IsDeleted,
                    user.IsApproved,
                    user.IsAdmin,
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
                user.IsDeleted,
                user.IsAdmin,
                user.TrialEndDate,
                user.IsSubscriptionActive,
            }).ToList();

            return Ok(userList);
        }


        [Authorize]
        [HttpPost("update-profile-photo/{userId}")]
        public async Task<IActionResult> UpdateProfilePhoto(string userId, IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
                return BadRequest("No image uploaded.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            using (var ms = new MemoryStream())
            {
                await photo.CopyToAsync(ms);
                user.ProfilePhoto = ms.ToArray();
            }

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile photo updated successfully." });
        }




        [HttpGet("profile-photo/{userId}")]
        public async Task<IActionResult> GetProfilePhoto(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.ProfilePhoto == null) return NotFound();

            return File(user.ProfilePhoto, "image/jpeg");
        }


        private void EnsureUserDates(ApplicationUser user)
        {
            user.LastTaskDoneDate = user.LastTaskDoneDate == default ? DateTime.UtcNow : user.LastTaskDoneDate;
            user.LastTaskEmployedDate = user.LastTaskEmployedDate == default ? DateTime.UtcNow : user.LastTaskEmployedDate;
            user.DateJoined = user.DateJoined == default ? DateTime.UtcNow : user.DateJoined;
            user.TrialEndDate = user.TrialEndDate == default ? DateTime.UtcNow.AddDays(14) : user.TrialEndDate;
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
                // ✅ Fetch user + FreeTrial plan in parallel
                var userTask = _userManager.FindByIdAsync(userId);
                var planTask = _context.SubscriptionPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Type == SubscriptionTier.FreeTrial);

                await Task.WhenAll(userTask, planTask);

                var user = userTask.Result;
                var trialPlan = planTask.Result;

                if (user == null)
                    return NotFound(new { message = "User not found." });

                if (user.IsApproved)
                    return BadRequest(new { message = "User is already approved." });

                if (trialPlan == null)
                    return StatusCode(500, new { message = "Trial plan not configured." });

                // ✅ Activate trial properly
                var now = DateTime.UtcNow;
                user.IsApproved = true;
                user.IsSubscriptionActive = true;
                user.CurrentPlanId = trialPlan.Id;
                user.SubscriptionStartDate = now;
                user.SubscriptionEndDate = now.AddDays(trialPlan.DurationDays > 0 ? trialPlan.DurationDays : 7);
                user.TrialEndDate = user.SubscriptionEndDate!.Value;

                user.LastTaskDoneDate = user.LastTaskDoneDate == default ? now : user.LastTaskDoneDate;
                user.LastTaskEmployedDate = user.LastTaskEmployedDate == default ? now : user.LastTaskEmployedDate;
                user.DateJoined = user.DateJoined == default ? now : user.DateJoined;
                user.Name ??= "User";
                user.Location ??= "Unknown";

                // ✅ Save trial record using app DbContext only
                if (!await _context.TrialRecords.AnyAsync(r => r.Email == user.Email))
                {
                    _context.TrialRecords.Add(new TrialRecord
                    {
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        DeviceId = user.DeviceId,
                        UsedAt = now
                    });
                    await _context.SaveChangesAsync();
                }

                // ✅ Update user using IdentityDbContext (correct way)
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"User update failed: {errors}" });
                }

                // ✅ Send email (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailSender.SendEmailAsync(
                            user.Email,
                            "🎉 Welcome to Splxit Jobs – Your Free Trial Has Begun!",
                            $@"
                    <html>
                    <body style='font-family: Arial;'>
                        <h2>Welcome, {user.Name}!</h2>
                        <p>Your free trial ends on <b>{user.TrialEndDate:dddd, MMM dd, yyyy}</b>.</p>
                        <p>Start exploring jobs at <a href='https://job.splxit.com'>job.splxit.com</a></p>
                    </body>
                    </html>"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send approval email to {Email}", user.Email);
                    }
                });

                return Ok(new { message = "✅ User approved successfully and trial activated." });
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical error in ApproveUser for userId: {UserId}", userId);
                return StatusCode(500, new { message = $"Unexpected error: {ex.Message}" });
            }
        }




        //[HttpPost("ApproveUser/{userId}")]
        //public async Task<IActionResult> ApproveUser(string userId)
        //{
        //    try
        //    {
        //        // ✅ Fetch user + FreeTrial plan in parallel
        //        var userTask = _userManager.FindByIdAsync(userId);
        //        var planTask = _context.SubscriptionPlans
        //            .AsNoTracking()
        //            .FirstOrDefaultAsync(p => p.Type == SubscriptionTier.FreeTrial);

        //        await Task.WhenAll(userTask, planTask);

        //        var user = userTask.Result;
        //        var trialPlan = planTask.Result;

        //        if (user == null)
        //            return NotFound(new { message = "User not found." });

        //        if (user.IsApproved)
        //            return BadRequest(new { message = "User is already approved." });

        //        if (trialPlan == null)
        //            return StatusCode(500, new { message = "Trial plan not configured." });

        //        // ✅ Activate trial properly (fix for expired issue)
        //        var now = DateTime.UtcNow;
        //        user.IsApproved = true;
        //        user.IsSubscriptionActive = true;
        //        user.CurrentPlanId = trialPlan.Id;
        //        user.SubscriptionStartDate = now;
        //        user.SubscriptionEndDate = now.AddDays(trialPlan.DurationDays > 0 ? trialPlan.DurationDays : 7);
        //        user.TrialEndDate = user.SubscriptionEndDate!.Value;

        //        user.LastTaskDoneDate = user.LastTaskDoneDate == default ? now : user.LastTaskDoneDate;
        //        user.LastTaskEmployedDate = user.LastTaskEmployedDate == default ? now : user.LastTaskEmployedDate;
        //        user.DateJoined = user.DateJoined == default ? now : user.DateJoined;

        //        user.Name ??= "User";
        //        user.Location ??= "Unknown";

        //        // ✅ Insert trial record (ignore duplicates)
        //        if (!_context.TrialRecords.Any(r => r.Email == user.Email))
        //        {
        //            _context.TrialRecords.Add(new TrialRecord
        //            {
        //                Email = user.Email,
        //                PhoneNumber = user.PhoneNumber,
        //                DeviceId = user.DeviceId
        //            });
        //        }

        //        // ✅ Batch update (faster)
        //        await _context.SaveChangesAsync();

        //        // ✅ Update IdentityUser (sync in one go)
        //        await _userManager.UpdateAsync(user);

        //        // ⚡ Fire-and-forget email to avoid slowing request
        //        _ = Task.Run(async () =>
        //        {
        //            try
        //            {
        //                await _emailSender.SendEmailAsync(
        //                    user.Email,
        //                    "🎉 Welcome to Splxit Jobs – Your Free Trial Has Begun!",
        //                    $@"
        //            <html>
        //            <body style='font-family: Arial;'>
        //                <h2>Welcome, {user.Name}!</h2>
        //                <p>Your free trial ends on <b>{user.TrialEndDate:dddd, MMM dd, yyyy}</b>.</p>
        //                <p>Start exploring jobs at <a href='https://job.splxit.com'>job.splxit.com</a></p>
        //            </body>
        //            </html>"
        //                );
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogWarning(ex, "Failed to send email to {Email}", user.Email);
        //            }
        //        });

        //        return Ok(new { message = "✅ User approved successfully and trial activated." });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogCritical(ex, "Critical error in ApproveUser for userId: {UserId}", userId);
        //        return StatusCode(500, new { message = $"Unexpected error: {ex.Message}" });
        //    }
        //}



        [HttpPost("DisapproveUser/{userId}")]
        public async Task<IActionResult> DisapproveUser(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            EnsureUserDates(user);

            user.IsApproved = false;
            user.IsDeleted = true;
            user.IsSubscriptionActive = false;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Fire-and-forget email
            _ = Task.Run(() => _emailSender.SendEmailAsync(
                user.Email,
                "❌ Your Splxit Jobs Account Was Not Approved",
                $@"
        <p>Dear {user.UserName},</p>
        <p>Thank you for your interest in <strong>Splxit Jobs</strong>.</p>
        <p>After review, your account was <strong>not approved</strong> at this time.</p>
        <p>If you wish to appeal, contact <a href='mailto:support@splxit.com'>support@splxit.com</a>.</p>
        <p>— Splxit Jobs Team</p>"
            ));

            return Ok("User disapproved and background email triggered.");
        }



        [HttpPost("BlockUser/{userId}")]
        public async Task<IActionResult> BlockUser(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            EnsureUserDates(user);
            user.IsBlocked = true;
            user.IsSubscriptionActive = false;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _ = Task.Run(() => _emailSender.SendEmailAsync(
                user.Email,
                "⚠️ Your Splxit Jobs Account Has Been Blocked",
                $@"
        <p>Dear {user.UserName},</p>
        <p>Your <strong>Splxit Jobs</strong> account has been <strong>temporarily blocked</strong>.</p>
        <p>If this was a mistake, contact <a href='mailto:support@splxit.com'>support@splxit.com</a>.</p>
        <p>— Splxit Jobs Compliance Team</p>"
            ));

            return Ok("User blocked and background email triggered.");
        }

        [HttpPost("UnblockUser/{userId}")]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            EnsureUserDates(user);
            user.IsBlocked = false;
            user.LockoutEnd = null;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _ = Task.Run(() => _emailSender.SendEmailAsync(
                user.Email,
                "✅ Your Splxit Jobs Account Access Has Been Restored",
                $@"
        <p>Dear {user.UserName},</p>
        <p>Your <strong>Splxit Jobs</strong> account has been <strong>unblocked</strong>.</p>
        <p>You can now log in and continue using your account normally.</p>
        <p>Thank you for your patience.</p>
        <p>— Splxit Jobs Support Team</p>"
            ));

            return Ok("User unblocked and background email triggered.");
        }

        [HttpDelete("DeleteUser/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            EnsureUserDates(user);
            user.IsDeleted = true;
            user.IsSubscriptionActive = false;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("User soft-deleted successfully.");
        }


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
