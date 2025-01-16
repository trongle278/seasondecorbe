using BusinessLogicLayer.Interfaces;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using Microsoft.AspNetCore.Authorization;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DecorCategoryController : ControllerBase
    {
        private readonly IDecorCategoryService _decorCategoryService;

        public DecorCategoryController(IDecorCategoryService decorCategoryService)
        {
            _decorCategoryService = decorCategoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await _decorCategoryService.GetAllDecorCategoriesAsync();
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
       
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int categoryId)
        {
            var result = await _decorCategoryService.GetDecorCategoryByIdAsync(categoryId);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        [Authorize(Policy = "RequireDecoratorRole")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] DecorCategoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _decorCategoryService.CreateDecorCategoryAsync(request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [Authorize(Policy = "RequireDecoratorRole")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int categoryId, [FromBody] DecorCategoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _decorCategoryService.UpdateDecorCategoryAsync(categoryId, request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [Authorize(Policy = "RequireDecoratorRole")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var result = await _decorCategoryService.DeleteDecorCategoryAsync(categoryId);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
