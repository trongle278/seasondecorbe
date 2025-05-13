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
        [HttpPost("getPaginatedListForCustomer")]
        public async Task<IActionResult> GetMeetingListForCustomer([FromQuery] ZoomFilterRequest request)
        {
            var accountId = GetUserId();

            var result = await _zoomService.GetMeetingForCustomerAsync(accountId, request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpGet("getPaginatedListForProvider")]
        public async Task<IActionResult> GetMeetingListForProvider([FromQuery] ZoomFilterRequest request)
        {
            var accountId = GetUserId();

            var result = await _zoomService.GetMeetingForProviderAsync(accountId, request);

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

            var result = await _zoomService.CreateMeetingRequestAsync(bookingCode, accountId, request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpPost("acceptingMeetingRequest/{bookingCode}")]
        public async Task<IActionResult> AcceptMeetingReqeust(string bookingCode, [FromQuery] int id)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomService.AcceptMeetingRequestAsync(bookingCode, id);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpPut("rejectMeetingRequest/{bookingCode}")]
        public async Task<IActionResult> RejectMeetingRequest(string bookingCode, [FromQuery] int id)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomService.RejectMeetingRequestAsync(bookingCode, id);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpPost("createMeetingSchedule/{bookingCode}")]
        public async Task<IActionResult> CreateMeetingSchedule(string bookingCode, [FromBody] List<DateTime> scheduleTime)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomService.CreateMeetingScheduleAsync(bookingCode, accountId, scheduleTime);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpPost("selectMeetingRequest/{bookingCode}")]
        public async Task<IActionResult> SelectMeetingReqeust(string bookingCode, [FromQuery] int id)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomService.SelectMeetingAsync(bookingCode, id);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpPut("cancelMeetingRequest/{id}")]
        public async Task<IActionResult> CancelMeetingRequest(int id)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomService.CancelMeetingAsync(id);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        //[Authorize]
        //[HttpGet("join-info/{id}")]
        //public async Task<IActionResult> GetZoomJoinInfo(int id)
        //{
        //    var accountId = GetUserId();
        //    if (accountId == 0)
        //    {
        //        return Unauthorized(new { Message = "Unauthorized" });
        //    }

        //    var result = await _zoomService.GetZoomJoinInfo(id);

        //    if (result.Success)
        //    {
        //        return Ok(result);
        //    }

        //    return BadRequest(result);
        //}
    }
}
