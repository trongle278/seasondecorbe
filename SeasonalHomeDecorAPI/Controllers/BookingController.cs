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
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        /// <summary>
        /// Tạo booking (trạng thái ban đầu là Pending).
        /// </summary>
        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
            {
                return Unauthorized(new BaseResponse { Success = false, Message = "Invalid token." });
            }

            int accountId = int.Parse(userIdClaim.Value);
            var response = await _bookingService.CreateBookingAsync(request, accountId);
            if (response.Success) return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// API tự động chuyển trạng thái booking sang giai đoạn tiếp theo dựa trên trạng thái hiện tại.
        /// Chỉ cần nhập bookingId.
        /// Ví dụ: nếu booking đang ở Surveying và Deposit đã hoàn thành, sẽ chuyển sang Procuring.
        /// </summary>
        [HttpPut("advance/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> AdvanceBookingPhase(int bookingId)
        {
            var response = await _bookingService.AdvanceBookingPhaseAsync(bookingId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Sau khảo sát, customer đặt cọc (Deposit): Surveying -> (Deposit phase tạo mới) -> (sau đó chuyển sang Procuring qua API riêng nếu cần).
        /// </summary>
        [HttpPost("deposit/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> ApproveSurveyAndDeposit(int bookingId, [FromBody] PaymentRequest paymentRequest)
        {
            // PaymentRequest chỉ chứa Total
            double depositAmount = paymentRequest.Total;
            var response = await _bookingService.ApproveSurveyAndDepositAsync(bookingId, depositAmount);
            if (response.Success) return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Khi thi công xong, customer thanh toán cuối (FinalPayment): Progressing -> (Final phase tạo mới) -> Completed.
        /// </summary>
        [HttpPost("complete/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> CompleteBooking(int bookingId, [FromBody] PaymentRequest paymentRequest)
        {
            double finalAmount = paymentRequest.Total;
            var response = await _bookingService.CompleteBookingAsync(bookingId, finalAmount);
            if (response.Success) return Ok(response);
            return BadRequest(response);
        }        

        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetBookingHistory()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
                return Unauthorized(new BaseResponse { Success = false, Message = "Invalid token." });

            int accountId = int.Parse(userIdClaim.Value);
            var response = await _bookingService.GetBookingHistoryAsync(accountId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Hủy booking.
        /// </summary>
        [HttpPut("cancel/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var response = await _bookingService.CancelBookingAsync(bookingId);
            if (response.Success) return Ok(response);
            return BadRequest(response);
        }

    }
}
