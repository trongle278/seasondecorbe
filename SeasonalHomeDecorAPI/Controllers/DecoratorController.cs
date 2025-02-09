using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelResponse;
using Common.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.ModelRequest;

namespace SeasonalHomeDecorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DecoratorController : ControllerBase
    {
        private readonly IDecoratorService _decoratorService;

        public DecoratorController(IDecoratorService decoratorService)
        {
            _decoratorService = decoratorService;
        }

        [HttpPost("send-invitation")]
        public async Task<ActionResult<BaseResponse>> SendInvitation([FromBody] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "Email is required" }
                });
            }

            var response = await _decoratorService.SendDecoratorInvitationEmailAsync(email);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpPost("profile")]
        public async Task<ActionResult<BaseResponse>> CreateProfile([FromBody] BecomeDecoratorRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "User not found" }
                });
            }

            var response = await _decoratorService.CreateDecoratorProfileAsync(int.Parse(userId), request);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<DecoratorResponse>> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new DecoratorResponse
                {
                    Success = false,
                    Errors = new List<string> { "User not found" }
                });
            }

            var response = await _decoratorService.GetDecoratorProfileAsync(int.Parse(userId));
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<BaseResponse>> UpdateStatus(int id, [FromBody] DecoratorApplicationStatus status)
        {
            var response = await _decoratorService.UpdateDecoratorStatusAsync(id, status);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
