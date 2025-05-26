using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nest;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _contractService;

        public ContractController(IContractService contractService)
        {
            _contractService = contractService;
        }

        [HttpPost("createByQuotationCode/{quotationCode}")]
        public async Task<IActionResult> CreateContract(string quotationCode, ContractRequest request)
        {
            var response = await _contractService.CreateContractByQuotationCodeAsync(quotationCode, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getContractContent/{contractCode}")]
        public async Task<IActionResult> GetContractContent(string contractCode)
        {
            var response = await _contractService.GetContractContentAsync(contractCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("requestSignature/{contractCode}")]
        public async Task<IActionResult> RequestSignature(string contractCode)
        {
            var response = await _contractService.RequestSignatureAsync(contractCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("requestSignatureForMobile/{contractCode}")]
        public async Task<IActionResult> RequestSignatureForMobileAsync(string contractCode)
        {
            var response = await _contractService.RequestSignatureForMobileAsync(contractCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [AllowAnonymous]
        [HttpPost("verifySignature")]
        public async Task<IActionResult> VerifySignature([FromBody] string signatureToken)
        {
            var response = await _contractService.VerifyContractSignatureAsync(signatureToken);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        // Thêm endpoint này vào controller của bạn
        [AllowAnonymous]
        [HttpGet("verify-signature-mobile/{token}")]
        public IActionResult VerifySignatureMobile(string token)
        {
            // Tạo URL để mở ứng dụng trực tiếp
            string appUrl = $"com.baymaxphan.seasondecormobileapp:/signature_success?token={Uri.EscapeDataString(token)}";

            
            // Trả về HTML trang chuyển hướng
            string html = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1'>
        <title>Redirecting to SeasonDecor App</title>
        <style>
            body {{
                font-family: Arial, sans-serif;
                text-align: center;
                padding: 20px;
                background-color: #f8f9fa;
            }}
            .logo {{
                margin-bottom: 20px;
            }}
            .logo img {{
                max-width: 200px;
            }}
            h1 {{
                color: #2980b9;
                font-size: 22px;
            }}
            .loading {{
                margin: 30px auto;
                border: 5px solid #f3f3f3;
                border-top: 5px solid #5fc1f1;
                border-radius: 50%;
                width: 50px;
                height: 50px;
                animation: spin 1.5s linear infinite;
            }}
            @keyframes spin {{
                0% {{ transform: rotate(0deg); }}
                100% {{ transform: rotate(360deg); }}
            }}
            .message {{
                margin: 20px 0;
                font-size: 16px;
            }}
            .fallback {{
                margin-top: 30px;
            }}
            .fallback a {{
                display: inline-block;
                background-color: #5fc1f1;
                color: white;
                text-decoration: none;
                padding: 12px 25px;
                border-radius: 4px;
                font-weight: bold;
            }}
        </style>
    </head>
    <body>
        <div class='logo'>
            <img src='https://i.postimg.cc/RFmTf94F/seasondecorlogo.png' alt='SeasonDecor Logo'>
        </div>
        
        <h1>Redirecting to SeasonDecor App</h1>
        
        <div class='loading'></div>
        
        <p class='message'>Opening the SeasonDecor app...</p>
        
        <div class='fallback'>
            <p>If the app doesn't open automatically:</p>
        
        </div>
        
        <script>
            // Thử mở ứng dụng ngay khi trang tải
            setTimeout(function() {{
                window.location.href = '{appUrl}';
                
                // Lắng nghe sự kiện visibilitychange để kiểm tra xem ứng dụng đã mở chưa
                let visibilityChanged = false;
                document.addEventListener('visibilitychange', function() {{
                    visibilityChanged = true;
                }});
                
                // Nếu sau 2 giây mà app không mở (trang vẫn hiển thị), chuyển hướng đến trang web
                setTimeout(function() {{
                    if (!visibilityChanged && document.visibilityState !== 'hidden') {{
                        window.location.href = '';
                    }}
                }}, 2000);
            }}, 500);
        </script>
    </body>
    </html>
    ";

            return Content(html, "text/html");
        }
        [HttpGet("getContractFile/{quotationCode}")]
        public async Task<IActionResult> GetContractFile([FromRoute] string quotationCode)
        {
            var response = await _contractService.GetContractFileAsync(quotationCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getRequestContractCancelDetail/{contractCode}")]
        public async Task<IActionResult> GetRequestContractCancelDetail(string contractCode)
        {

            var response = await _contractService.GetRequestContractCancelDetailAsync(contractCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("requestCancelContract/{contractCode}")]
        public async Task<IActionResult> RequestCancelContract(string contractCode, int cancelReasonId, string cancelReason)
        {

            var response = await _contractService.RequestCancelContractAsync(contractCode, cancelReasonId, cancelReason);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("approveCancelContract/{contractCode}")]
        public async Task<IActionResult> ApproveCancelContract(string contractCode)
        {

            var response = await _contractService.ApproveCancelContractAsync(contractCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("requestTerminationOtp/{contractCode}")]
        public async Task<IActionResult> RequestTerminationOtp(string contractCode)
        {
            var result = await _contractService.RequestTerminateOtpAsync(contractCode);
            return Ok(result);
        }

        [HttpPut("terminateContract/{contractCode}")]
        public async Task<IActionResult> TerminateContract(string contractCode, string otp)
        {

            var response = await _contractService.TerminateContract(contractCode, otp);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("setTerminatableAllContract")]
        public async Task<IActionResult> SetTerminatableAllContract()
        {

            var response = await _contractService.TriggerAllContractTerminatableAsync();
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("setTerminatableByContractCode/{contractCode}")]
        public async Task<IActionResult> SetTerminatableByContractCode(string contractCode)
        {

            var response = await _contractService.SetTerminatableByContractCodeAsync(contractCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
