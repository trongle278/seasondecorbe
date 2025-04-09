using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.Interfaces
{
    public interface IQuotationService
    {
        Task<BaseResponse> CreateQuotationAsync(string bookingCode, CreateQuotationRequest request);
        Task<BaseResponse> UploadQuotationFileAsync(string bookingCode, IFormFile quotationFile);
        //Task<BaseResponse> GetQuotationByBookingCodeAsync(string bookingCode);
        Task<BaseResponse<PageResult<QuotationResponseForCustomer>>> GetPaginatedQuotationsForCustomerAsync(QuotationFilterRequest request, int accountId);
        Task<BaseResponse<PageResult<QuotationResponseForProvider>>> GetPaginatedQuotationsForProviderAsync(QuotationFilterRequest request, int providerId);
        Task<BaseResponse<QuotationDetailResponseForCustomer>> GetQuotationDetailByCustomerAsync(string quotationCode, int customerId);
        Task<BaseResponse> ConfirmQuotationAsync(string quotationCode, bool isConfirmed);
    }
}
