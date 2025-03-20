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

        // Inject PayOS đã đăng ký ở Program/Startup
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
        public async Task<IActionResult> Return([FromQuery] VnPayResponse response, [FromQuery] int customerId, [FromQuery] int adminId, [FromQuery] int proverId)
        {
            if (response.vnp_TransactionStatus == "00")
            {
                if (customerId != 0)
                {
                    await _paymentService.TopUp(customerId, response.vnp_Amount);
                }
                // Xử lý đơn hàng (Cập nhật trạng thái đơn hàng thành "Đã thanh toán")
                var htmlResponse = @"
    <html>
        <head>
            <script type='text/javascript'>
                window.open('https://example.com/confirmation', '_blank');              
            </script>
        </head>
        <body>
            <p>Thanh toán thành công  click <a href='http://localhost:3000/product' target='_blank'>here</a>.</p>
        </body>
    </html>
";
                return Content(htmlResponse, "text/html");
            }

            return Ok();
        }
    }
}
