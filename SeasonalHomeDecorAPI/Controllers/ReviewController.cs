using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Product;
using BusinessLogicLayer.ModelRequest.Review;
using BusinessLogicLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpGet("getList")]
        public async Task<IActionResult> GetAllReview()
        {
            var result = await _reviewService.GetReviewList();

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest();
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetReviewById(int id)
        {
            var result = await _reviewService.GetReviewById(id);

            if (result.Success == false && result.Message == "Invalid review")
            {
                ModelState.AddModelError("", $"Review not found!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Error retrieving review")
            {
                ModelState.AddModelError("", $"Error retrieving Review!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpGet("getReviewByService/{id}")]
        public async Task<IActionResult> GetReviewByServiceId(int id)
        {
            var result = await _reviewService.GetReviewByServiceId(id);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest();
        }

        [HttpGet("getReviewByProduct/{id}")]
        public async Task<IActionResult> GetReviceByProductId(int id)
        {
            var result = await _reviewService.GetReviewByProductId(id);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest();
        }

        [Authorize]
        [HttpPost("reviewProduct")]
        public async Task<IActionResult> CreateOrderReview([FromForm] ReviewOrderRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _reviewService.CreateOrderReview(request);

            if (result.Success == false && result.Message == "Invalid order")
            {
                ModelState.AddModelError("", $"Product has to be ordered before review!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Product reviewed")
            {
                ModelState.AddModelError("", $"Product in order has been reviewed!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Error reviewing product")
            {
                ModelState.AddModelError("", $"Error reviewing product!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [Authorize]
        [HttpPost("reviewService")]
        public async Task<IActionResult> CreateBookingReview([FromForm] ReviewBookingRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _reviewService.CreateBookingReview(request);

            if (result.Success == false && result.Message == "Invalid booking")
            {
                ModelState.AddModelError("", $"Service has to be booked before review!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Service reviewed")
            {
                ModelState.AddModelError("", $"Booking service has been reviewed!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Error reviewing service")
            {
                ModelState.AddModelError("", $"Error reviewing service!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [Authorize]
        [HttpPut("updateProductReview/{id}")]
        public async Task<IActionResult> UpdateProductReview(int id, [FromQuery] int productId, [FromQuery] int orderId, [FromForm] UpdateOrderReviewRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _reviewService.UpdateOrderReview(id, productId, orderId, request);

            if (result.Success == false && result.Message == "Invalid review")
            {
                ModelState.AddModelError("", $"Review not found!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid product")
            {
                ModelState.AddModelError("", $"Invalid product!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid order")
            {
                ModelState.AddModelError("", $"Invalid order!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Expired")
            {
                ModelState.AddModelError("", $"Review can only be updated within 3 days of creation!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Error updating review")
            {
                ModelState.AddModelError("", $"Error updating review!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [Authorize]
        [HttpPut("updateServiceReview/{id}")]
        public async Task<IActionResult> UpdateServiceReview(int id, [FromQuery] int serviceId, [FromQuery] int bookingId, [FromForm] UpdateBookingReviewRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _reviewService.UpdateBookingReview(id, serviceId, bookingId, request);

            if (result.Success == false && result.Message == "Invalid review")
            {
                ModelState.AddModelError("", $"Review not found!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid service")
            {
                ModelState.AddModelError("", $"Invalid service!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid booking")
            {
                ModelState.AddModelError("", $"Invalid booking!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Expired")
            {
                ModelState.AddModelError("", $"Review can only be updated within 3 days of creation!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Error updating review")
            {
                ModelState.AddModelError("", $"Error updating review!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [Authorize]
        [HttpDelete("deleteReview/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _reviewService.DeleteReview(id);

            if (result.Success == false && result.Message == "Invalid review")
            {
                ModelState.AddModelError("", $"Review not found!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Expired")
            {
                ModelState.AddModelError("", $"Review can only be deleted within 3 days of creation!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Error deleting review")
            {
                ModelState.AddModelError("", $"Error deleting review!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }
    }
}
