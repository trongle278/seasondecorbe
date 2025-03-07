using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.Services;
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

        [HttpGet("profile/{slug}")]
        public async Task<IActionResult> GetProviderProfileBySlug(string slug)
        {
            var response = await _providerService.GetProviderProfileBySlugAsync(slug);
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

        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProviderProfile([FromBody] UpdateProviderRequest request)
        {
            if (request == null)
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "Invalid request data" }
                });
            }

            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null)
            {
                return Unauthorized("Account ID not found in token.");
            }
            int accountId = int.Parse(accountIdClaim.Value);

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
    }
}
