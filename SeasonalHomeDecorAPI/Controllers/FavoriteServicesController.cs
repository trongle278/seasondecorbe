using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoriteServiceController : ControllerBase
    {
        private readonly IFavoriteServiceService _favoriteService;

        public FavoriteServiceController(IFavoriteServiceService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        [HttpGet("myfavorite")]
        public async Task<IActionResult> GetFavoriteDecorServices()
        {
            int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var response = await _favoriteService.GetFavoriteServicesAsync(accountId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        [HttpPost("{decorServiceId}")]
        public async Task<IActionResult> AddToFavorites(int decorServiceId)
        {
            int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var response = await _favoriteService.AddToFavoritesAsync(accountId, decorServiceId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        [HttpDelete("{decorServiceId}")]
        public async Task<IActionResult> RemoveFromFavorites(int decorServiceId)
        {
            int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var response = await _favoriteService.RemoveFromFavoritesAsync(accountId, decorServiceId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }
    }
} 