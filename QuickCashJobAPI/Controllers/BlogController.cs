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


        // POST: api/Blog
        [HttpPost]
        public async Task<IActionResult> CreateBlog([FromBody] Blog blog)
        {
            if (string.IsNullOrWhiteSpace(blog.Title) || string.IsNullOrWhiteSpace(blog.Content))
                return BadRequest(new { message = "Title and Content are required." });

            blog.CreatedAt = DateTime.UtcNow;
            blog.UpdatedAt = null;

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
