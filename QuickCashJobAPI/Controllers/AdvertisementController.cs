using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Models.DTO;
using QuickCashJobAPI.Services; // Import SD
using System.Security.Claims;

namespace QuickCashJobAPI.Controllers
{
    [Route("api/ads")]
    [ApiController]
    public class AdvertisementController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdvertisementController(ApplicationDbContext db) => _db = db;


        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            var userClaims = HttpContext.User.Identity as ClaimsIdentity;
            if (userClaims == null) return null;

            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return null;

            return await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var ads = await _db.Advertisements
                .Include(a => a.User)
                .Where(a =>
                    a.IsSubscriptionActive &&
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
                    Area = a.Area,
                    Contact = a.Contact,
                    IsSubscriptionActive = a.IsSubscriptionActive,
                })
                .ToListAsync();

            return Ok(ads);
        }


        // POST: api/Advertisement
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] AdvertisementDTO model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            if (!user.IsApproved || !user.IsSubscriptionActive || user.IsDeleted)
            {
                return Forbid("Only approved, active, and non-deleted users can create ads.");
            }

            var ad = new Advertisement
            {
                Category = model.Category,
                Name = string.IsNullOrWhiteSpace(model.Name) ? user.Name : model.Name,
                Description = model.Description,
                Area = model.Area,
                Contact = model.Contact,
                IsSubscriptionActive = true,
                UserId = user.Id
            };

            _db.Advertisements.Add(ad);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { id = ad.Id }, new AdvertisementDTO
            {
                Id = ad.Id,
                Category = ad.Category,
                Name = ad.Name,
                Description = ad.Description,
                Area = ad.Area,
                Contact = ad.Contact,
                IsSubscriptionActive = ad.IsSubscriptionActive,
            });
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
