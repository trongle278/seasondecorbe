using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IPayosService _payosService; // Giả lập service kết nối PayOS

        public BookingController(IBookingService bookingService, IPayosService payosService)
        {
            _bookingService = bookingService;
            _payosService = payosService;
        }

        // POST: api/Booking/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            var result = await _bookingService.CreateBookingAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // POST: api/Booking/confirm
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmBooking([FromBody] ConfirmBookingRequest request)
        {
            var result = await _bookingService.ConfirmBookingAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // PUT: api/Booking/status
        [HttpPut("status")]
        public async Task<IActionResult> UpdateBookingStatus([FromBody] UpdateBookingStatusRequest request)
        {
            var result = await _bookingService.UpdateBookingStatusAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // GET: api/Booking/{bookingId}
        [HttpGet("{bookingId}")]
        public async Task<IActionResult> GetBooking(int bookingId)
        {
            var result = await _bookingService.GetBookingAsync(bookingId);
            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // POST: api/Booking/payment
        // Giả sử endpoint này để thực hiện thanh toán qua PayOS
        [HttpPost("payment")]
        public async Task<IActionResult> MakePayment([FromBody] MakePaymentRequest request)
        {
            // 1. Gọi PayOS để thanh toán (giả lập)
            //    Tùy vào PayOS SDK / API, bạn có thể thay đổi logic
            var payResult = await _payosService.PayViaPayOSAsync(request.Amount, request.OrderId, request.AccountId);

            // Nếu thanh toán thất bại
            if (!payResult.IsSuccess)
            {
                var failResponse = new BaseResponse
                {
                    Success = false,
                    Message = "PayOS payment failed",
                    Errors = new System.Collections.Generic.List<string> { payResult.ErrorMessage }
                };
                return BadRequest(failResponse);
            }

            // 2. Nếu thành công => Ghi nhận Payment vào BookingSystem
            var response = await _bookingService.MakePaymentAsync(request);

            // 3. Trả về kết quả
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }
}
