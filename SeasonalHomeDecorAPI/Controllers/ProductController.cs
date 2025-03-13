using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelRequest.Product;
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

            if (result.Success == false && result.Message == "Non product available")
            {
                ModelState.AddModelError("", $"Non product available!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Error retrieving product list")
            {
                ModelState.AddModelError("", $"Error retrieving product list!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);            
        }

        [HttpGet("getPaginatedList")]
        public async Task<IActionResult> GetFilterProduct([FromQuery] ProductFilterRequest request)
        {
            var result = await _productService.GetPaginate(request);

            if (result.Success == false && result.Message == "Non product available")
            {
                ModelState.AddModelError("", $"Non product available!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Error retrieving product list")
            {
                ModelState.AddModelError("", $"Error retrieving product list!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpGet("getProductByCategory/{id}")]
        public async Task<IActionResult> GetProductByCategory(int id)
        {
            var result = await _productService.GetProductByCategoryId(id);

            if (result.Success == false && result.Message == "Non product available")
            {
                ModelState.AddModelError("", $"Non product available!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Error retrieving product list")
            {
                ModelState.AddModelError("", $"Error retrieving product list!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpGet("getPaginatedListByCategory")]
        public async Task<IActionResult> GetFilterByCategory([FromQuery] FilterByCategoryRequest request)
        {
            var result = await _productService.GetPaginateByCategory(request);

            if (result.Success == false && result.Message == "Non product available")
            {
                ModelState.AddModelError("", $"Non product available!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Error retrieving product list")
            {
                ModelState.AddModelError("", $"Error retrieving product list!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpGet("getPaginatedListByProvider")]
        public async Task<IActionResult> GetFilterByProvider([FromQuery] FilterByProviderRequest request)
        {
            var result = await _productService.GetPaginateByProvider(request);

            if (result.Success == false && result.Message == "Non product available")
            {
                ModelState.AddModelError("", $"Non product available!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Error retrieving product list")
            {
                ModelState.AddModelError("", $"Error retrieving product list!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpGet("getProductByProvider/{slug}")]
        public async Task<IActionResult> GetProductByProvider(string slug)
        {
            var result = await _productService.GetProductByProvider(slug);

            if (result.Success == false && result.Message == "Non product available")
            {
                ModelState.AddModelError("", $"Non product available!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Error retrieving product list")
            {
                ModelState.AddModelError("", $"Error retrieving product list!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var result = await _productService.GetProductById(id);

            if (result.Success == false && result.Message == "Invalid product")
            {
                ModelState.AddModelError("", $"Product not found!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Error retrieving product")
            {
                ModelState.AddModelError("", $"Error retrieving product!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpPost("createProduct")]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductRequest request)
        {
            var result = await _productService.CreateProduct(request);

            if (result.Success == false && result.Message == "Invalid product request")
            {
                ModelState.AddModelError("", $"Invalid product request!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Product name is required")
            {
                ModelState.AddModelError("", $"Product name is required!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Negative product price")
            {
                ModelState.AddModelError("", $"Product price has to be > 0.!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Negative quantity")
            {
                ModelState.AddModelError("", $"Product quantity has to be > 0.");
                return StatusCode(403, ModelState);
            }
            
            if (result.Success == false && result.Message == "Maximum 5 images")
            {
                ModelState.AddModelError("", $"Product Images cannot exceed 5.");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Error creating product")
            {
                ModelState.AddModelError("", $"Error creating product!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpPut("updateProduct/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductRequest request)
        {
            var result = await _productService.UpdateProduct(id, request);

            if (result.Success == false && result.Message == "Invalid product")
            {
                ModelState.AddModelError("", $"Product not found!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Product name is required")
            {
                ModelState.AddModelError("", $"Product name is required!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Negative product price")
            {
                ModelState.AddModelError("", $"Product price has to be > 0.!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Negative quantity")
            {
                ModelState.AddModelError("", $"Product quantity has to be > 0.");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Maximum 5 images")
            {
                ModelState.AddModelError("", $"Product Images cannot exceed 5.");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Error updating product")
            {
                ModelState.AddModelError("", $"Error updating product!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpDelete("deleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProduct(id);

            if (result.Success == false && result.Message == "Invalid product")
            {
                ModelState.AddModelError("", $"Product not found!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Error deleting product")
            {
                ModelState.AddModelError("", $"Error deleting product!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }
    }
}
