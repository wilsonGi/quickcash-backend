// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Services;

namespace QuickCashJobAPI.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
            private readonly SignInManager<ApplicationUser> _signInManager;
            private readonly RoleManager<IdentityRole> _roleManager;
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly IUserStore<ApplicationUser> _userStore;
            private readonly IUserEmailStore<ApplicationUser> _emailStore;
            private readonly ILogger<RegisterModel> _logger;
            private readonly IEmailSender _emailSender;

            public RegisterModel(
                UserManager<ApplicationUser> userManager,
                RoleManager<IdentityRole> roleManager,
                IUserStore<ApplicationUser> userStore,
                SignInManager<ApplicationUser> signInManager,
                ILogger<RegisterModel> logger,
                IEmailSender emailSender)
            {
                _roleManager = roleManager;
                _userManager = userManager;
                _userStore = userStore;
                _emailStore = GetEmailStore();
                _signInManager = signInManager;
                _logger = logger;
                _emailSender = emailSender;
            }

            [BindProperty]
            public InputModel Input { get; set; }

            public string ReturnUrl { get; set; }

            public IList<AuthenticationScheme> ExternalLogins { get; set; }

            public class InputModel
            {
                [Required]
                [EmailAddress]
                [Display(Name = "Email")]
                public string Email { get; set; }

                [Required]
                [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
                [DataType(DataType.Password)]
                [Display(Name = "Password")]
                public string Password { get; set; }

                [DataType(DataType.Password)]
                [Display(Name = "Confirm password")]
                [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
                public string ConfirmPassword { get; set; }

                public string? Role { get; set; }

                [ValidateNever]
                public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> RoleList { get; set; }

                [Required]
                public string Name { get; set; }

                [Required]
                public string Location { get; set; }

                [Required]
                public int NumberOfTasksCompleted { get; set; }

                [Required]
                public int NumberOfTasksEmployed { get; set; }

                [Required]
                public DateTime LastTaskDoneDate { get; set; } = DateTime.Now;

                [Required]
                public DateTime LastTaskEmployedDate { get; set; } = DateTime.Now;

                [Required]
                [Range(0, 100)]
                public double UserRating { get; set; }


                [Required]
                public DateTime DateJoined { get; set; } = DateTime.Now;

                [Required]
                [GhanaPhoneNumber(15)]  // Assuming a maximum of 15 digits
                [Display(Name = "Phone Number")]
                public string PhoneNumber { get; set; }



            }


            public async Task OnGetAsync(string returnUrl = null)
            {

                if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
                {
                    _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                    _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                    _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                    _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();
                }

                Input = new()
                {
                    RoleList = _roleManager.Roles.Select(x => x.Name).Select(i => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Text = i,
                        Value = i
                    })
                };


                ReturnUrl = returnUrl;
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            }

            public async Task<IActionResult> OnPostAsync(string returnUrl = null)
            {
                returnUrl ??= Url.Content("~/");
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
                if (ModelState.IsValid)
                {
                    // Check if the NationalIdNo is already in use by a non-deleted user
                    var existingUser = await _userManager.Users
                                        .FirstOrDefaultAsync(u => u.PhoneNumber == Input.PhoneNumber && !u.IsDeleted);

                    if (existingUser != null)
                    {
                        ModelState.AddModelError(string.Empty, "The National ID No is already in use.");
                        return Page();
                    }

                    var user = CreateUser();

                    await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                    await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                    user.Location = Input.Location;
                    user.DateJoined = Input.DateJoined;
                    user.PhoneNumber = Input.PhoneNumber;
                    user.Name = Input.Name;
                    user.TrialEndDate = DateTime.UtcNow.AddDays(30);


                    var result = await _userManager.CreateAsync(user, Input.Password);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created a new account with password.");
                    // Assign the role, defaulting to "Customer" if no role is provided
                    //string role = string.IsNullOrEmpty(Input.Role) ? SD.Role_Customer : Input.Role;
                    string role = string.IsNullOrEmpty(Input.Role) ? SD.Role_Admin : Input.Role;

                    await _userManager.AddToRoleAsync(user, role);


                    if (Input.Role == "Admin")
                        {
                            await _userManager.AddToRoleAsync(user, SD.Role_Admin);
                        }
                        else if (Input.Role == "Employee")
                        {
                            await _userManager.AddToRoleAsync(user, SD.Role_Employee);
                        }

                        else if (Input.Role == SD.Role_Company)
                        {
                            await _userManager.AddToRoleAsync(user, SD.Role_Company);
                        }

                        else
                        {
                            await _userManager.AddToRoleAsync(user, SD.Role_Customer);
                        }


                    if (!user.IsApproved)
                    {
                        try
                        {
                            await _emailSender.SendEmailAsync(Input.Email, "Registration successful!",
                               $"Dear {user.Name}, <br><br>Your registration is successful," +
                               $"<strong>Email:</strong> {user.Email}<br>" +
                               $"please wait for approval to get full access. Thank you.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Failed to send registration email to {Input.Email}: {ex.Message}");
                        }

                        // Continue execution even if email sending fails
                        return RedirectToAction("ApprovalPending", "Account");

                    }

                    if (user.IsApproved) // Assuming you have an IsApproved flag
                    {
                        try
                        {
                            await _emailSender.SendEmailAsync(Input.Email, "Welcome to the app",
                               $"Dear {user.Name}, <br><br>Welcome to Quick Cash Job app! You have been approved as a user.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Failed to send welcome email to {Input.Email}: {ex.Message}");
                        }
                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                        }


                        // Send email confirmation
                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId, code, returnUrl },
                            protocol: Request.Scheme);

                        await _emailSender.SendEmailAsync(Input.Email, "Welcome to the app",
                        $"Dear {user.Name}, <br><br>Welcome to Quick Cash Job app! You have been approved as a user.");

                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                        }

                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                // If we got this far, something failed, redisplay form
                return Page();
            }

            private ApplicationUser CreateUser()
            {
                try
                {
                    return Activator.CreateInstance<ApplicationUser>();
                }
                catch
                {
                    throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                        $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                        $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
                }
            }

            private IUserEmailStore<ApplicationUser> GetEmailStore()
            {
                if (!_userManager.SupportsUserEmail)
                {
                    throw new NotSupportedException("The default UI requires a user store with email support.");
                }
                return (IUserEmailStore<ApplicationUser>)_userStore;
            }
        }
}
