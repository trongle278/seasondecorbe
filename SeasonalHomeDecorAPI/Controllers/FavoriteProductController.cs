using BusinessLogicLayer.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoriteProductController : ControllerBase
    {
        private readonly IFavoriteProductService _favoriteProduct;

        public FavoriteProductController(IFavoriteProductService favoriteProduct)
        {
            _favoriteProduct = favoriteProduct;
        }

        [HttpGet("productList")]
        public async Task<IActionResult> GetFavoriteProduct()
        {
            int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var response = await _favoriteProduct.GetFavoriteProduct(accountId);
            
            if (response.Success)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }

        [HttpPost("{productId}")]
        public async Task<IActionResult> AddToFavorites(int productId)
        {
            int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var response = await _favoriteProduct.AddToFavorite(accountId, productId);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            return BadRequest(response);
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFromFavorites(int productId)
        {
            int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var response = await _favoriteProduct.RemoveFromFavorite(accountId, productId);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            return BadRequest(response);
        }
    }
}
