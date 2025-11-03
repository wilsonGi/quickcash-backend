//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using QuickCashJobAPI.Data;
//using QuickCashJobAPI.Models;
//using QuickCashJobAPI.Models.DTO;
//using QuickCashJobAPI.Services;
//using System.Security.Claims;
//using System.Text.RegularExpressions;

//namespace QuickCashJobAPI.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class AdvertisementController : ControllerBase
//    {
//        private readonly ApplicationDbContext _db;
//        private readonly SubscriptionService _subscriptionService;

//        public AdvertisementController(ApplicationDbContext db, SubscriptionService subscriptionService)
//        {
//            _db = db;
//            _subscriptionService = subscriptionService;
//        }

//        // ✅ CREATE AD
//        [HttpPost]
//        [Authorize]
//        public async Task<IActionResult> Create([FromBody] AdvertisementDTO model)
//        {
//            var user = await GetCurrentUserAsync();
//            if (user == null) return Unauthorized();

//            if (string.IsNullOrWhiteSpace(user.Location) || string.IsNullOrWhiteSpace(user.PhoneNumber))
//                return BadRequest("Your profile must include Location and Phone Number to post an ad.");

//            // 🔒 Check ad limit
//            bool canPostAd = await _subscriptionService.CanPostAd(user);
//            if (!canPostAd)
//            {
//                return StatusCode(402, new
//                {
//                    code = "AD_LIMIT_REACHED",
//                    message = "You have reached your advertisement limit. Please upgrade your plan or PAYG to continue."
//                });
//            }

//            // ✅ Create new ad
//            var ad = new Advertisement
//            {
//                Category = model.Category,
//                Name = string.IsNullOrWhiteSpace(model.Name) ? user.Name : model.Name,
//                Description = model.Description,
//                Area = string.IsNullOrWhiteSpace(model.Area) ? user.Location : model.Area,
//                Contact = string.IsNullOrWhiteSpace(model.Contact) ? user.PhoneNumber : model.Contact,
//                UserId = user.Id,
//                CreatedAt = DateTime.UtcNow,
//                IsActive = model.IsActive
//            };

//            _db.Advertisements.Add(ad);
//            await _db.SaveChangesAsync();

//            var dto = new AdvertisementDTO
//            {
//                Id = ad.Id,
//                Category = ad.Category,
//                Name = ad.Name,
//                Description = ad.Description,
//                Area = ad.Area,
//                Contact = ad.Contact,
//                IsActive = ad.IsActive,
//                User = new AdUserDTO
//                {
//                    Id = user.Id,
//                    Name = user.Name,
//                    Location = user.Location,
//                    PhoneNumber = user.PhoneNumber,
//                    ProfilePhoto = user.ProfilePhoto != null
//                        ? Convert.ToBase64String(user.ProfilePhoto)
//                        : null,
//                    NumberOfTasksCompleted = user.NumberOfTasksCompleted,
//                    NumberOfTasksEmployed = user.NumberOfTasksEmployed,
//                    UserRating = user.UserRating,
//                    IsSubscriptionActive = user.IsSubscriptionActive,
//                    IsApproved = user.IsApproved
//                }
//            };

//            return Ok(dto);
//        }

//        private async Task<ApplicationUser?> GetCurrentUserAsync()
//        {
//            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//            if (string.IsNullOrEmpty(userId)) return null;

//            return await _db.ApplicationUsers
//                .Include(u => u.UserSkills)
//                    .ThenInclude(us => us.Skill)
//                .FirstOrDefaultAsync(u => u.Id == userId);
//        }

//        // ✅ GET ALL ADS
//        [HttpGet]
//        public async Task<IActionResult> GetAll()
//        {
//            var currentUser = await GetCurrentUserAsync();

//            var ads = await _db.Advertisements
//                .Include(a => a.User)
//                .Where(a => a.User != null &&
//                            a.User.IsApproved &&
//                            a.User.IsSubscriptionActive &&
//                            !a.User.IsDeleted)
//                .Select(a => new AdvertisementDTO
//                {
//                    Id = a.Id,
//                    Category = a.Category,
//                    Name = a.Name,
//                    Description = (currentUser != null && currentUser.IsApproved && currentUser.IsSubscriptionActive)
//                        ? a.Description
//                        : Regex.Replace(
//                            a.Description ?? "",
//                            @"(
//                                \+?\d[\d\s\-]{7,}
//                                |[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}
//                                |(?:https?:\/\/)?(?:www\.)?[A-Za-z0-9.-]+\.[A-Za-z]{2,}
//                                |(?<!\w)@\w{3,30}
//                            )",
//                            "[Restricted]",
//                            RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase
//                        ),
//                    Area = a.Area,
//                    Contact = (currentUser != null && currentUser.IsApproved && currentUser.IsSubscriptionActive)
//                        ? a.Contact
//                        : "Restricted",
//                    IsActive = a.IsActive,
//                    User = new AdUserDTO
//                    {
//                        Id = a.User.Id,
//                        Name = a.User.Name,
//                        Location = a.User.Location,
//                        PhoneNumber = (currentUser != null && currentUser.IsApproved && currentUser.IsSubscriptionActive)
//                            ? a.User.PhoneNumber
//                            : "Restricted",
//                        ProfilePhoto = a.User.ProfilePhoto != null
//                            ? Convert.ToBase64String(a.User.ProfilePhoto)
//                            : null,
//                        NumberOfTasksCompleted = a.User.NumberOfTasksCompleted,
//                        NumberOfTasksEmployed = a.User.NumberOfTasksEmployed,
//                        UserRating = a.User.UserRating,
//                        IsSubscriptionActive = a.User.IsSubscriptionActive,
//                        IsApproved = a.User.IsApproved
//                    }
//                })
//                .ToListAsync();

