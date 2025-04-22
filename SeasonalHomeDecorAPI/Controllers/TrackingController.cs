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

        [HttpPost("getTrackingByBookingCode")]
        public async Task<IActionResult> GetTrackingByBookingCode(string bookingCode)
        {
            var result = await _trackingService.GetTrackingByBookingCodeAsync(bookingCode);
            return Ok(result);
        }

        [HttpPost("updateTracking")]
        public async Task<IActionResult> UpdateTracking([FromForm] UpdateTrackingRequest request, string bookingCode)
        {
            var result = await _trackingService.UpdateTrackingAsync(request, bookingCode);
            return Ok(result);
        }
    }
}
