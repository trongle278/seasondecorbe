using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Cart;
using BusinessLogicLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateCart([FromBody] CartRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _cartService.CreateCartAsync(request);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result.Errors);
        }

        [Authorize]
        [HttpGet("getCart/{id}")]
        public async Task<IActionResult> GetCartByAccountId(int id)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _cartService.GetCart(id);

            if (result.Success == false && result.Message == "Invalid cart")
            {
                ModelState.AddModelError("", $"Cart not found!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Error retrieving cart")
            {
                ModelState.AddModelError("", $"Error retrieving cart!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [Authorize]
        [HttpPost("addToCart/{id}")]
        public async Task<IActionResult> AddToCart(int id, int productId, int quantity)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _cartService.AddToCart(id, productId, quantity);

            if (result.Success == false && result.Message == "Invalid cart")
            {
                ModelState.AddModelError("", $"Cart not found!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid product")
            {
                ModelState.AddModelError("", $"Product not found!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Not enough existing product")
            {
                ModelState.AddModelError("", $"Insufficent product quantity.");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Product quantity has to be > 0")
            {
                ModelState.AddModelError("", $"Product quantity has to be > 0.");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Error adding product to cart")
            {
                ModelState.AddModelError("", $"Error adding item!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [Authorize]
        [HttpPut("updateQuantity/{id}")]
        public async Task<IActionResult> EditProductQuantity(int id, int productId, int quantity)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _cartService.UpdateProductQuantity(id, productId, quantity);

            if (result.Success == false && result.Message == "Invalid cart")
            {
                ModelState.AddModelError("", $"Cart not found!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid cartItem")
            {
                ModelState.AddModelError("", $"Cart item not found!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid product")
            {
                ModelState.AddModelError("", $"Product not found!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Not enough existing product")
            {
                ModelState.AddModelError("", $"Insufficent product quantity.");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Product quantity has to be > 0")
            {
                ModelState.AddModelError("", $"Product quantity has to be > 0.");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Error updating product in cart")
            {
                ModelState.AddModelError("", $"Error updating item!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [Authorize]
        [HttpDelete("removeProduct/{id}")]
        public async Task<IActionResult> RemoveProductFromCart(int id, int productId)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _cartService.RemoveProduct(id, productId);

            if (result.Success == false && result.Message == "Invalid cart")
            {
                ModelState.AddModelError("", $"Cart not found!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid product")
            {
                ModelState.AddModelError("", $"Product not found!");
            }

            if (result.Success == false && result.Message == "Error removing product in cart")
            {
                ModelState.AddModelError("", $"Error removing item!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }
    }
}
