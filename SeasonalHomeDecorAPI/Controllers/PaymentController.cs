using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Interfaces;
using KCP.Service.Service.Pay;
using BusinessLogicLayer.ModelResponse;

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
            var result = VNPayService.VNPay(HttpContext, request);
            return Ok(result);
        }

        [HttpGet("return")]
        public async Task<IActionResult> Return([FromQuery] VnPayResponse response, [FromQuery] int customerId)
        {
                : "Giao dich that bai.";
            if (response.vnp_TransactionStatus == "00")
            {
                if (customerId != 0)
                {
                    await _paymentService.TopUp(customerId, response.vnp_Amount);
                }
                // Xử lý đơn hàng (Cập nhật trạng thái đơn hàng thành "Đã thanh toán"
                return Redirect("https://meet.google.com/yjx-rcgq-yfx?authuser=1");
            }
            return Redirect("https://meet.google.com/yjx-rcgq-yfx?authuser=1");//trang thất bại
        }
    }
}
