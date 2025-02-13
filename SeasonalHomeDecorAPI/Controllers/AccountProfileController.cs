using System.Security.Claims;
using BusinessLogicLayer;
using BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountProfileController : ControllerBase
    {
        private readonly IAccountProfileService _accountProfileService;

        public AccountProfileController(IAccountProfileService accountProfileService)
        {
            _accountProfileService = accountProfileService;
        }

        [HttpPut("avatar")]
        public async Task<IActionResult> UpdateAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Retrieve the user ID from the claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("User ID not found in token.");
            }

            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return BadRequest("Invalid user ID.");
            }

            using (var stream = file.OpenReadStream())
            {
                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                var response = await _accountProfileService.UpdateAvatarAsync(userId, stream, fileName);

                if (response.Success)
                {
                    return Ok(new { Message = response.Message, AvatarUrl = response.Data });
                }
                return BadRequest(response.Message);
            }
        }
    }
}
