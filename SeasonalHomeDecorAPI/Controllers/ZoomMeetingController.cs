using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZoomMeetingController : ControllerBase
    {
        private readonly IZoomService _zoomService;

        public ZoomMeetingController(IZoomService zoomService)
        {
            _zoomService = zoomService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        [Authorize]
        [HttpGet("getPaginatedList")]
        public async Task<IActionResult> GetFilterMeetingList([FromQuery] ZoomFilterRequest request)
        {
            var accountId = GetUserId();

            var result = await _zoomService.GetMeetingByBookingAsync(accountId, request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetMeetingById(int id)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomService.GetMeetingById(id);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpPost("createMeetingRequest/{bookingCode}")]
        public async Task<IActionResult> CreateMeetingRequest(string bookingCode, [FromBody] CreateMeetingRequest request)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomService.CreateMeetingRequestAsync(bookingCode, request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpPost("acceptingMeetingRequest/{bookingCode}")]
        public async Task<IActionResult> AcceptMeetingReqeust(string bookingCode)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomService.AcceptMeetingRequestAsync(bookingCode);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpPut("rejectMeetingRequest/{bookingCode}")]
        public async Task<IActionResult> RejectMeetingRequest(string bookingCode)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomService.RejectMeetingRequestAsync(bookingCode);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
    }
}
