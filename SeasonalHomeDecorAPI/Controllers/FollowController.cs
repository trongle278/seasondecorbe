using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FollowController : ControllerBase
    {
        private readonly IFollowService _followService;

        public FollowController(IFollowService followService)
        {
            _followService = followService;
        }

        // POST: api/follow/follow?followingId=xxx&isNotify=true
        [HttpPost("follow")]
        public async Task<IActionResult> Follow([FromQuery] int followingId)
        {
            try
            {
                // Lấy userId (followerId) từ token
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _followService.FollowAsync(userId, followingId);
                if (result.Success)
                    return Ok(result);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    Message = "Error occurred.",
                    Errors = { ex.Message }
                });
            }
        }

        // DELETE: api/follow/unfollow?followingId=xxx
        [HttpDelete("unfollow")]
        public async Task<IActionResult> Unfollow([FromQuery] int followingId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _followService.UnfollowAsync(userId, followingId);
                if (result.Success)
                    return Ok(result);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    Message = "Error occurred.",
                    Errors = { ex.Message }
                });
            }
        }

        // GET: api/follow/followers
        [HttpGet("followers")]
        public async Task<IActionResult> GetFollowers()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _followService.GetFollowersAsync(userId);
                if (result.Success)
                    return Ok(result);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    Message = "Error occurred.",
                    Errors = { ex.Message }
                });
            }
        }

        // GET: api/follow/followings
        [HttpGet("followings")]
        public async Task<IActionResult> GetFollowings()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _followService.GetFollowingsAsync(userId);
                if (result.Success)
                    return Ok(result);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    Message = "Error occurred.",
                    Errors = { ex.Message }
                });
            }
        }

        // GET: api/follow/counts
        [HttpGet("counts")]
        public async Task<IActionResult> GetFollowCounts()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _followService.GetFollowCountsAsync(userId);
                if (result.Success)
                    return Ok(result);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    Message = "Error occurred.",
                    Errors = { ex.Message }
                });
            }
        }
    }
}
