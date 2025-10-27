using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Models.DTO;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace QuickCashJobAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, 
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<AuthController> logger,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db,
            SignInManager<ApplicationUser> signInManager)
        {
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _db = db;
            _logger = logger;
            _signInManager = signInManager;
        }



        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { message = "Invalid login data." });

                var user = await _userManager.FindByEmailAsync(loginModel.Email);
                if (user == null || !await _userManager.CheckPasswordAsync(user, loginModel.Password))
                {
                    return Unauthorized(new { message = "Invalid email or password." });
                }

                if (!user.IsApproved)
                {
                    return Unauthorized(new { message = "Your account has not been approved yet. Please wait for admin approval." });
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                var isAdmin = userRoles.Contains("Admin");
                var isSubscriptionActive = user.TrialEndDate > DateTime.UtcNow;
                var isApproved = user.IsApproved;

                // Generate tokens
                var refreshToken = GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userManager.UpdateAsync(user);

                var token = GenerateJwtToken(user, isAdmin, isSubscriptionActive, isApproved);

                return Ok(new
                {
                    UserId = user.Id,
                    Token = token,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiry = user.RefreshTokenExpiryTime,
                    UserName = user.Name,
                    UserEmail = user.Email,
                    IsAdmin = isAdmin,
                    IsSubscriptionActive = isSubscriptionActive,
                    IsApproved = isApproved
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt for {Email}", loginModel.Email);

                // ⚠️ Temporary detailed error response (only for debugging)
                return StatusCode(500, new
                {
                    message = ex.Message,
                    stack = ex.StackTrace
                });
            }
        }


        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }


        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenApiModel tokenModel)
        {
            if (tokenModel is null)
                return BadRequest("Invalid client request");

            var principal = GetPrincipalFromExpiredToken(tokenModel.AccessToken);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.RefreshToken != tokenModel.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized("Invalid refresh token or expired.");
            }

            var newAccessToken = GenerateJwtToken(user, user.IsAdmin, user.TrialEndDate > DateTime.UtcNow, user.IsApproved);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }



        // ✅ USER REGISTRATION ENDPOINT (Optimized)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterModel registerModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errorMessages = string.Join("; ", ModelState.Values
                        .SelectMany(x => x.Errors)
                        .Select(x => x.ErrorMessage));
                    return BadRequest(new { message = errorMessages });
                }

                // ✅ Preload read-only queries with AsNoTracking()
                var emailExists = await _db.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Email == registerModel.Email);

                if (emailExists)
                    return BadRequest(new { message = "This email is already registered." });

                var phoneExists = await _db.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.PhoneNumber == registerModel.PhoneNumber);

                if (phoneExists)
                    return BadRequest(new { message = "This phone number is already registered." });

                var deviceExists = await _db.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.DeviceId == registerModel.DeviceId);

                if (deviceExists)
                    return BadRequest(new { message = "Registration from this device is already used." });

                // ✅ Trial check (fast if indexes exist)
                bool hasTrialBefore = await _db.TrialRecords
                    .AsNoTracking()
                    .AnyAsync(r =>
                        r.Email == registerModel.Email ||
                        r.PhoneNumber == registerModel.PhoneNumber ||
                        r.DeviceId == registerModel.DeviceId);

                if (hasTrialBefore)
                {
                    return BadRequest(new { message = "You have already used a free trial. Please subscribe or choose PAYG." });
                }

                // ✅ Validate profile photo (simple)
                if (registerModel.ProfilePhoto != null)
                {
                    if (registerModel.ProfilePhoto.Length > 5 * 1024 * 1024)
                        return BadRequest(new { message = "File size too large. Max allowed size is 5MB" });

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(registerModel.ProfilePhoto.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                        return BadRequest(new { message = "Invalid file type. Only JPG, JPEG, and PNG are allowed." });
                }

                // ✅ Create user
                var user = new ApplicationUser
                {
                    UserName = registerModel.Email,
                    Email = registerModel.Email,
                    Name = registerModel.Name,
                    Location = registerModel.Location,
                    PhoneNumber = registerModel.PhoneNumber,
                    DeviceId = registerModel.DeviceId,
                    NumberOfTasksCompleted = 0,
                    NumberOfTasksEmployed = 0,
                    LastTaskDoneDate = DateTime.SpecifyKind(registerModel.LastTaskDoneDate, DateTimeKind.Utc),
                    LastTaskEmployedDate = DateTime.SpecifyKind(registerModel.LastTaskEmployedDate, DateTimeKind.Utc),
                    DateJoined = DateTime.SpecifyKind(registerModel.DateJoined, DateTimeKind.Utc),

                    // 🚫 Not approved yet
                    IsAdmin = false,
                    IsApproved = false,
                    IsSubscriptionActive = false,
                    CurrentPlanId = null,
                    TrialEndDate = DateTime.MinValue,
                    SubscriptionStartDate = null,
                    SubscriptionEndDate = null
                };

                var result = await _userManager.CreateAsync(user, registerModel.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { message = errors });
                }

                // ✅ Handle photo after user creation
                if (registerModel.ProfilePhoto != null)
                {
                    using var memoryStream = new MemoryStream();
                    await registerModel.ProfilePhoto.CopyToAsync(memoryStream);
                    user.ProfilePhoto = memoryStream.ToArray();
                    await _userManager.UpdateAsync(user);
                }

                // ✅ Assign role
                var role = registerModel.IsAdmin ? "Admin" : "Customer";
                await _userManager.AddToRoleAsync(user, role);

                // ✅ Send email asynchronously (doesn't block)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailSender.SendEmailAsync(
                            user.Email,
                            "🎉 Registration Successful – Awaiting Approval | Splxit Jobs",
                            $@"
                    <p>Dear {user.Name},</p>
                    <p>Thank you for registering with <strong>Splxit Jobs</strong>!</p>
                    <p>Your account has been successfully created and is currently <strong>pending admin approval</strong>.</p>
                    <p>Once approved, you’ll receive an email confirming activation of your <strong>free trial</strong>.</p>
                    <p>Warm regards,<br><strong>The Splxit Jobs Team</strong><br><a href='https://job.splxit.com'>job.splxit.com</a></p>
                    "
                        );
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send registration email to {Email}", user.Email);
                    }
                });

                return Ok(new { message = "User registered successfully, pending approval." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration.");
                return StatusCode(500, new { message = ex.Message });
            }
        }




        // ✅ USER REGISTRATION ENDPOINT
        //[HttpPost("register")]
        //public async Task<IActionResult> Register([FromForm] RegisterModel registerModel)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            var errorMessages = string.Join("; ", ModelState.Values
        //                .SelectMany(x => x.Errors)
        //                .Select(x => x.ErrorMessage));
        //            return BadRequest(new { message = errorMessages });
        //        }

        //        // 🔹 Check for duplicate email, phone, and device
        //        if (await _userManager.FindByEmailAsync(registerModel.Email) != null)
        //            return BadRequest(new { message = "This email is already registered." });

        //        if (_userManager.Users.Any(u => u.PhoneNumber == registerModel.PhoneNumber))
        //            return BadRequest(new { message = "This phone number is already registered." });

        //        if (_userManager.Users.Any(u => u.DeviceId == registerModel.DeviceId))
        //            return BadRequest(new { message = "Registration from this device is already used." });

        //        // 🔹 Check if user already used trial
        //        bool hasTrialBefore = await _db.TrialRecords.AnyAsync(r =>
        //            r.Email == registerModel.Email ||
        //            r.PhoneNumber == registerModel.PhoneNumber ||
        //            r.DeviceId == registerModel.DeviceId);

        //        if (hasTrialBefore)
        //        {
        //            return BadRequest(new { message = "You have already used a free trial. Please subscribe or choose PAYG." });
        //        }

        //        // 🔹 Validate profile photo
        //        if (registerModel.ProfilePhoto != null)
        //        {
        //            if (registerModel.ProfilePhoto.Length > 5 * 1024 * 1024)
        //                return BadRequest(new { message = "File size too large. Max allowed size is 5MB" });

        //            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        //            var fileExtension = Path.GetExtension(registerModel.ProfilePhoto.FileName);
        //            if (!allowedExtensions.Contains(fileExtension.ToLower()))
        //                return BadRequest(new { message = "Invalid file type. Only JPG, JPEG, and PNG are allowed." });
        //        }

        //        // ✅ Create user with *no active subscription* yet
        //        var user = new ApplicationUser
        //        {
        //            UserName = registerModel.Email,
        //            Email = registerModel.Email,
        //            Name = registerModel.Name,
        //            Location = registerModel.Location,
        //            PhoneNumber = registerModel.PhoneNumber,
        //            DeviceId = registerModel.DeviceId,
        //            NumberOfTasksCompleted = 0,
        //            NumberOfTasksEmployed = 0,
        //            LastTaskDoneDate = DateTime.SpecifyKind(registerModel.LastTaskDoneDate, DateTimeKind.Utc),
        //            LastTaskEmployedDate = DateTime.SpecifyKind(registerModel.LastTaskEmployedDate, DateTimeKind.Utc),
        //            DateJoined = DateTime.SpecifyKind(registerModel.DateJoined, DateTimeKind.Utc),

        //            // 🚫 Trial and subscription not yet active
        //            IsAdmin = false,
        //            IsApproved = false,
        //            IsSubscriptionActive = false,
        //            CurrentPlanId = null,
        //            TrialEndDate = DateTime.MinValue,
        //            SubscriptionStartDate = null,
        //            SubscriptionEndDate = null
        //        };

        //        var result = await _userManager.CreateAsync(user, registerModel.Password);
        //        if (!result.Succeeded)
        //            return BadRequest(result.Errors);

        //        // 🔹 Process profile photo
        //        if (registerModel.ProfilePhoto != null)
        //        {
        //            using var memoryStream = new MemoryStream();
        //            await registerModel.ProfilePhoto.CopyToAsync(memoryStream);
        //            user.ProfilePhoto = memoryStream.ToArray();
        //            await _userManager.UpdateAsync(user);
        //        }

        //        // 🔹 Assign role
        //        var role = registerModel.IsAdmin ? "Admin" : "Customer";
        //        await _userManager.AddToRoleAsync(user, role);

        //        // 🔹 Send registration email (pending approval)
        //        try
        //        {
        //            await _emailSender.SendEmailAsync(
        //            user.Email,
        //            "🎉 Registration Successful – Awaiting Approval | Splxit Jobs",
        //            $@"
        //            <p>Dear {user.Name},</p>
        //            <p>Thank you for registering with <strong>Splxit Jobs</strong>!</p>
        //            <p>Your account has been successfully created and is currently <strong>pending admin approval</strong>.</p>
        //            <p>Once approved, you’ll receive an email confirming activation of your <strong>7-day free trial</strong> period.</p>
        //            <p>We’re excited to have you on board and can’t wait for you to start exploring opportunities on our platform.</p>
        //            <p>Warm regards,<br><strong>The Splxit Jobs Team</strong><br><a href='https://job.splxit.com'>job.splxit.com</a></p>
        //            "
        //        );
        //        }
        //        catch (Exception emailEx)
        //        {
        //            _logger.LogWarning(emailEx, "Failed to send registration email to {Email}", user.Email);
        //        }

        //        return Ok(new { message = "User registered successfully, pending approval." });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error during registration.");
        //        return StatusCode(500, new { message = ex.Message });
        //    }
        //}




        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Name,
                    u.Location,
                    u.NumberOfTasksCompleted,
                    u.NumberOfTasksEmployed,
                    u.LastTaskDoneDate,
                    u.LastTaskEmployedDate,
                    u.UserRating,
                    u.PhoneNumber,
                    u.DateJoined,
                    u.UserName,
                    u.IsBlocked,
                    u.IsDeleted,
                    u.IsAdmin,
                    u.IsSubscriptionActive,
                    u.IsApproved,
                    u.TrialEndDate,
                    // Optional: include skills or categories if needed
                })
                .FirstOrDefaultAsync();

            return Ok(user);
        }


        [HttpPost("social-login")]
        public async Task<IActionResult> SocialLogin([FromBody] SocialLoginDto model)
        {
            try
            {
                ApplicationUser user = null;
                string email = null;
                string name = null;

                // ✅ 1. Validate provider token
                if (model.Provider == "Google")
                {
                    var payload = await GoogleJsonWebSignature.ValidateAsync(model.IdToken);
                    email = payload.Email;
                    name = payload.Name;
                }
                else if (model.Provider == "Apple")
                {
                    return BadRequest(new { message = "Apple login not yet supported." });
                }
                else
                {
                    return BadRequest(new { message = "Unsupported provider." });
                }

                // ✅ 2. Find or create user
                user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    // Prevent duplicates by device or email
                    if (_userManager.Users.Any(u => u.Email == email || u.DeviceId == model.DeviceId))
                        return BadRequest(new { message = "A user with this email or device already exists." });

                    user = new ApplicationUser
                    {
                        Email = email,
                        UserName = email,
                        Name = name,
                        DateJoined = DateTime.UtcNow,
                        DeviceId = model.DeviceId,
                        IsApproved = false,
                        IsSubscriptionActive = false,
                        IsAdmin = false,
                        TrialEndDate = DateTime.UtcNow.AddDays(7)
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                        return BadRequest(createResult.Errors);

                    // Assign default role
                    await _userManager.AddToRoleAsync(user, "User");

                    // Record trial
                    _db.TrialRecords.Add(new TrialRecord
                    {
                        Email = email,
                        DeviceId = model.DeviceId,
                        UsedAt = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync();

                    // Notify
                    try
                    {
                        await _emailSender.SendEmailAsync(
                            user.Email,
                            "🎉 Registration Successful – Awaiting Approval | Splxit Jobs",
                            $"<p>Dear {user.Name},</p><p>Your account has been created and is pending admin approval.</p>"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send registration email to {Email}", user.Email);
                    }
                }

                // ✅ 3. Stop if not approved
                if (!user.IsApproved)
                    return Unauthorized(new { message = "Your account is pending admin approval. Please wait for approval." });

                // ✅ 4. Role and subscription logic
                var userRoles = await _userManager.GetRolesAsync(user);
                var isAdmin = userRoles.Contains("Admin");
                var isSubscriptionActive = user.TrialEndDate > DateTime.UtcNow;
                var isApproved = user.IsApproved;

                user.IsSubscriptionActive = isSubscriptionActive;

                // ✅ 5. Generate refresh token and JWT
                var refreshToken = GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userManager.UpdateAsync(user);

                var token = GenerateJwtToken(user, isAdmin, isSubscriptionActive, isApproved);

                // ✅ 6. Return identical structure
                return Ok(new
                {
                    UserId = user.Id,
                    Token = token,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiry = user.RefreshTokenExpiryTime,
                    UserName = user.Name,
                    UserEmail = user.Email,
                    IsAdmin = isAdmin,
                    IsSubscriptionActive = isSubscriptionActive,
                    IsApproved = isApproved,
                    TrialEndDate = user.TrialEndDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Social login failed for provider {Provider}", model.Provider);
                return StatusCode(500, new { message = "Social login failed.", details = ex.Message });
            }
        }


        public class DeregisterRequest
        {
            public string UserEmailOrPhone { get; set; }
        }


        [Authorize(Policy = "AdminPolicy")]
        [HttpPost("admin/deregister-device")]
        public async Task<IActionResult> AdminDeregisterDevice([FromBody] DeregisterRequest request)
        {
            try
            {
                _logger.LogInformation("Deregistering for: {UserIdentifier}", request.UserEmailOrPhone);

                // Step 1 — Find user
                var user = _userManager.Users
                    .FirstOrDefault(u => u.Email == request.UserEmailOrPhone || u.PhoneNumber == request.UserEmailOrPhone);

                if (user == null)
                {
                    _logger.LogWarning("User not found.");
                    return NotFound(new { message = "User not found." });
                }

                if (string.IsNullOrEmpty(user.DeviceId))
                {
                    _logger.LogInformation("No device registered for user.");
                }
                else
                {
                    // Step 2 — Clear the device from the user table
                    user.DeviceId = null;
                    var result = await _userManager.UpdateAsync(user);
                    if (!result.Succeeded)
                    {
                        _logger.LogError("Device update failed for user table.");
                        return StatusCode(500, new { message = "Failed to update user record." });
                    }
                    _logger.LogInformation("DeviceId cleared from user table.");
                }

                // Step 3 — Also clear matching entries in TrialRecords
                var matchingTrials = _db.TrialRecords
                    .Where(t => t.Email == request.UserEmailOrPhone || t.PhoneNumber == request.UserEmailOrPhone)
                    .ToList();

                if (matchingTrials.Any())
                {
                    foreach (var trial in matchingTrials)
                    {
                        trial.DeviceId = null;
                    }

                    _db.TrialRecords.UpdateRange(matchingTrials);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("DeviceId cleared from TrialRecords table.");
                }

                return Ok(new { message = $"All devices for user {user.Email} have been deregistered successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin device deregistration.");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }


        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromForm] RegisterModel registerModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // 🔹 Admins skip trial restrictions (no TrialRecords check)
                // 🔹 Fetch special admin subscription plan
                var adminPlan = await _db.SubscriptionPlans
                    .FirstOrDefaultAsync(p => p.Name == "Admin Forever" && p.Type == SubscriptionTier.Subscribed);

                if (adminPlan == null)
                {
                    return StatusCode(500, new { message = "Admin subscription plan is not configured." });
                }

                var now = DateTime.UtcNow;

                var adminUser = new ApplicationUser
                {
                    UserName = registerModel.Email,
                    Email = registerModel.Email,
                    Name = registerModel.Name,
                    Location = registerModel.Location,
                    NumberOfTasksCompleted = 0,
                    NumberOfTasksEmployed = 0,
                    LastTaskDoneDate = DateTime.SpecifyKind(registerModel.LastTaskDoneDate, DateTimeKind.Utc),
                    LastTaskEmployedDate = DateTime.SpecifyKind(registerModel.LastTaskEmployedDate, DateTimeKind.Utc),
                    UserRating = 0,
                    DateJoined = DateTime.SpecifyKind(registerModel.DateJoined, DateTimeKind.Utc),
                    PhoneNumber = registerModel.PhoneNumber,
                    DeviceId = registerModel.DeviceId,

                    // ✅ Admin privileges
                    IsAdmin = true,
                    IsApproved = true,
                    IsSubscriptionActive = true,

                    // ✅ Subscription details
                    CurrentPlanId = adminPlan.Id,
                    SubscriptionStartDate = now,
                    SubscriptionEndDate = now.AddYears(100), // or use AddDays(adminPlan.DurationDays)

                    // Optional fallback
                    TrialEndDate = now.AddYears(100)
                };



                if (registerModel.ProfilePhoto != null)
                {
                    using var memoryStream = new MemoryStream();
                    await registerModel.ProfilePhoto.CopyToAsync(memoryStream);
                    adminUser.ProfilePhoto = memoryStream.ToArray();
                }

                var result = await _userManager.CreateAsync(adminUser, registerModel.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                await _userManager.AddToRoleAsync(adminUser, "Admin");

                // Send welcome + confirmation email
                await _emailSender.SendEmailAsync(adminUser.Email, "Admin Registration Successful",
                    $"Dear {adminUser.Name},<br><br>Your admin account has been successfully created.<br>" +
                    $"<strong>Email:</strong> {adminUser.Email}<br>" +
                    $"Please log in and set up your account.<br><br>Thank you!");

                var userId = await _userManager.GetUserIdAsync(adminUser);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(adminUser);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId, code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(adminUser.Email, "Confirm Your Email",
                    $"Dear {adminUser.Name},<br><br>Please confirm your email by clicking <a href='{callbackUrl}'>here</a>.<br><br>Thank you!");

                return Ok(new { message = "Admin registered successfully. Please confirm your email." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Registration failed.");
                return StatusCode(500, new
                {
                    error = "Something went wrong during registration.",
                    details = ex.Message
                });
            }
        }



        //Firebase Cloud Messagimg (FCM) 
        [HttpPost("SaveToken")]
        public async Task<IActionResult> SaveToken([FromBody] TokenDTO model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            user.FcmToken = model.FcmToken;
            await _userManager.UpdateAsync(user);

            return Ok();
        }


        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Do not reveal that the user does not exist or is not confirmed
                return Ok(new { message = "If an account exists for this email, a password reset link has been sent." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = $"{model.ClientAppUrl}?token={encodedToken}&email={user.Email}";

            await _emailSender.SendEmailAsync(
                user.Email,
                "Reset Your Password",
                $"Please reset your password by clicking <a href='{callbackUrl}'>here</a>.<br><br>" +
                $"This link will expire soon. If you did not request this, ignore this email.");

            return Ok(new { message = "If an account exists for this email, a password reset link has been sent." });
        }



        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { message = "Invalid request." });

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);

            if (!result.Succeeded)
                return BadRequest(new { message = "Failed to reset password.", errors = result.Errors });

            return Ok(new { message = "Password has been reset successfully." });
        }



        [Authorize]
        [HttpGet("user")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var notifications = await _db.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(notifications);
        }

        private string GenerateJwtToken(ApplicationUser user, bool isAdmin, bool isSubscriptionActive, bool isApproved)
        {
            var jwtSettings = _configuration.GetSection("JWT");

            // ✅ Load secret from appsettings or environment
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? _configuration["JWT:Secret"];
            if (string.IsNullOrEmpty(secret))
                throw new Exception("JWT secret is missing — check environment variables or appsettings.json.");

            var key = Encoding.ASCII.GetBytes(secret);
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email),
            new Claim("IsAdmin", isAdmin.ToString()),
            new Claim("IsSubscriptionActive", isSubscriptionActive.ToString()),
            new Claim("IsApproved", isApproved.ToString())
        }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = jwtSettings.GetValue<string>("ValidIssuer"),
                Audience = jwtSettings.GetValue<string>("ValidAudience"),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            // ✅ Load secret safely for validation
            var secret = _configuration["JWT:Secret"] ?? Environment.GetEnvironmentVariable("JWT_SECRET");
            if (string.IsNullOrEmpty(secret))
                throw new Exception("JWT secret is missing — check environment variables or appsettings.json.");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false, // ignore expiration for refresh
                ValidIssuer = _configuration["JWT:ValidIssuer"],
                ValidAudience = _configuration["JWT:ValidAudience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }


        // Models for Login and Registration
        public class LoginModel
        {
            [Required]
            public string Email { get; set; }

            [Required]
            public string Password { get; set; }
        }

        public class RegisterModel
        {
            [Required]
            public string Email { get; set; }

            [Required]
            public string Password { get; set; }

            [MaxLength(50)]
            [Required(ErrorMessage = "Name is required")]
            [RegularExpression(@"^[A-Za-z\s\-']+$", ErrorMessage = "Name must contain only letters and spaces.")]
            public string Name { get; set; }


            [MaxLength(100)]
            [Required(ErrorMessage = "Location is required")]
            public string Location { get; set; }


            [Required]
            public DateTime LastTaskDoneDate { get; set; } = DateTime.UtcNow;

            [Required]
            public DateTime LastTaskEmployedDate { get; set; } = DateTime.UtcNow;

            [Required]
            public DateTime DateJoined { get; set; } = DateTime.UtcNow;


            [Required(ErrorMessage = "Phone number is required")]
            [RegularExpression(@"^[0-9+\-\s]+$", ErrorMessage = "Phone number must contain only digits and allowed symbols (+ - space).")]
            public string PhoneNumber { get; set; }

            [DefaultValue(false)]
            public bool IsAdmin { get; set; } = false; // Default to false for regular users
            public DateTime TrialEndDate { get; set; }
            public IFormFile? ProfilePhoto { get; set; }
            [Required]
            public string DeviceId { get; set; } // Device ID
        }
    }
}