//            return Ok(ads);
//        }

//        // ✅ GET MY ADS
//        [HttpGet("my")]
//        [Authorize]
//        public async Task<IActionResult> GetMyAds()
//        {
//            var user = await GetCurrentUserAsync();
//            if (user == null) return Unauthorized();

//            var ads = await _db.Advertisements
//                .Where(a => a.UserId == user.Id)
//                .Select(a => new AdvertisementDTO
//                {
//                    Id = a.Id,
//                    Category = a.Category,
//                    Name = a.Name,
//                    Description = a.Description,
//                    Area = a.Area,
//                    Contact = a.Contact,
//                    IsActive = a.IsActive,
//                    User = new AdUserDTO
//                    {
//                        Id = user.Id,
//                        Name = user.Name,
//                        Location = user.Location,
//                        PhoneNumber = user.PhoneNumber,
//                        ProfilePhoto = user.ProfilePhoto != null
//                            ? Convert.ToBase64String(user.ProfilePhoto)
//                            : null,
//                        NumberOfTasksCompleted = user.NumberOfTasksCompleted,
//                        NumberOfTasksEmployed = user.NumberOfTasksEmployed,
//                        UserRating = user.UserRating,
//                        IsSubscriptionActive = user.IsSubscriptionActive,
//                        IsApproved = user.IsApproved
//                    }
//                }).ToListAsync();

//            return Ok(ads);
//        }

//        // ✅ UPDATE AD
//        [HttpPut("{id}")]
//        [Authorize]
//        public async Task<IActionResult> Update(int id, [FromBody] AdvertisementDTO model)
//        {
//            var user = await GetCurrentUserAsync();
//            if (user == null) return Unauthorized();

//            var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
//            if (ad == null) return NotFound("Advertisement not found or not yours.");

//            ad.Category = model.Category;
//            ad.Name = string.IsNullOrWhiteSpace(model.Name) ? user.Name : model.Name;
//            ad.Description = model.Description;
//            ad.Area = string.IsNullOrWhiteSpace(model.Area) ? user.Location : model.Area;
//            ad.Contact = string.IsNullOrWhiteSpace(model.Contact) ? user.PhoneNumber : model.Contact;
//            ad.IsActive = model.IsActive;

//            await _db.SaveChangesAsync();
//            return NoContent();
//        }



//        // ✅ TOGGLE SHOW/HIDE AD
//        [HttpPut("{id}/{action}")]
//        [Authorize]
//        public async Task<IActionResult> PerformAction(int id, string action)
//        {
//            var user = await GetCurrentUserAsync();
//            if (user == null) return Unauthorized();

//            var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
//            if (ad == null) return NotFound("Ad not found or not yours.");

//            // Determine the action
//            if (action.Equals("hide", StringComparison.OrdinalIgnoreCase))
//            {
//                ad.IsActive = false;
//            }
//            else if (action.Equals("show", StringComparison.OrdinalIgnoreCase))
//            {
//                ad.IsActive = true;
//            }
//            else
//            {
//                return BadRequest("Invalid action. Use 'show' or 'hide'.");
//            }

//            await _db.SaveChangesAsync();
//            return Ok(new { message = $"Ad {(ad.IsActive ? "shown" : "hidden")} successfully." });
//        }




//        [HttpDelete("{id}")]
//        [Authorize]
//        public async Task<IActionResult> Delete(int id)
//        {
//            var ad = await _db.Advertisements.FindAsync(id);
//            if (ad == null) return NotFound();

//            _db.Advertisements.Remove(ad);
//            await _db.SaveChangesAsync();
//            return NoContent();
//        }
//    }
//}




