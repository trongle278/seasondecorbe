using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    
    public class ProviderController : ControllerBase
    {
        private readonly IProviderService _providerService;

        public ProviderController(IProviderService providerService)
        {
            _providerService = providerService;
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllProviders()
        {
            var response = await _providerService.GetAllProvidersAsync();
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // Endpoint mới để lấy Provider profile theo accountId
        [HttpGet("myprofile")]
        [Authorize]
        public async Task<IActionResult> GetMyProviderProfile()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null)
            {
                return Unauthorized("Account ID not found in token.");
            }
            int accountId = int.Parse(accountIdClaim.Value);
            var response = await _providerService.GetProviderProfileByAccountIdAsync(accountId);
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpGet("profile/{accountId}")]
        public async Task<IActionResult> GetProviderProfileByAccountId(int accountId)
        {
            var response = await _providerService.GetProviderProfileByAccountIdAsync(accountId);
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("create-profile")]
        [Authorize]
        public async Task<IActionResult> CreateProviderProfile([FromBody] BecomeProviderRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Lấy accountId từ claims của token
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null)
            {
                return Unauthorized("Account ID not found in token.");
            }

            int accountId = int.Parse(accountIdClaim.Value);
            var response = await _providerService.CreateProviderProfileAsync(accountId, request);
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("send-invitation")]
        [Authorize]
        public async Task<IActionResult> SendProviderInvitationEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email is required.");
            }

            var response = await _providerService.SendProviderInvitationEmailAsync(email);
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPut("update-profile/{accountId}")]
        [Authorize]
        public async Task<IActionResult> UpdateProviderProfile(int accountId, [FromBody] UpdateProviderRequest request)
        {
            if (request == null)
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "Invalid request data" }
                });
            }

            var response = await _providerService.UpdateProviderProfileByAccountIdAsync(accountId, request);
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPut("change-status")]
        [Authorize]
        public async Task<IActionResult> ChangeProviderStatus([FromBody] bool isProvider)
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null)
            {
                return Unauthorized("Account ID not found in token.");
            }

            int accountId = int.Parse(accountIdClaim.Value);
            var response = await _providerService.ChangeProviderStatusByAccountIdAsync(accountId, isProvider);

            if (response.Success)
            {
                return Ok(response);
            }

            // Directly return the response if it fails
            return BadRequest(response);
        }

        [HttpPut("upload-provider-avatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

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
                var response = await _providerService.UploadProviderAvatarAsync(userId, stream, fileName);
                if (response.Success)
                {
                    return Ok(new { Message = response.Message, AvatarUrl = response.Data });
                }
                return BadRequest(response.Message);
            }
        }       
    }
}
