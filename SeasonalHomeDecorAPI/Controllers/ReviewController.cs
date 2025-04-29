using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Pagination;
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

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        [HttpGet("getList")]
        public async Task<IActionResult> GetAllReview()
        {
            var result = await _reviewService.GetReviewList();

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetReviewById(int id)
        {
            var result = await _reviewService.GetReviewById(id);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpGet("getReviewByAccount")]
        public async Task<IActionResult> GetReviewByAccountId([FromQuery] ReviewFilterRequest request)
        {
            var account = GetUserId();

            var result = await _reviewService.GetReviewByAccount(account, request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [HttpGet("getReviewByService/{id}")]
        public async Task<IActionResult> GetReviewByServiceId(int id, [FromQuery] ReviewServiceFilterRequest request)
        {
            var result = await _reviewService.GetReviewByServiceId(id, request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [HttpGet("getReviewByProduct/{id}")]
        public async Task<IActionResult> GetReviceByProductId(int id, [FromQuery] ReviewProductFilterRequest request)
        {
            var result = await _reviewService.GetReviewByProductId(id, request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpPost("reviewProduct")]
        public async Task<IActionResult> CreateOrderReview([FromForm] ReviewOrderRequest request)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _reviewService.CreateOrderReview(accountId, request);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpPost("reviewService")]
        public async Task<IActionResult> CreateBookingReview([FromForm] ReviewBookingRequest request)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _reviewService.CreateBookingReview(accountId, request);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpPut("updateProductReview/{id}")]
        public async Task<IActionResult> UpdateProductReview(int id, [FromQuery] int productId, [FromQuery] int orderId, [FromForm] UpdateOrderReviewRequest request)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _reviewService.UpdateOrderReview(id, productId, orderId, request);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpPut("updateServiceReview/{id}")]
        public async Task<IActionResult> UpdateServiceReview(int id, [FromQuery] int bookingId, [FromForm] UpdateBookingReviewRequest request)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _reviewService.UpdateBookingReview(id, bookingId, request);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpDelete("deleteReview/{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _reviewService.DeleteReview(id);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
