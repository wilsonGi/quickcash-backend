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

            return Ok(new
            {
                jobs
            });
        }
    }
}
