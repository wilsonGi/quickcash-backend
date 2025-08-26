using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Models.DTO;
using QuickCashJobAPI.Services; // SD
using System.Security.Claims;

namespace QuickCashJobAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdvertisementController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdvertisementController(ApplicationDbContext db) => _db = db;

        // ✅ Only load what ApplicationUser really has
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
            var ads = await _db.Advertisements
     .Include(a => a.User)
     .Where(a => a.IsSubscriptionActive &&
                 a.User != null &&
                 a.User.IsApproved &&
                 a.User.IsSubscriptionActive &&
                 !a.User.IsDeleted)
     .Select(a => new AdvertisementDTO
     {
         Id = a.Id,
         Category = a.Category,
         Name = a.Name,
         Description = a.Description,
         IsSubscriptionActive = a.IsSubscriptionActive,
         User = new AdUserDTO
         {
             Id = a.User.Id,
             Name = a.User.Name,
             Location = a.User.Location,
             PhoneNumber = a.User.PhoneNumber,
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
             EmployedCategories = _db.Jobs.Where(j => j.UserId == a.User.Id).Select(j => j.Category.CategoryName).Distinct().ToList()
         }
     })
     .ToListAsync();

            Console.WriteLine("🔥 [GetAll] AdvertisementController hit!");


            return Ok(ads);
        }




        // ✅ POST: create ad
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] AdvertisementDTO model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            // ✅ Add this check right after getting the user
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
                IsSubscriptionActive = true,
                UserId = user.Id
            };

            _db.Advertisements.Add(ad);
            await _db.SaveChangesAsync();

            // ✅ Explicit queries for categories
            var completedCategories = await _db.UserCompletedCategories
                .Where(c => c.UserId == user.Id)
                .Include(c => c.Category)
                .Select(c => c.Category.CategoryName)
                .ToListAsync();

            var employedCategories = await _db.Jobs
                .Where(j => j.UserId == user.Id)
                .Select(j => j.Category.CategoryName)
                .Distinct()
                .ToListAsync();

            var skills = await _db.UserSkills
                .Where(us => us.UserId == user.Id)
                .Select(us => us.Skill.Name)
                .ToListAsync();

            // ✅ Return same shape as GET
            var dto = new AdvertisementDTO
            {
                Id = ad.Id,
                Category = ad.Category,
                Name = ad.Name,
                Description = ad.Description,
         
                IsSubscriptionActive = ad.IsSubscriptionActive,

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
                    LastTaskDoneDate = user.LastTaskDoneDate == default(DateTime) ? null : user.LastTaskDoneDate,
                    LastTaskEmployedDate = user.LastTaskEmployedDate == default(DateTime) ? null : user.LastTaskEmployedDate,
                    UserRating = user.UserRating,
                    Skills = skills,
                    CompletedCategories = completedCategories,
                    EmployedCategories = employedCategories
                }
            };

            return Ok(dto);
        }





        [HttpPut("{id}/activate")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Activate(int id)
        {
            var ad = await _db.Advertisements.FindAsync(id);
            if (ad == null) return NotFound();

            ad.IsSubscriptionActive = true;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var ad = await _db.Advertisements.FindAsync(id);
            if (ad == null) return NotFound();

            ad.IsSubscriptionActive = false;
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
