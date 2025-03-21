using System.Security.Claims;
using BusinessLogicLayer;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductCategoryController : ControllerBase
    {
        private readonly IProductCategoryService _productCategoryService;

        public ProductCategoryController(IProductCategoryService productCategoryService)
        {
            _productCategoryService = productCategoryService;
        }

        [HttpGet("getList")]
        public async Task<IActionResult> GetAllProductCategory()
        {
            var result = await _productCategoryService.GetAllProductCategory();

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest();
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetProductCategoryById(int id)
        {
            var result = await _productCategoryService.GetProductCategoryById(id);

            if (result.Success == false && result.Message == "Invalid product category")
            {
                ModelState.AddModelError("", $"Product category not found!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Error retrieving product category")
            {
                ModelState.AddModelError("", $"Error retrieving product category!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [Authorize]
        [HttpPost("createProductCategory")]
        public async Task<IActionResult> CreateProductCategory(ProductCategoryRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _productCategoryService.CreateProductCategory(request);

            if (result.Success == false && result.Message == "Invalid product category request")
            {
                ModelState.AddModelError("", $"Invalid product request!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Product category name is required")
            {
                ModelState.AddModelError("", $"Product category name is required!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Error creating product category")
            {
                ModelState.AddModelError("", $"Error creating product category");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [Authorize]
        [HttpPut("updateProductCategory/{id}")]
        public async Task<IActionResult> UpdateProductCategory(int id, ProductCategoryRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _productCategoryService.UpdateProductCategory(id, request);

            if (result.Success == false && result.Message == "Invalid product category")
            {
                ModelState.AddModelError("", $"Product category not found!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Product category name is required")
            {
                ModelState.AddModelError("", $"Product category name is required!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Error updating product category")
            {
                ModelState.AddModelError("", $"Error updating product category!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [Authorize]
        [HttpDelete("deleteProductCategory/{id}")]
        public async Task<IActionResult> DeleteProductCategory(int id)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _productCategoryService.DeleteProductCategory(id);

            if (result.Success == false && result.Message == "Invalid product category")
            {
                ModelState.AddModelError("", $"Product category not found!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Error deleting product category")
            {
                ModelState.AddModelError("", $"Error deleting product category!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }
    }
}