using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Models.DTO;
using QuickCashJobAPI.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace QuickCashJobAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdvertisementController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly SubscriptionService _subscriptionService;

        public AdvertisementController(ApplicationDbContext db, SubscriptionService subscriptionService)
        {
            _db = db;
            _subscriptionService = subscriptionService;
        }

        // ✅ CREATE AD (with optional image upload)
        [HttpPost]
        [Authorize]
        [RequestSizeLimit(5_000_000)] // limit ~5MB
        public async Task<IActionResult> Create([FromForm] AdvertisementDTO model, IFormFile? imageFile)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(user.Location) || string.IsNullOrWhiteSpace(user.PhoneNumber))
                return BadRequest("Your profile must include Location and Phone Number to post an ad.");

            bool canPostAd = await _subscriptionService.CanPostAd(user);
            if (!canPostAd)
            {
                return StatusCode(402, new
                {
                    code = "AD_LIMIT_REACHED",
                    message = "You have reached your advertisement limit. Please upgrade your plan or PAYG to continue."
                });
            }

            byte[]? compressedImageBytes = null;

            if (imageFile != null && imageFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await imageFile.CopyToAsync(memoryStream);
                var originalBytes = memoryStream.ToArray();
                compressedImageBytes = CompressImage(originalBytes, 70L); // ~70% quality
            }

            var ad = new Advertisement
            {
                Category = model.Category,
                Name = string.IsNullOrWhiteSpace(model.Name) ? user.Name : model.Name,
                Description = model.Description,
                Area = string.IsNullOrWhiteSpace(model.Area) ? user.Location : model.Area,
                Contact = string.IsNullOrWhiteSpace(model.Contact) ? user.PhoneNumber : model.Contact,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = model.IsActive,
                AdImage = compressedImageBytes
            };

            _db.Advertisements.Add(ad);
            await _db.SaveChangesAsync();

            var dto = MapToDTO(ad, user);
            return Ok(dto);
        }

        // 🧠 Helper to compress image to reduce DB weight
        private static byte[] CompressImage(byte[] imageBytes, long quality)
        {
            using var inputStream = new MemoryStream(imageBytes);
            using var image = Image.FromStream(inputStream);

            var encoder = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

            using var outputStream = new MemoryStream();
            image.Save(outputStream, encoder, encoderParams);
            return outputStream.ToArray();
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return null;

            return await _db.ApplicationUsers
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        // ✅ GET ALL ADS
        [HttpGet]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)] // cache client-side for 1min
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
                                \+?\d[\d\s\-]{7,}
                                |[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}
                                |(?:https?:\/\/)?(?:www\.)?[A-Za-z0-9.-]+\.[A-Za-z]{2,}
                                |(?<!\w)@\w{3,30}
                            )",
                            "[Restricted]",
                            RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase
                        ),
                    Area = a.Area,
                    Contact = (currentUser != null && currentUser.IsApproved && currentUser.IsSubscriptionActive)
                        ? a.Contact
                        : "Restricted",
                    IsActive = a.IsActive,
                    AdImageBase64 = a.AdImage != null ? Convert.ToBase64String(a.AdImage) : null,
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
                        UserRating = a.User.UserRating,
                        IsSubscriptionActive = a.User.IsSubscriptionActive,
                        IsApproved = a.User.IsApproved
                    }
                })
                .ToListAsync();

            return Ok(ads);
        }

        // ✅ GET MY ADS
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
                    Area = a.Area,
                    Contact = a.Contact,
                    IsActive = a.IsActive,
                    AdImageBase64 = a.AdImage != null ? Convert.ToBase64String(a.AdImage) : null,
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
                        UserRating = user.UserRating,
                        IsSubscriptionActive = user.IsSubscriptionActive,
                        IsApproved = user.IsApproved
                    }
                }).ToListAsync();

            return Ok(ads);
        }

        // ✅ UPDATE AD (can also update image)
        [HttpPut("{id}")]
        [Authorize]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> Update(int id, [FromForm] AdvertisementDTO model, IFormFile? imageFile)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
            if (ad == null) return NotFound("Advertisement not found or not yours.");

            ad.Category = model.Category;
            ad.Name = string.IsNullOrWhiteSpace(model.Name) ? user.Name : model.Name;
            ad.Description = model.Description;
            ad.Area = string.IsNullOrWhiteSpace(model.Area) ? user.Location : model.Area;
            ad.Contact = string.IsNullOrWhiteSpace(model.Contact) ? user.PhoneNumber : model.Contact;
            ad.IsActive = model.IsActive;

            if (imageFile != null && imageFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await imageFile.CopyToAsync(memoryStream);
                ad.AdImage = CompressImage(memoryStream.ToArray(), 70L);
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ✅ TOGGLE SHOW/HIDE AD
        [HttpPut("{id}/{action}")]
        [Authorize]
        public async Task<IActionResult> PerformAction(int id, string action)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
            if (ad == null) return NotFound("Ad not found or not yours.");

            ad.IsActive = action.Equals("show", StringComparison.OrdinalIgnoreCase);
            await _db.SaveChangesAsync();

            return Ok(new { message = $"Ad {(ad.IsActive ? "shown" : "hidden")} successfully." });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var ad = await _db.Advertisements.FindAsync(id);
            if (ad == null) return NotFound();

            _db.Advertisements.Remove(ad);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // 🔧 Helper mapper
        private static AdvertisementDTO MapToDTO(Advertisement ad, ApplicationUser user)
        {
            return new AdvertisementDTO
            {
                Id = ad.Id,
                Category = ad.Category,
                Name = ad.Name,
                Description = ad.Description,
                Area = ad.Area,
                Contact = ad.Contact,
                IsActive = ad.IsActive,
                AdImageBase64 = ad.AdImage != null ? Convert.ToBase64String(ad.AdImage) : null,
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
                    UserRating = user.UserRating,
                    IsSubscriptionActive = user.IsSubscriptionActive,
                    IsApproved = user.IsApproved
                }
            };
        }
    }
}
