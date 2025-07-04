﻿using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse;
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
        private readonly IZoomOAuthService _zoomOAuthService;

        public ZoomMeetingController(IZoomService zoomService, IZoomOAuthService zoomOAuthService)
        {
            _zoomService = zoomService;
            _zoomOAuthService = zoomOAuthService;
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
        [HttpGet("getProviderMeetingForCustomer")]
        public async Task<IActionResult> GetProviderMeetingsForCustomer([FromQuery] ZoomFilterRequest request)
        {
            var accountId = GetUserId();

            var result = await _zoomService.GetProviderMeetingsForCustomerAsync(request);

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
        [HttpPut("endMeeting/{bookingCode}")]
        public async Task<IActionResult> EndMeeting(string bookingCode, [FromQuery] int id)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomService.EndMeetingAsync(bookingCode, id);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        //[Authorize]
        //[HttpPost("createMeetingSchedule/{bookingCode}")]
        //public async Task<IActionResult> CreateMeetingSchedule(string bookingCode, [FromBody] List<DateTime> scheduleTime)
        //{
        //    var accountId = GetUserId();
        //    if (accountId == 0)
        //    {
        //        return Unauthorized(new { Message = "Unauthorized" });
        //    }

        //    var result = await _zoomService.CreateMeetingScheduleAsync(bookingCode, accountId, scheduleTime);

        //    if (result.Success)
        //    {
        //        return Ok(result);
        //    }

        //    return BadRequest(result);
        //}

        //[Authorize]
        //[HttpPost("selectMeetingRequest/{bookingCode}")]
        //public async Task<IActionResult> SelectMeetingReqeust(string bookingCode, [FromQuery] int id)
        //{
        //    var accountId = GetUserId();
        //    if (accountId == 0)
        //    {
        //        return Unauthorized(new { Message = "Unauthorized" });
        //    }

        //    var result = await _zoomService.SelectMeetingAsync(bookingCode, id);

        //    if (result.Success)
        //    {
        //        return Ok(result);
        //    }

        //    return BadRequest(result);
        //}

        //[Authorize]
        //[HttpPut("cancelMeetingRequest/{id}")]
        //public async Task<IActionResult> CancelMeetingRequest(int id)
        //{
        //    var accountId = GetUserId();
        //    if (accountId == 0)
        //    {
        //        return Unauthorized(new { Message = "Unauthorized" });
        //    }

        //    var result = await _zoomService.CancelMeetingAsync(id);

        //    if (result.Success)
        //    {
        //        return Ok(result);
        //    }

        //    return BadRequest(result);
        //}

        //[Authorize]
        //[HttpGet("oauth/authorize")]
        //public IActionResult Authorize()
        //{
        //    var result = _zoomOAuthService.GenerateZoomAuthorizeUrl();
        //    if (result.Success)
        //    {
        //        return Ok(result);
        //    }

        //    return BadRequest(result);
        //}

        [Authorize]
        [HttpGet("oauth/callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomOAuthService.GetAccessTokenAsync(code);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpPost("oauth/acceptMeetingRequest/{bookingCode}")]
        public async Task<IActionResult> OAuthAcceptMeeting(string bookingCode, [FromQuery] int id, [FromBody] ZoomOAuthRequest request)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomOAuthService.AcceptMeetingRequestAsync(bookingCode, id, request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpPut("oauth/endMeeting/{bookingCode}")]
        public async Task<IActionResult> OAuthEndMeeting(string bookingCode, [FromQuery] int id, [FromBody] ZoomOAuthRequest request)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomOAuthService.EndMeetingAsync(bookingCode, id, request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpGet("oauth/join-info/{id}")]
        public async Task<IActionResult> GetZoomJoinInfo(int id)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _zoomOAuthService.GetZoomJoinInfo(id);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
    }
}
