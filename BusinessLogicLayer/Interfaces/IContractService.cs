using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Payment;
using DataAccessObject.Models;

namespace BusinessLogicLayer.Interfaces
{
    public interface IContractService
    {
        Task<BaseResponse<string>> GetContractContentAsync(string contractCode);
        Task<BaseResponse<ContractResponse>> CreateContractByQuotationCodeAsync(string quotationCode, ContractRequest request);
        Task<BaseResponse<string>> RequestSignatureAsync(string contractCode);
        Task<BaseResponse<string>> RequestSignatureForMobileAsync(string contractCode);
        Task<BaseResponse<string>> VerifyContractSignatureAsync(string signatureToken);
        Task<BaseResponse<ContractFileResponse>> GetContractFileAsync(string quotationCode);
    }
}
