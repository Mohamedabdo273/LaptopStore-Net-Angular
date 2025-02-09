using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebsitSellsLaptop.Models;
using WebsitSellsLaptop.Repository.IRepository;
using WebsitSellsLaptop.Utility;

namespace WebsitSellsLaptop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = SD.adminRole)] // Ensure this is defined correctly
    public class CategoryController : ControllerBase
    {
        private readonly ICategory _category;

        public CategoryController(ICategory category)
        {
            _category = category;
        }

        [HttpGet("GetAll")]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
            var categories = _category.Get();
            if (categories == null || !categories.Any())
                return NotFound("No categories found.");
            return Ok(categories);
        }

        [HttpPost("Create")]
        public IActionResult Create([FromBody] Category category)
        {
            if (category == null)
                return BadRequest("Category data is required.");

            // Optimized duplicate name check
            if (_category.GetOne(expression: c => c.Name == category.Name) != null)
                return BadRequest("Category name already exists.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _category.Create(category);
            _category.Commit();

            return Ok(category);
               
        }

        [HttpPut("Edit/{categoryId}")]
        public IActionResult Edit(int categoryId, [FromBody] Category category)
        {
            if (categoryId != category.Id)
                return BadRequest("Category ID mismatch.");

            var existingCategory = _category.GetOne(expression: e => e.Id == categoryId);
            if (existingCategory == null)
                return NotFound($"Category with ID {categoryId} not found.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Exclude current category when checking for duplicate name
            if (_category.GetOne(expression: c => c.Name == category.Name && c.Id != categoryId) != null)
                return BadRequest("Category name already exists.");

            existingCategory.Name = category.Name;
            _category.Edit(existingCategory);
             _category.Commit();

            return Ok("Category updated successfully.");
        }

        [HttpDelete("Delete/{categoryId}")]
        public IActionResult Delete(int categoryId)
        {
            var category = _category.GetOne(expression: e => e.Id == categoryId);
            if (category == null)
                return NotFound($"Category with ID {categoryId} not found.");

            _category.Delete(category);
             _category.Commit();
            return Ok($"Category with ID {categoryId} deleted successfully.");
        }
    }
}
