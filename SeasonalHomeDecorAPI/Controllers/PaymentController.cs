using BusinessLogicLayer.POS;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using Net.payOS;
using BusinessLogicLayer.Interfaces;

namespace SeasonalHomeDecorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PayOS _payOS;

        // Inject PayOS đã đăng ký ở Program/Startup
        public PaymentController(PayOS payOS)
        {
            _payOS = payOS;
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
    }
}
