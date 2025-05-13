using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;
using System.Linq;

namespace QuickCashJobAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            query = query?.ToLower() ?? "";

            var jobs = await _context.Jobs
                .Include(j => j.Category)
                .Where(j =>
                    j.Description.ToLower().Contains(query) ||
                    j.Location.ToLower().Contains(query) ||
                    j.Category.CategoryName.ToLower().Contains(query) ||
                    j.ContractorName.ToLower().Contains(query)
                )
                .Select(j => new
                {
                    j.Id,
                    j.Description,
                    j.Location,
                    Category = j.Category.CategoryName,
                    j.Payout
                })
                .ToListAsync();

            var users = await _context.Users
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Where(u =>
                    u.Name.ToLower().Contains(query) ||
                    u.UserSkills.Any(us => us.Skill.Name.ToLower().Contains(query))
                )
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    Skills = u.UserSkills.Select(us => us.Skill.Name).ToList()
                })
                .ToListAsync();

            var categories = await _context.Categories
                .Where(c => c.CategoryName.ToLower().Contains(query))
                .Select(c => new
                {
                    c.Id,
                    c.CategoryName,
                    c.CategoryImage
                })
                .ToListAsync();

            return Ok(new
            {
                jobs,
                users,
                categories
            });
        }
    }
}
