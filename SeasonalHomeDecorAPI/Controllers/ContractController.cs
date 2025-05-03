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

        [HttpGet("getContractFile/{quotationCode}")]
        public async Task<IActionResult> GetContractFile([FromRoute] string quotationCode)
        {
            var response = await _contractService.GetContractFileAsync(quotationCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }      
    }
}
