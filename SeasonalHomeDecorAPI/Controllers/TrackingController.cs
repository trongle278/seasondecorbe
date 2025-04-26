using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TrackingController : ControllerBase
    {
        private readonly ITrackingService _trackingService;
        public TrackingController(ITrackingService trackingService)
        {
            _trackingService = trackingService;
        }

        [HttpGet("getTrackingByBookingCode")]
        public async Task<IActionResult> GetTrackingByBookingCode(string bookingCode)
        {
            var response = await _trackingService.GetTrackingByBookingCodeAsync(bookingCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("addTracking")]
        public async Task<IActionResult> AddTracking([FromForm] TrackingRequest request, string bookingCode)
        {
            var response = await _trackingService.AddTrackingAsync(request, bookingCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("updateTracking/{trackingId}")]
        public async Task<IActionResult> UpdateTracking(
    [FromRoute] int trackingId,
    [FromForm] UpdateTrackingRequest request)
        {
            var response = await _trackingService.UpdateTrackingAsync(request, trackingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
