using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using QuickCashJobAPI.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

        public AuthController(IConfiguration configuration, 
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _signInManager = signInManager;
        }

        // User Login Endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            var user = await _userManager.FindByEmailAsync(loginModel.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, loginModel.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var isAdmin = userRoles.Contains("Admin");

                var isSubscriptionActive = user.TrialEndDate > DateTime.UtcNow;
                var isApproved = user.IsApproved;

                if (!user.IsApproved)
                {
                    return Unauthorized(new { message = "Your account has not been approved yet. Please wait for admin approval." });
                }


                var token = GenerateJwtToken(user, isAdmin, isSubscriptionActive, isApproved);
                return Ok(new
                {
                    UserId = user.Id,
                    Token = token,
                    UserName = user.Name,
                    UserEmail = user.Email,
                    IsAdmin = isAdmin, // Include admin flag in the response
                    IsSubscriptionActive = isSubscriptionActive,
                    IsApproved = isApproved

                });
            }

            return Unauthorized(new { message = "Invalid email or password." });
        }

        // User Registration Endpoint
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterModel registerModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState is invalid: " + string.Join("; ", ModelState.Values
                        .SelectMany(x => x.Errors)
                        .Select(x => x.ErrorMessage)));
                    return BadRequest(ModelState);
                }

                var existingUser = await _userManager.FindByEmailAsync(registerModel.Email);
                if (existingUser != null)
                {
                    Console.WriteLine($"Registration attempt failed: Email {registerModel.Email} already exists.");
                    return BadRequest(new { message = "This email is already registered." });
                }

                var existingPhone = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == registerModel.PhoneNumber);
                if (existingPhone != null)
                {
                    Console.WriteLine($"Registration attempt failed: Phone {registerModel.PhoneNumber} already exists.");
                    return BadRequest(new { message = "This phone number is already registered." });
                }

                var existingDevice = _userManager.Users.FirstOrDefault(u => u.DeviceId == registerModel.DeviceId);
                if (existingDevice != null)
                {
                    Console.WriteLine($"Registration attempt failed: DeviceId {registerModel.DeviceId} already used.");
                    return BadRequest(new { message = "Registration from this device is already used." });
                }

                var user = new ApplicationUser
                {
                    UserName = registerModel.Email,
                    Email = registerModel.Email,
                    Name = registerModel.Name,
                    Location = registerModel.Location,
                    NumberOfTasksCompleted = 0,
                    NumberOfTasksEmployed = 0,
                    LastTaskDoneDate = registerModel.LastTaskDoneDate,
                    LastTaskEmployedDate = registerModel.LastTaskEmployedDate,
                    DateJoined = registerModel.DateJoined,
                    PhoneNumber = registerModel.PhoneNumber,
                    IsAdmin = false,
                    TrialEndDate = DateTime.UtcNow.AddDays(30),
                    DeviceId = registerModel.DeviceId
                };

                var result = await _userManager.CreateAsync(user, registerModel.Password);

                if (!result.Succeeded)
                {
                    Console.WriteLine("UserManager.CreateAsync failed: " + string.Join("; ", result.Errors.Select(e => e.Description)));
                    return BadRequest(result.Errors);
                }

                if (registerModel.ProfilePhoto != null)
                {
                    using var memoryStream = new MemoryStream();
                    await registerModel.ProfilePhoto.CopyToAsync(memoryStream);
                    user.ProfilePhoto = memoryStream.ToArray();
                    await _userManager.UpdateAsync(user);
                }

                var role = registerModel.IsAdmin ? "Admin" : "Customer";
                await _userManager.AddToRoleAsync(user, role);

                if (!user.IsApproved)
                {
                    await _emailSender.SendEmailAsync(user.Email, "Registration successful!",
                        $"Dear {user.Name},<br><br>Your registration is successful,<br><strong>Email:</strong> {user.Email}<br>Please wait for approval to get full access. Thank you.");

                    Console.WriteLine($"User {user.Email} registered successfully, pending approval.");
                    return Ok(new { message = "User registered successfully, pending approval" });
                }

                if (user.IsApproved)
                {
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    await _emailSender.SendEmailAsync(user.Email, "Welcome to the app",
                        $"Dear {user.Name},<br><br>Welcome to Quick Cash Job app! You have been approved as a user.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        Console.WriteLine($"User {user.Email} registered successfully. Requires email confirmation.");
                        return Ok(new { message = "User registered successfully. Please confirm your email." });
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    Console.WriteLine($"User {user.Email} registered and signed in successfully.");
                    return Ok(new { message = "User registered and signed in successfully" });
                }

                Console.WriteLine($"User {user.Email} registered successfully without requiring approval.");
                return Ok(new { message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during registration: {ex.Message} - {ex.StackTrace}");
                return StatusCode(500, new { message = "An unexpected error occurred during registration." });
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

                var adminUser = new ApplicationUser
                {
                    UserName = registerModel.Email,
                    Email = registerModel.Email,
                    Name = registerModel.Name,
                    Location = registerModel.Location,
                    NumberOfTasksCompleted = 0,
                    NumberOfTasksEmployed = 0,
                    LastTaskDoneDate = registerModel.LastTaskDoneDate,
                    LastTaskEmployedDate = registerModel.LastTaskEmployedDate,
                    UserRating = 0,
                    DateJoined = registerModel.DateJoined,
                    PhoneNumber = registerModel.PhoneNumber,
                    IsAdmin = true,
                    IsApproved = true,
                    IsSubscriptionActive = true,
                    TrialEndDate = DateTime.UtcNow.AddDays(30),
                    DeviceId = registerModel.DeviceId
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
                // Log the full error to console or logger
                Console.WriteLine($"❌ Registration failed: {ex.Message} \n {ex.StackTrace}");

                return StatusCode(500, new
                {
                    error = "Something went wrong during registration.",
                    details = ex.Message
                });
            }
        }


        private string GenerateJwtToken(ApplicationUser user, bool isAdmin, bool isSubscriptionActive, bool isApproved)
        {
            var jwtSettings = _configuration.GetSection("JWT");
            var key = Encoding.ASCII.GetBytes(jwtSettings.GetValue<string>("Secret"));
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Name), // Store Full Name
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName), // Store Username
            new Claim("IsAdmin", isAdmin.ToString()),  // Add IsAdmin claim
            new Claim("IsSubscriptionActive", isSubscriptionActive.ToString()),
            new Claim("IsApproved", isApproved.ToString()),

                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = jwtSettings.GetValue<string>("ValidIssuer"),
                Audience = jwtSettings.GetValue<string>("ValidAudience"),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
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

            [Required]
            [MaxLength(50)]
            public string Name { get; set; }

            [Required]
            [MaxLength(100)]
            public string Location { get; set; }


            [Required]
            public DateTime LastTaskDoneDate { get; set; } = DateTime.Now;

            [Required]
            public DateTime LastTaskEmployedDate { get; set; } = DateTime.Now;

            [Required]
            public DateTime DateJoined { get; set; } = DateTime.Now;

            [Required]
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
