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

                // ✅ Activate trial properly (fix for expired issue)
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

                // ✅ Insert trial record (ignore duplicates)
                if (!_context.TrialRecords.Any(r => r.Email == user.Email))
                {
                    _context.TrialRecords.Add(new TrialRecord
                    {
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        DeviceId = user.DeviceId
                    });
                }

                // ✅ Batch update (faster)
                await _context.SaveChangesAsync();

                // ✅ Update IdentityUser (sync in one go)
                await _userManager.UpdateAsync(user);

                // ⚡ Fire-and-forget email to avoid slowing request
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
                        _logger.LogWarning(ex, "Failed to send email to {Email}", user.Email);
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






        // ✅ APPROVE USER ENDPOINT
        //[HttpPost("ApproveUser/{userId}")]
        //public async Task<IActionResult> ApproveUser(string userId)
        //{
        //    try
        //    {
        //        var user = await _userManager.FindByIdAsync(userId);
        //        if (user == null)
        //        {
        //            _logger.LogWarning("ApproveUser failed: No user found with ID {UserId}", userId);
        //            return NotFound(new { message = "User not found." });
        //        }

        //        if (user.IsApproved)
        //        {
        //            _logger.LogInformation("User {Email} is already approved.", user.Email);
        //            return BadRequest(new { message = "User is already approved." });
        //        }

        //        // ✅ Fetch trial plan
        //        var trialPlan = await _context.SubscriptionPlans
        //            .FirstOrDefaultAsync(p => p.Type == SubscriptionTier.FreeTrial);

        //        if (trialPlan == null)
        //            return StatusCode(500, new { message = "Trial subscription plan not configured. Please contact support." });

        //        // ✅ Activate user and start trial
        //        user.IsApproved = true;
        //        user.IsSubscriptionActive = true;
        //        user.CurrentPlanId = trialPlan.Id;
        //        user.SubscriptionStartDate = DateTime.UtcNow;
        //        user.SubscriptionEndDate = DateTime.UtcNow.AddDays(7);
        //        user.TrialEndDate = DateTime.UtcNow.AddDays(7);

        //        // Ensure essential fields are valid
        //        user.LastTaskDoneDate = user.LastTaskDoneDate == default
        //            ? DateTime.UtcNow
        //            : user.LastTaskDoneDate;
        //        user.LastTaskEmployedDate = user.LastTaskEmployedDate == default
        //            ? DateTime.UtcNow
        //            : user.LastTaskEmployedDate;
        //        user.DateJoined = user.DateJoined == default
        //            ? DateTime.UtcNow
        //            : user.DateJoined;

        //        if (string.IsNullOrEmpty(user.Name)) user.Name = "User";
        //        if (string.IsNullOrEmpty(user.Location)) user.Location = "Unknown";

        //        // ✅ Save trial record permanently
        //        _context.TrialRecords.Add(new TrialRecord
        //        {
        //            Email = user.Email,
        //            PhoneNumber = user.PhoneNumber,
        //            DeviceId = user.DeviceId
        //        });

        //        await _userManager.UpdateAsync(user);
        //        await _context.SaveChangesAsync();

        //        // ✅ Send approval + activation email
        //        // ✅ Send detailed welcome + educational email after approval
        //        try
        //        {
        //            await _emailSender.SendEmailAsync(
        //                user.Email,
        //                "🎉 Welcome to Splxit Jobs – Your Free Trial Has Begun!",
        //                $@"
        //<html>
        //<body style='font-family: Arial, sans-serif; color: #333;'>
        //    <h2 style='color: #2d89ef;'>Welcome to Splxit Jobs, {user.Name}!</h2>
        //    <p>Your account has been <strong>approved</strong> 🎉</p>
        //    <p>Your 7-day <strong>free trial</strong> has begun and will end on <strong>{user.TrialEndDate:dddd, MMM dd, yyyy}</strong>.</p>

        //    <hr>

        //    <h3>🌍 What We Do at Splxit Jobs</h3>

        //    <p><strong>1. The Job Completion Cycle on Splxit Jobs</strong><br>
        //    Splxit Jobs makes job posting and completion easy and transparent. A user posts a job, another commits to it, and the poster approves. Once the contractor confirms and completes the job, both parties gain credibility with improved ratings and job counts. This cycle ensures accountability, fairness, and trust between job creators and job seekers.</p>

        //    <p><strong>2. Why Everyone Wins on Splxit Jobs</strong><br>
        //    On Splxit Jobs, both the job poster and the job provider benefit. Posters get their tasks done quickly, while contractors earn income. At the end of each completed job, both sides see their ratings improve and their credibility increase. The more jobs you complete or post, the stronger your reputation becomes on the platform.</p>

        //    <p><strong>3. Commit, Approve, Confirm – How Jobs Get Done</strong><br>
        //    When you see a job you like on Splxit Jobs, click “Commit.” The job poster reviews all who committed and approves just one contractor. Once approved, you confirm, complete the job, and both sides win. This three-step process (Commit → Approve → Confirm) keeps the system fair and efficient.</p>

        //    <p><strong>4. Small Jobs Matter Too!</strong><br>
        //    Don’t feel shy to post small tasks. On Splxit Jobs, you can post anything from watering flowers, washing a car, to walking a dog, even if you’re paying just GHC10. Every job matters because it puts money in someone’s pocket and helps you get things done. No job is too small.</p>

        //    <p><strong>5. Your Dashboard, Your Control</strong><br>
        //    On your Splxit Jobs dashboard, you can track all your jobs. Filter jobs by status—active, inactive, committed, approved, confirmed, or completed. See how many people viewed your job, approve contractors, and manage your work from one place. It’s your control center for all activities.</p>

        //    <p><strong>6. Building Trust Through Ratings</strong><br>
        //    Every time you complete a job, your rating goes up. Job seekers get a percentage boost, and job posters build credibility by completing tasks successfully. The more jobs you do, the higher your profile credibility. Trust is earned, and Splxit Jobs helps you build it with every completed task.</p>

        //    <p><strong>7. Anonymity and Privacy First</strong><br>
        //    We understand the need for privacy. On Splxit Jobs, your phone number is hidden by default when posting jobs. You can choose to show it or keep it private. Instead, use the built-in chat to collaborate with contractors safely and conveniently.</p>

        //    <p><strong>8. Advertise Your Skills and Services</strong><br>
        //    Splxit Jobs is not just about posting jobs—it’s about showcasing what you can do. Add your skills to your profile and update them regularly. Whether you’re a plumber, graphic designer, cleaner, or tutor, your skills make you visible to those who need your services.</p>

        //    <p><strong>9. Multiple Contractors, One Approval</strong><br>
        //    When you post a job, multiple people may commit to it. You’ll see all their details, but you can approve only one contractor. If the approved contractor delays or refuses to confirm, you can disapprove them and choose someone else. This system ensures flexibility without compromising trust.</p>

        //    <p><strong>10. The Purpose of Splxit Jobs</strong><br>
        //    Splxit Jobs exists to solve one problem: no one should go hungry or broke. By allowing people to post tasks and get them done for money—no matter how small—we create daily, weekly, and instant earning opportunities. Whether you’re seeking work or needing a service, Splxit Jobs connects you quickly.</p>

        //    <hr>
        //    <p>Welcome aboard! You can now explore opportunities at <a href='https://job.splxit.com'>job.splxit.com</a>.</p>
        //    <p style='font-size: 12px; color: #777;'>This message was sent from no-reply@job.splxit.com</p>
        //</body>
        //</html>");
        //        }
        //        catch (Exception emailEx)
        //        {
        //            _logger.LogWarning(emailEx, "Failed to send approval email to {Email}", user.Email);
        //        }


        //        _logger.LogInformation("User {Email} approved successfully.", user.Email);
        //        return Ok(new { message = "User approved successfully and trial activated." });
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
            await _emailSender.SendEmailAsync(
                 user.Email,
                 "Application Update – Your Splxit Jobs Account Was Not Approved",
                 $@"
                <p>Dear {user.UserName},</p>
                <p>Thank you for your interest in joining <strong>Splxit Jobs</strong>.</p>
                <p>After reviewing your application, we regret to inform you that it has not been approved at this time.</p>
                <p>If you believe this is an error or would like to appeal, please reach out to our support team at 
                <a href='mailto:support@splxit.com'>support@splxit.com</a>.</p>
                <p>We appreciate your understanding.</p>
                <p>Sincerely,<br><strong>The Splxit Jobs Team</strong><br><a href='https://job.splxit.com'>job.splxit.com</a></p>
                "
             );

            return Ok("User disapproved successfully, and a notification email has been sent.");
        }


        [HttpPost("BlockUser/{userId}")]
        public async Task<IActionResult> BlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            // Ensure critical date fields are in UTC
            user.LastTaskDoneDate = user.LastTaskDoneDate == default
                ? DateTime.UtcNow
                : DateTime.SpecifyKind(user.LastTaskDoneDate, DateTimeKind.Utc);

            user.LastTaskEmployedDate = user.LastTaskEmployedDate == default
                ? DateTime.UtcNow
                : DateTime.SpecifyKind(user.LastTaskEmployedDate, DateTimeKind.Utc);

            user.DateJoined = user.DateJoined == default
                ? DateTime.UtcNow
                : DateTime.SpecifyKind(user.DateJoined, DateTimeKind.Utc);

            user.TrialEndDate = user.TrialEndDate == default
                ? DateTime.UtcNow.AddDays(14) // example fallback
                : DateTime.SpecifyKind(user.TrialEndDate, DateTimeKind.Utc);

            user.IsBlocked = true;
            user.IsSubscriptionActive = false;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest("Failed to block user.");
            }

            await _emailSender.SendEmailAsync(
                 user.Email,
                 "⚠️ Your Splxit Jobs Account Has Been Blocked",
                 $@"
                <p>Dear {user.UserName},</p>
                <p>We regret to inform you that your <strong>Splxit Jobs</strong> account has been <strong>temporarily blocked</strong> due to a violation of our community guidelines or suspicious activity.</p>
                <p>If you believe this was a mistake or wish to discuss further, please contact us at 
                <a href='mailto:support@splxit.com'>support@splxit.com</a>.</p>
                <p>Thank you for your cooperation and understanding.</p>
                <p>Best regards,<br><strong>The Splxit Jobs Compliance Team</strong><br><a href='https://job.splxit.com'>job.splxit.com</a></p>
                "
             );


            return Ok("User has been successfully blocked and notified via email.");
        }

        [HttpPost("UnblockUser/{userId}")]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            // Ensure critical date fields are in UTC
            user.LastTaskDoneDate = user.LastTaskDoneDate == default
                ? DateTime.UtcNow
                : DateTime.SpecifyKind(user.LastTaskDoneDate, DateTimeKind.Utc);

            user.LastTaskEmployedDate = user.LastTaskEmployedDate == default
                ? DateTime.UtcNow
                : DateTime.SpecifyKind(user.LastTaskEmployedDate, DateTimeKind.Utc);

            user.DateJoined = user.DateJoined == default
                ? DateTime.UtcNow
                : DateTime.SpecifyKind(user.DateJoined, DateTimeKind.Utc);

            user.TrialEndDate = user.TrialEndDate == default
                ? DateTime.UtcNow.AddDays(14)
                : DateTime.SpecifyKind(user.TrialEndDate, DateTimeKind.Utc);

            user.IsBlocked = false;
            user.LockoutEnd = null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest("Failed to unblock user.");
            }

                await _emailSender.SendEmailAsync(
                user.Email,
                "✅ Your Splxit Jobs Account Access Has Been Restored",
                $@"
                <p>Dear {user.UserName},</p>
                <p>Good news! Your <strong>Splxit Jobs</strong> account has been <strong>unblocked</strong> and your access has been fully restored.</p>
                <p>You can now continue to use your account to browse, post, and manage job opportunities.</p>
                <p>Thank you for your patience.</p>
                <p>Warm regards,<br><strong>The Splxit Jobs Support Team</strong><br><a href='https://job.splxit.com'>job.splxit.com</a></p>
                "
            );

            return Ok("User has been successfully unblocked and notified via email.");
        }

        [HttpDelete("DeleteUser/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            // Ensure critical date fields are in UTC
            user.LastTaskDoneDate = user.LastTaskDoneDate == default
                ? DateTime.UtcNow
                : DateTime.SpecifyKind(user.LastTaskDoneDate, DateTimeKind.Utc);

            user.LastTaskEmployedDate = user.LastTaskEmployedDate == default
                ? DateTime.UtcNow
                : DateTime.SpecifyKind(user.LastTaskEmployedDate, DateTimeKind.Utc);

            user.DateJoined = user.DateJoined == default
                ? DateTime.UtcNow
                : DateTime.SpecifyKind(user.DateJoined, DateTimeKind.Utc);

            user.TrialEndDate = user.TrialEndDate == default
                ? DateTime.UtcNow.AddDays(14)
                : DateTime.SpecifyKind(user.TrialEndDate, DateTimeKind.Utc);

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
