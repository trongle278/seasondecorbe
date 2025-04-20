using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Interfaces;
using KCP.Service.Service.Pay;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Payment;
using static DataAccessObject.Models.PaymentTransaction;

namespace SeasonalHomeDecorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly IPaymentService _paymentService;

        public PaymentController(IWalletService walletService, IPaymentService paymentService)
        {
            _walletService = walletService;
            _paymentService = paymentService;
        }

        [HttpPost("top-up")]
        public IActionResult TopUp([FromBody] VnPayRequest request)
        {
            var vnPayResult = VNPayService.VNPay(HttpContext, request);
            var response = new BaseResponse
            {
                Success = true,
                Message = "Payment URL generated successfully",
                Data = vnPayResult
            };
            return Ok(response);
        }

        // Mobile application payment endpoint
        [HttpPost("top-up-mobile")]
        public IActionResult TopUpMobile([FromBody] VnPayRequest request)
        {
            // Set transaction type and status if not already set
            if (request.TransactionType == 0)
            {
                request.TransactionType = EnumTransactionType.TopUp;
            }

            if (request.TransactionStatus == 0)
            {
                request.TransactionStatus = EnumTransactionStatus.Pending;
            }

            // Generate the payment URL for mobile
            var vnPayResult = VNPayService.VNPay(HttpContext, request, true);

            var response = new BaseResponse
            {
                Success = true,
                Message = "Mobile payment URL generated successfully",
                Data = new
                {
                    PaymentUrl = vnPayResult,
                    CustomerId = request.CustomerId
                }
            };

            return Ok(response);
        }

        [HttpGet("return")]
        public async Task<IActionResult> Return([FromQuery] VnPayResponse response, [FromQuery] int customerId)
        {
            if (response.vnp_TransactionStatus == "00")
            {
                if (customerId != 0)
                {
                    await _paymentService.TopUp(customerId, response.vnp_Amount);
                }
                // Xử lý đơn hàng (Cập nhật trạng thái đơn hàng thành "Đã thanh toán"
                return Redirect("http://localhost:3000/payment/success");
            }
            return Redirect("http://localhost:3000/payment/failure");//trang thất bại
        }

        [HttpGet("mobileReturn")]
        public async Task<IActionResult> MobileReturn([FromQuery] VnPayResponse response, [FromQuery] int customerId)
        {
            if (response.vnp_TransactionStatus == "00")
            {
                if (customerId != 0)
                {
                    await _paymentService.TopUp(customerId, response.vnp_Amount);
                }
                // Xử lý đơn hàng (Cập nhật trạng thái đơn hàng thành "Đã thanh toán"
                return Redirect("com.baymaxphan.seasondecormobileapp:/screens/payment/success");
            }
            return Redirect("com.baymaxphan.seasondecormobileapp:/screens/payment/failure");//trang thất bại
        }

        /// <summary>
        /// Lấy thông tin thanh toán đặt cọc.
        /// </summary>
        [HttpGet("getDepositPayment/{quotationCode}")]
        public async Task<IActionResult> GetDepositPayment(string quotationCode)
        {
            var result = await _paymentService.GetDepositPaymentAsync(quotationCode);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin thanh toán số tiền còn lại.
        /// </summary>
        [HttpGet("getFinalPaymentAsync/{quotationCode}")]
        public async Task<IActionResult> GetFinalPayment(string quotationCode)
        {
            var result = await _paymentService.GetFinalPaymentAsync(quotationCode);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
