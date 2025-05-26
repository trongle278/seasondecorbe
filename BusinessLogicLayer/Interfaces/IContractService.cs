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

        Task<BaseResponse<ContractCancelDetailResponse>> GetRequestContractCancelDetailAsync(string contractCode);
        Task<BaseResponse> RequestCancelContractAsync(string contractCode, int cancelReasonId, string cancelReason);
        Task<BaseResponse> ApproveCancelContractAsync(string contractCode);

        Task<BaseResponse<string>> RequestTerminateOtpAsync(string contractCode);
        Task<BaseResponse> TerminateContract(string contractCode, string otp);

        Task<BaseResponse> TriggerAllContractTerminatableAsync();
        Task<BaseResponse> SetTerminatableByContractCodeAsync(string contractCode);
    }
}
