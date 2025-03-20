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
        private readonly IPaymentService _paymentService;

        public BookingController(IBookingService bookingService, IPaymentService paymentService)
        {
            _bookingService = bookingService;
            _paymentService = paymentService;
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
        /// [Customer] Confirm booking và lưu số tiền đặt cọc
        /// </summary>
        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> ConfirmBooking(int id, [FromBody] ConfirmBookingRequest request)
        {
            try
            {
                var response = await _bookingService.ConfirmBookingAsync(id, request.DepositAmount);
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
        /// [Customer] Thanh toán đặt cọc
        /// </summary>
        [HttpPost("{id}/deposit")]
        public async Task<IActionResult> DepositPayment(int id)
        {
            try
            {
                // Lấy ID khách hàng hiện tại
                var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (accountId == 0)
                {
                    return Unauthorized(new { Message = "Unauthorized" });
                }

                // Lấy thông tin booking
                var bookingResponse = await _bookingService.GetBookingDetailsAsync(id, accountId);
                if (!bookingResponse.Success)
                {
                    return BadRequest(bookingResponse);
                }

                // Giả sử booking có trường DepositAmount để biết số tiền cần đặt cọc
                var booking = (dynamic)bookingResponse.Data;
                decimal depositAmount = booking.DepositAmount;

                // Thực hiện thanh toán đặt cọc
                // Giả sử adminId = 1, đây là tài khoản admin nhận tiền đặt cọc
                var paymentSuccess = await _paymentService.Deposit(accountId, 1, depositAmount, id);

                if (paymentSuccess)
                {
                    // Nếu thanh toán thành công, cập nhật trạng thái booking
                    var updateResponse = await _bookingService.MarkDepositPaidAsync(id);
                    if (updateResponse.Success)
                    {
                        return Ok(new BaseResponse
                        {
                            Success = true,
                            Message = "Deposit payment successful, booking updated to DepositPaid.",
                            Data = updateResponse.Data
                        });
                    }
                    return Ok(new BaseResponse
                    {
                        Success = true,
                        Message = "Deposit payment successful, but failed to update booking status.",
                        Data = booking
                    });
                }

                return BadRequest(new BaseResponse
                {
                    Success = false,
                    Message = "Deposit payment failed."
                });
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
        /// [Provider] Chuyển booking sang trạng thái ConstructionPayment
        /// </summary>
        [HttpPut("{id}/construction-payment")]
        public async Task<IActionResult> MarkConstructionPayment(int id)
        {
            try
            {
                var response = await _bookingService.MarkConstructionPaymentAsync(id);
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
        /// [Customer] Thanh toán cuối cùng (phần còn lại) và hoàn thành booking
        /// </summary>
        [HttpPost("{id}/final-payment")]
        public async Task<IActionResult> FinalPayment(int id)
        {
            try
            {
                // Lấy ID khách hàng hiện tại
                var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (accountId == 0)
                {
                    return Unauthorized(new { Message = "Unauthorized" });
                }

                // Lấy thông tin booking
                var bookingResponse = await _bookingService.GetBookingDetailsAsync(id, accountId);
                if (!bookingResponse.Success)
                {
                    return BadRequest(bookingResponse);
                }

                // Lấy thông tin booking và tính toán số tiền cần thanh toán
                var booking = (dynamic)bookingResponse.Data;
                double totalPrice = booking.TotalPrice;

                // Lấy thông tin provider của dịch vụ
                int providerId = booking.ProviderId;

                // Thực hiện thanh toán cuối cùng
                var paymentSuccess = await _paymentService.Pay(accountId, (decimal)totalPrice, providerId, id);

                if (paymentSuccess)
                {
                    // Nếu thanh toán thành công, hoàn thành booking
                    var updateResponse = await _bookingService.CompleteBookingAsync(id);
                    if (updateResponse.Success)
                    {
                        return Ok(new BaseResponse
                        {
                            Success = true,
                            Message = "Final payment successful, booking completed.",
                            Data = updateResponse.Data
                        });
                    }
                    return Ok(new BaseResponse
                    {
                        Success = true,
                        Message = "Final payment successful, but failed to update booking status.",
                        Data = booking
                    });
                }

                return BadRequest(new BaseResponse
                {
                    Success = false,
                    Message = "Final payment failed."
                });
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
