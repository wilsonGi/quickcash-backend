using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models;
using System.Reflection.Metadata;

namespace QuickCashJobAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public BlogController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetBlogs()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}"; // Get API base URL dynamically

            var blogs = await _db.blogs
                .Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.Content,
                    ImageUrl = b.ImageUrl != null ? $"{baseUrl}{b.ImageUrl}" : null,
                    b.CreatedAt,
                    b.UpdatedAt
                })
                .ToListAsync();

            return Ok(blogs);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetBlog(int id)
        {
            var blog = await _db.blogs.FindAsync(id);
            if (blog == null) return NotFound();

            var baseUrl = $"{Request.Scheme}://{Request.Host}"; // Ensure full URL
            var response = new
            {
                blog.Id,
                blog.Title,
                blog.Content,
                ImageUrl = blog.ImageUrl != null ? $"{baseUrl}{blog.ImageUrl}" : null,
                blog.CreatedAt,
                blog.UpdatedAt
            };

            return Ok(response);
        }


     
        [HttpPost]
        public async Task<IActionResult> CreateBlog([FromForm] Blog blog, IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(blog.Title) || string.IsNullOrWhiteSpace(blog.Content))
            {
                return BadRequest(new { message = "Title and Content are required." });
            }

            blog.CreatedAt = DateTime.UtcNow; // ✅ Ensure CreatedAt is always set

            if (imageFile != null)
            {
                var uploadsFolder = Path.Combine("wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, imageFile.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                blog.ImageUrl = "/uploads/" + imageFile.FileName; // ✅ Only set if an image is provided
            }

            _db.blogs.Add(blog);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBlog), new { id = blog.Id }, blog);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBlog(int id, [FromForm] Blog blog, IFormFile imageFile)
        {
            var existingBlog = await _db.blogs.FindAsync(id);
            if (existingBlog == null) return NotFound();

            existingBlog.Title = blog.Title;
            existingBlog.Content = blog.Content;
            existingBlog.UpdatedAt = DateTime.UtcNow;

            if (imageFile != null)
            {
                var filePath = Path.Combine("wwwroot/uploads", imageFile.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                existingBlog.ImageUrl = "/uploads/" + imageFile.FileName;
            }

            _db.blogs.Update(existingBlog);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            var blog = await _db.blogs.FindAsync(id);
            if (blog == null) return NotFound();

            _db.blogs.Remove(blog);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
