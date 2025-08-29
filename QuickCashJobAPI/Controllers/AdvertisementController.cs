using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Models.DTO;
using QuickCashJobAPI.Services; // SD
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace QuickCashJobAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdvertisementController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdvertisementController(ApplicationDbContext db) => _db = db;

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return null;

            return await _db.ApplicationUsers
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        // ✅ GET all ads
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var currentUser = await GetCurrentUserAsync();


            var ads = await _db.Advertisements
                .Include(a => a.User)
                .Where(a => a.User != null &&
                            a.User.IsApproved &&
                            a.User.IsSubscriptionActive &&
                            !a.User.IsDeleted)
                .Select(a => new AdvertisementDTO
                {
                    Id = a.Id,
                    Category = a.Category,
                    Name = a.Name,
                                    Description = (currentUser != null && currentUser.IsApproved && currentUser.IsSubscriptionActive)
                    ? a.Description
                    : Regex.Replace(
                        a.Description ?? "",
                        @"(
                            \+?\d[\d\s\-]{7,}                               # phone numbers
                            |[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,} # emails
                            |(?:https?:\/\/)?(?:www\.)?[A-Za-z0-9.-]+\.[A-Za-z]{2,} # websites/domains
                            |(?<!\w)@\w{3,30}                               # social media handles
                        )",
                        "[Restricted]",
                        RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase
                      ),



                    User = new AdUserDTO
                    {
                        Id = a.User.Id,
                        Name = a.User.Name,
                        Location = a.User.Location,

                        PhoneNumber = (currentUser != null && currentUser.IsApproved && currentUser.IsSubscriptionActive)
                        ? a.User.PhoneNumber
                        : "Restricted",
                        ProfilePhoto = a.User.ProfilePhoto != null
                            ? Convert.ToBase64String(a.User.ProfilePhoto)
                            : null,
                        NumberOfTasksCompleted = a.User.NumberOfTasksCompleted,
                        NumberOfTasksEmployed = a.User.NumberOfTasksEmployed,
                        LastTaskDoneDate = a.User.LastTaskDoneDate == default ? null : a.User.LastTaskDoneDate,
                        LastTaskEmployedDate = a.User.LastTaskEmployedDate == default ? null : a.User.LastTaskEmployedDate,
                        UserRating = a.User.UserRating,
                        Skills = _db.UserSkills.Where(us => us.UserId == a.User.Id).Select(us => us.Skill.Name).ToList(),
                        CompletedCategories = _db.UserCompletedCategories.Where(uc => uc.UserId == a.User.Id).Select(uc => uc.Category.CategoryName).ToList(),
                        EmployedCategories = _db.Jobs.Where(j => j.UserId == a.User.Id).Select(j => j.Category.CategoryName).Distinct().ToList(),
                        IsSubscriptionActive = a.User.IsSubscriptionActive,
                        IsApproved = a.User.IsApproved
                    }
                })
                .ToListAsync();

            return Ok(ads);
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyAds()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var ads = await _db.Advertisements
                .Where(a => a.UserId == user.Id)
                .Select(a => new AdvertisementDTO
                {
                    Id = a.Id,
                    Category = a.Category,
                    Name = a.Name,
                    Description = a.Description,
                    User = new AdUserDTO
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Location = user.Location,
                        PhoneNumber = user.PhoneNumber,
                        ProfilePhoto = user.ProfilePhoto != null
                            ? Convert.ToBase64String(user.ProfilePhoto)
                            : null,
                        NumberOfTasksCompleted = user.NumberOfTasksCompleted,
                        NumberOfTasksEmployed = user.NumberOfTasksEmployed,
                        LastTaskDoneDate = user.LastTaskDoneDate == default ? null : user.LastTaskDoneDate,
                        LastTaskEmployedDate = user.LastTaskEmployedDate == default ? null : user.LastTaskEmployedDate,
                        UserRating = user.UserRating,
                        Skills = _db.UserSkills.Where(us => us.UserId == user.Id).Select(us => us.Skill.Name).ToList(),
                        CompletedCategories = _db.UserCompletedCategories.Where(uc => uc.UserId == user.Id).Select(uc => uc.Category.CategoryName).ToList(),
                        EmployedCategories = _db.Jobs.Where(j => j.UserId == user.Id).Select(j => j.Category.CategoryName).Distinct().ToList(),
                        IsSubscriptionActive = user.IsSubscriptionActive,
                        IsApproved = user.IsApproved
                    }
                }).ToListAsync();

            return Ok(ads);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] AdvertisementDTO model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(user.Location) || string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                return BadRequest("Your profile must include Location and Phone Number to post an ad.");
            }

            if (!user.IsApproved || !user.IsSubscriptionActive || user.IsDeleted)
            {
                return BadRequest("Only approved, active, and non-deleted users can create ads.");
            }

            var ad = new Advertisement
            {
                Category = model.Category,
                Name = string.IsNullOrWhiteSpace(model.Name) ? user.Name : model.Name,
                Description = model.Description,
                Area = user.Location,
                Contact = user.PhoneNumber,
                UserId = user.Id
            };

            _db.Advertisements.Add(ad);
            await _db.SaveChangesAsync();

            var dto = new AdvertisementDTO
            {
                Id = ad.Id,
                Category = ad.Category,
                Name = ad.Name,
                Description = ad.Description,
                User = new AdUserDTO
                {
                    Id = user.Id,
                    Name = user.Name,
                    Location = user.Location,
                    PhoneNumber = user.PhoneNumber,
                    ProfilePhoto = user.ProfilePhoto != null
                        ? Convert.ToBase64String(user.ProfilePhoto)
                        : null,
                    NumberOfTasksCompleted = user.NumberOfTasksCompleted,
                    NumberOfTasksEmployed = user.NumberOfTasksEmployed,
                    LastTaskDoneDate = user.LastTaskDoneDate == default ? null : user.LastTaskDoneDate,
                    LastTaskEmployedDate = user.LastTaskEmployedDate == default ? null : user.LastTaskEmployedDate,
                    UserRating = user.UserRating,
                    Skills = _db.UserSkills.Where(us => us.UserId == user.Id).Select(us => us.Skill.Name).ToList(),
                    CompletedCategories = _db.UserCompletedCategories.Where(uc => uc.UserId == user.Id).Select(uc => uc.Category.CategoryName).ToList(),
                    EmployedCategories = _db.Jobs.Where(j => j.UserId == user.Id).Select(j => j.Category.CategoryName).Distinct().ToList(),
                    IsSubscriptionActive = user.IsSubscriptionActive,
                    IsApproved = user.IsApproved
                }
            };

            return Ok(dto);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] AdvertisementDTO model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
            if (ad == null) return NotFound("Advertisement not found or not yours.");

            ad.Category = model.Category;
            ad.Name = string.IsNullOrWhiteSpace(model.Name) ? user.Name : model.Name;
            ad.Description = model.Description;
            ad.Area = user.Location;
            ad.Contact = user.PhoneNumber;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var ad = await _db.Advertisements.FindAsync(id);
            if (ad == null) return NotFound();

            _db.Advertisements.Remove(ad);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
