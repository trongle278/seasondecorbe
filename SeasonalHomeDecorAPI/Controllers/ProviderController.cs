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
    [Authorize]
    public class ProviderController : ControllerBase
    {
        private readonly IProviderService _providerService;

        public ProviderController(IProviderService providerService)
        {
            _providerService = providerService;
        }

        [HttpPost("create-profile")]
        public async Task<IActionResult> CreateProviderProfile([FromBody] BecomeProviderRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Extract accountId from JWT token claims
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
            else
            {
                return BadRequest(response);
            }
        }

        [HttpPut("change-status")]
        public async Task<IActionResult> ChangeProviderStatus([FromBody] bool isProvider)
        {
            // Extract accountId from JWT token claims
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
            return BadRequest(response);
        }
    }
}
