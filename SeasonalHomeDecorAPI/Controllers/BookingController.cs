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
            // Lấy AccountId từ token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
            {
                return Unauthorized(new BaseResponse { Success = false, Message = "Invalid token." });
            }

            int accountId = int.Parse(userIdClaim.Value);

            // Gọi service tạo booking, bên service bạn sẽ sử dụng accountId lấy từ token
            var response = await _bookingService.CreateBookingAsync(request, accountId);
            if (response.Success) return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Provider xác nhận booking: Pending -> Confirmed.
        /// </summary>
        [HttpPut("confirm/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> ConfirmBooking(int bookingId)
        {
            // (Tuỳ chọn) Kiểm tra role provider, v.v.
            var response = await _bookingService.ConfirmBookingAsync(bookingId);
            if (response.Success) return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Provider bắt đầu khảo sát: Confirmed -> Surveying.
        /// </summary>
        [HttpPut("survey/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> StartSurvey(int bookingId)
        {
            var response = await _bookingService.StartSurveyAsync(bookingId);
            if (response.Success) return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Sau khảo sát, customer chốt OK và thanh toán đặt cọc (Deposit): Surveying -> Procuring.
        /// </summary>
        [HttpPost("deposit/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> ApproveSurveyAndDeposit(int bookingId, [FromBody] PaymentRequest paymentRequest)
        {
            // Map PaymentRequest -> Payment entity
            var depositPayment = new Payment
            {
                Code = paymentRequest.Code,
                Total = paymentRequest.Total
            };

            var response = await _bookingService.ApproveSurveyAndDepositAsync(bookingId, depositPayment);
            if (response.Success) return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Khi bắt đầu thi công: Procuring -> Progressing.
        /// </summary>
        [HttpPut("progress/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> StartProgress(int bookingId)
        {
            var response = await _bookingService.StartProgressAsync(bookingId);
            if (response.Success) return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Khi thi công xong, customer thanh toán cuối (FinalPayment): Progressing -> Completed.
        /// </summary>
        [HttpPost("complete/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> CompleteBooking(int bookingId, [FromBody] PaymentRequest paymentRequest)
        {
            var finalPayment = new Payment
            {
                Code = paymentRequest.Code,
                Total = paymentRequest.Total
            };

            var response = await _bookingService.CompleteBookingAsync(bookingId, finalPayment);
            if (response.Success) return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Hủy booking: bất kỳ trạng thái chưa Completed -> Cancelled.
        /// </summary>
        [HttpPut("cancel/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var response = await _bookingService.CancelBookingAsync(bookingId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }
    }
}
