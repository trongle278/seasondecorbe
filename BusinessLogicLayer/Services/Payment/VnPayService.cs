using BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Http;
using Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCP.Service.Service.Pay
{
    public class VNPayService
    {
        private readonly IUnitOfWork _unitOfWork;
        public VNPayService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public static string VNPay(HttpContext context, VnPayRequest vnPayRequest)
        {
            //string vnp_Returnurl = $"http://localhost:5297/api/Payment/return?customerId={vnPayRequest.CustomerId}"; //URL nhan ket qua tra ve
            string vnp_Returnurl = $"https://season-decor.somee.com/api/Payment/return?customerId={vnPayRequest.CustomerId}"; //URL nhan ket qua tra ve
            string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"; //URL thanh toan cua VNPAY 
            string vnp_TmnCode = "ULVE3NUK"; //Ma định danh merchant kết nối (Terminal Id)
            string vnp_HashSecret = "REFWL616A23MJOFK118BV7GA6FBS0609"; //Secret Key

            TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

            //Get payment input
            var OrderId = DateTime.Now.Ticks; // Giả lập mã giao dịch hệ thống merchant gửi sang VNPAY
            var Amount = 100000; // Giả lập số tiền thanh toán hệ thống merchant gửi sang VNPAY 100,000 VND
            var Status = "0"; //0: Trạng thái thanh toán "chờ thanh toán" hoặc "Pending" khởi tạo giao dịch chưa có IPN
            var CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            //Save order to db

            //Build URL for VNPAY
            VnPayLib vnpay = new VnPayLib();

            vnpay.AddRequestData("vnp_Version", VnPayLib.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", (vnPayRequest.Amount * 100).ToString()); //Số tiền thanh toán. Số tiền không mang các ký tự phân tách thập phân, phần nghìn, ký tự tiền tệ. Để gửi số tiền thanh toán là 100,000 VND (một trăm nghìn VNĐ) thì merchant cần nhân thêm 100 lần (khử phần thập phân), sau đó gửi sang VNPAY là: 10000000
            vnpay.AddRequestData("vnp_BankCode", "VNBANK");


            vnpay.AddRequestData("vnp_CreateDate", CreatedDate.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));


            vnpay.AddRequestData("vnp_Locale", "vn");

            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other"); //default value: other

            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", OrderId.ToString()); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày

            //Add Params of 2.1.0 Version
            //Billing

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            return paymentUrl;
        }
    }
}