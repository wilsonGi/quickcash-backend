using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models.DTO;
using QuickCashJobAPI.Services;

namespace QuickCashJobAPI.Controllers
{
    
    [Route("api/Category")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<CategoryDTO>> GetCategories()
        {
            var categories = _db.Categories.ToList();
            var categoryDTOs = categories.Select(c => new CategoryDTO
            {
                Id = c.Id,
                CategoryName = c.CategoryName,
                NumberOfInstances = c.NumberOfInstances,
                CategoryImage = c.CategoryImage
            }).ToList();


            return Ok(categoryDTOs);
        }

        [HttpGet("{id:int}", Name = "GetCategory")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<CategoryDTO> GetCategory(int id)
        {
            var category = _db.Categories.FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            var categoryDTO = new CategoryDTO
            {
                Id = category.Id,
                CategoryName = category.CategoryName,
                NumberOfInstances = category.NumberOfInstances,
                CategoryImage = category.CategoryImage
            };

            return Ok(categoryDTO);
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CategoryDTO>> CreateCategory([FromBody] CategoryDTO categoryDTO)
        {
            if (categoryDTO == null || string.IsNullOrWhiteSpace(categoryDTO.CategoryImage))
            {
                return BadRequest("Invalid category data or image URL missing.");
            }

            var category = new Category
            {
                CategoryName = categoryDTO.CategoryName,
                NumberOfInstances = categoryDTO.NumberOfInstances,
                CategoryImage = categoryDTO.CategoryImage
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            categoryDTO.Id = category.Id;
            return CreatedAtRoute("GetCategory", new { id = category.Id }, categoryDTO);
        }


        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}", Name = "UpdateCategory")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDTO categoryDTO)
        {
            if (categoryDTO == null || id != categoryDTO.Id)
            {
                return BadRequest();
            }

            var category = _db.Categories.FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            category.CategoryName = categoryDTO.CategoryName;
            category.NumberOfInstances = categoryDTO.NumberOfInstances;
            category.CategoryImage = categoryDTO.CategoryImage;

            _db.Categories.Update(category);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpDelete("{id:int}", Name = "DeleteCategory")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = _db.Categories.FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
