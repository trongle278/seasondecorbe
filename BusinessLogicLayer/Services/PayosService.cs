using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using Microsoft.Extensions.Options;
using Net.payOS;         // Namespace của SDK PayOS
using Net.payOS.Types;

namespace BusinessLogicLayer.Services
{
    public class PayosService : IPayosService
    {
        private readonly PayosSettings _settings;

        public PayosService(IOptions<PayosSettings> options)
        {
            _settings = options.Value;
        }

        /// <summary>
        /// Tạo link thanh toán PayOS cho đơn hàng.
        /// Tham số:
        /// - orderCode: kiểu long (theo SDK yêu cầu)
        /// - amount: kiểu int (đơn vị nhỏ nhất, ví dụ: nếu amount=100 thì tương đương 100 VND)
        /// - description: mô tả đơn hàng
        /// - items: danh sách item (nếu có)
        /// </summary>
        public async Task<CreatePaymentResult> CreatePaymentLinkAsync(long orderCode, int amount, string description, List<ItemData> items = null)
        {
            // Khởi tạo PayOS client với thông tin cấu hình
            var payOS = new PayOS(_settings.ClientId, _settings.ApiKey, _settings.ChecksumKey);

            // Tạo PaymentData theo yêu cầu của SDK
            var paymentData = new PaymentData(
                orderCode,
                amount,
                description,
                items ?? new List<ItemData>(),
                _settings.ReturnUrl,
                _settings.CancelUrl
            );

            // Gọi tạo link thanh toán
            CreatePaymentResult createPaymentResult = await payOS.createPaymentLink(paymentData);
            return createPaymentResult;
        }

        /// <summary>
        /// Xác nhận webhook (nếu cần thiết).
        /// </summary>
        public async Task<string> ConfirmWebhookAsync()
        {
            var payOS = new PayOS(_settings.ClientId, _settings.ApiKey, _settings.ChecksumKey);
            return await payOS.confirmWebhook(_settings.WebhookUrl);
        }

        /// <summary>
        /// Xác minh dữ liệu webhook mà PayOS gửi về.
        /// </summary>
        public WebhookData VerifyWebhookData(WebhookType webhookData)
        {
            var payOS = new PayOS(_settings.ClientId, _settings.ApiKey, _settings.ChecksumKey);
            return payOS.verifyPaymentWebhookData(webhookData);
        }
    }
}
