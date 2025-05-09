using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelRequest.Product;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet("getList")]
        public async Task<IActionResult> GetAllProduct()
        {
            var result = await _productService.GetAllProduct();

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest();     
        }

        [HttpGet("getPaginatedList")]
        public async Task<IActionResult> GetFilterProduct([FromQuery] ProductFilterRequest request)
        {
            var result = await _productService.GetPaginate(request);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest();
        }

        [HttpGet("getProductByCategory/{id}")]
        public async Task<IActionResult> GetProductByCategory(int id)
        {
            var result = await _productService.GetProductByCategoryId(id);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest();
        }

        [HttpGet("getPaginatedListByCategory")]
        public async Task<IActionResult> GetFilterByCategory([FromQuery] FilterByCategoryRequest request)
        {
            var result = await _productService.GetPaginateByCategory(request);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("getProductByProvider/{slug}")]
        public async Task<IActionResult> GetProductByProvider(string slug)
        {
            var result = await _productService.GetProductByProvider(slug);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest();
        }

        [HttpGet("getPaginatedListByProvider")]
        public async Task<IActionResult> GetFilterByProvider([FromQuery] FilterByProviderRequest request)
        {
            var result = await _productService.GetPaginateByProvider(request);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var result = await _productService.GetProductById(id);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpPost("createProduct")]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _productService.CreateProduct(request);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpPut("updateProduct/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _productService.UpdateProduct(id, request);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpDelete("deleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _productService.DeleteProduct(id);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMultiCriteria(
        [FromQuery] string? ProductName,
        [FromQuery] string? CategoryName,
        [FromQuery] string? ShipFrom,
        [FromQuery] string? MadeIn)
        {
            var request = new SearchProductRequest
            {
                ProductName = ProductName,
                CategoryName = CategoryName,
                ShipFrom = ShipFrom,
                MadeIn = MadeIn
            };

            var result = await _productService.SearchMultiCriteriaProduct(request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
