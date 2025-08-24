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
            var blogs = await _db.blogs
                .Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.Content,
                    //ImageBase64 = b.ImageUrl != null ? Convert.ToBase64String(b.ImageUrl) : null,
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

            var response = new
            {
                blog.Id,
                blog.Title,
                blog.Content,
                ImageBase64 = blog.ImageUrl != null ? Convert.ToBase64String(blog.ImageUrl) : null,
                blog.CreatedAt,
                blog.UpdatedAt
            };

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBlog([FromForm] string title, [FromForm] string content, [FromForm] IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                return BadRequest(new { message = "Title and Content are required." });

            var blog = new Blog
            {
                Title = title,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            if (imageFile != null)
            {
                using var memoryStream = new MemoryStream();
                await imageFile.CopyToAsync(memoryStream);
                blog.ImageUrl = memoryStream.ToArray();
            }

            _db.blogs.Add(blog);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBlog), new { id = blog.Id }, blog);
        }


        // PUT: api/Blog/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBlog(int id, [FromBody] Blog updatedBlog)
        {
            var existingBlog = await _db.blogs.FindAsync(id);
            if (existingBlog == null) return NotFound();

            existingBlog.Title = updatedBlog.Title;
            existingBlog.Content = updatedBlog.Content;
            existingBlog.ImageUrl = updatedBlog.ImageUrl; // Firebase image URL
            existingBlog.UpdatedAt = DateTime.UtcNow;

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
