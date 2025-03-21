using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.Services;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Interfaces;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        /// <summary>
        /// Lấy danh sách booking của người dùng hiện tại (có thể là customer hoặc provider)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyBookings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Lấy ID người dùng hiện tại từ claim
                var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (accountId == 0)
                {
                    return Unauthorized(new { Message = "Unauthorized" });
                }

                // Gọi service để lấy booking của người dùng hiện tại
                var response = await _bookingService.GetBookingsByUserAsync(accountId, page, pageSize);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new BaseResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = { ex.Message }
                });
            }
        }

        /// <summary>
        /// Lấy chi tiết booking theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingDetails(int id)
        {
            try
            {
                var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (accountId == 0)
                {
                    return Unauthorized(new { Message = "Unauthorized" });
                }

                var response = await _bookingService.GetBookingDetailsAsync(id, accountId);
                if (response.Success)
                {
                    return Ok(response);
                }
                return NotFound(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new BaseResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = { ex.Message }
                });
            }
        }

        /// <summary>
        /// Customer tạo booking mới
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.CreateBookingAsync(request, accountId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Provider duyệt booking (chuyển sang Survey)
        /// </summary>
        [HttpPost("survey/{bookingId}")]
        public async Task<IActionResult> SurveyBooking(int bookingId)
        {
            var response = await _bookingService.SurveyBookingAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Provider thêm bảng báo giá (BookingDetails)
        /// </summary>
        [HttpPost("add-detail/{bookingId}")]
        public async Task<IActionResult> AddBookingDetail(int bookingId, [FromBody] BookingDetailRequest request)
        {
            var response = await _bookingService.AddBookingDetailAsync(bookingId, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Xác nhận booking và tiến hành đặt cọc
        /// </summary>
        [HttpPost("confirm/{bookingId}")]
        public async Task<IActionResult> ConfirmBooking(int bookingId)
        {
            var response = await _bookingService.ConfirmBookingAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Customer xác nhận báo giá và đặt cọc
        /// </summary>
        [HttpPost("deposit/{bookingId}")]
        public async Task<IActionResult> DepositForBooking(int bookingId)
        {
            var response = await _bookingService.DepositForBookingAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Chuyển booking sang các trạng thái tiếp theo
        /// </summary>
        [HttpPost("status/{bookingId}/{status}")]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, string status)
        {
            BaseResponse response = status.ToLower() switch
            {
                "preparing" => await _bookingService.MarkPreparingAsync(bookingId),
                "intransit" => await _bookingService.MarkInTransitAsync(bookingId),
                "progressing" => await _bookingService.MarkProgressingAsync(bookingId),
                _ => new BaseResponse { Success = false, Message = "Invalid status." }
            };

            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Customer thanh toán thi công (Tiền vào ví Admin)
        /// </summary>
        [HttpPost("pay/{bookingId}")]
        public async Task<IActionResult> PayForConstruction(int bookingId)
        {
            var response = await _bookingService.PayForConstructionAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Hoàn thành booking
        /// </summary>
        [HttpPost("complete/{bookingId}")]
        public async Task<IActionResult> CompleteBooking(int bookingId)
        {
            var response = await _bookingService.CompleteBookingAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Hủy booking
        /// </summary>
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            try
            {
                var response = await _bookingService.CancelBookingAsync(id);
                if (response.Success)
                {
                    return Ok(response);
                }
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new BaseResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = { ex.Message }
                });
            }
        }

        /// <summary>
        /// Provider cập nhật tiến độ thi công (Tracking)
        /// </summary>
        [HttpPost("tracking/{bookingId}")]
        public async Task<IActionResult> AddTracking(int bookingId, [FromBody] TrackingRequest request)
        {
            var response = await _bookingService.AddTrackingAsync(bookingId, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
