using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Interfaces;
using KCP.Service.Service.Pay;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Payment;

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
    }
}
