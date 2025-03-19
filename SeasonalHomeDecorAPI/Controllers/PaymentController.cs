using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using Net.payOS;
using BusinessLogicLayer.Interfaces;
using KCP.Service.Service.Pay;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.Utilities.POS;

namespace SeasonalHomeDecorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PayOS _payOS;
        private readonly IWalletService _walletService;
        private readonly IPaymentService _paymentService;

        // Inject PayOS đã đăng ký ở Program/Startup
        public PaymentController(PayOS payOS, IWalletService walletService, IPaymentService paymentService)
        {
            _payOS = payOS;
            _walletService = walletService;
            _paymentService = paymentService;
        }

        [HttpPost("create-payment-link")]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentLinkRequest request)
        {
            var domain = "http://localhost:5297";

            // Thay depositPayment.Total = request.Amount
            int finalAmount = (int)Math.Round((decimal)request.Amount);

            // items: ép Price sang int
            var itemList = request.Items.Select(x =>
                new ItemData(x.Name, x.Quantity, (int)Math.Round((decimal)x.Price))
            ).ToList();

            var paymentData = new PaymentData(
                orderCode: request.OrderCode,
                amount: finalAmount,
                description: request.Description,
                items: itemList,
                cancelUrl: $"{domain}/cancel",
                returnUrl: $"{domain}/success"
            );

            var payResponse = await _payOS.createPaymentLink(paymentData);

            // Trả về link cho client
            return Ok(new { checkoutUrl = payResponse.checkoutUrl });
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
