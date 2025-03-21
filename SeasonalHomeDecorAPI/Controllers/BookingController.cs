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
        /// Tạo booking mới (chỉ dành cho customer)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            try
            {
                // Lấy ID người dùng hiện tại từ claim
                var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (accountId == 0)
                {
                    return Unauthorized(new { Message = "Unauthorized" });
                }

                var response = await _bookingService.CreateBookingAsync(request, accountId);
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
        /// [Provider] Chuyển booking sang trạng thái Survey
        /// </summary>
        [HttpPut("{id}/survey")]
        public async Task<IActionResult> SurveyBooking(int id)
        {
            try
            {
                var response = await _bookingService.SurveyBookingAsync(id);
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
        /// Xác nhận booking và tiến hành đặt cọc
        /// </summary>
        [HttpPost("confirm/{bookingId}")]
        public async Task<IActionResult> ConfirmBooking(int bookingId, [FromBody] decimal depositAmount)
        {
            var response = await _bookingService.ConfirmBookingAsync(bookingId, depositAmount);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// [Provider] Chuyển booking sang trạng thái Preparing sau khi đã đặt cọc
        /// </summary>
        [HttpPut("{id}/preparing")]
        public async Task<IActionResult> MarkPreparing(int id)
        {
            try
            {
                var response = await _bookingService.MarkPreparingAsync(id);
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
        /// [Provider] Chuyển booking sang trạng thái InTransit
        /// </summary>
        [HttpPut("{id}/in-transit")]
        public async Task<IActionResult> MarkInTransit(int id)
        {
            try
            {
                var response = await _bookingService.MarkInTransitAsync(id);
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
        /// [Provider] Chuyển booking sang trạng thái Progressing (thi công)
        /// </summary>
        [HttpPut("{id}/progressing")]
        public async Task<IActionResult> MarkProgressing(int id)
        {
            try
            {
                var response = await _bookingService.MarkProgressingAsync(id);
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
        /// Thanh toán thi công
        /// </summary>
        [HttpPost("construction-payment/{bookingId}")]
        public async Task<IActionResult> MarkConstructionPayment(int bookingId)
        {
            var response = await _bookingService.MarkConstructionPaymentAsync(bookingId);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
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
    }
}
