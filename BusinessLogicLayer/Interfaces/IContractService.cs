using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;

namespace BusinessLogicLayer.Interfaces
{
    public interface IContractService
    {
        Task<BaseResponse<string>> GetContractContentAsync(string contractCode);
        Task<BaseResponse<ContractResponse>> CreateContractByQuotationCodeAsync(string quotationCode, ContractRequest request);
        Task<BaseResponse<string>> RequestSignatureAsync(string contractCode);
        Task<BaseResponse<string>> VerifyContractSignatureAsync(string signatureToken);
    }
}
