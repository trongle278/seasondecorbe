using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;
using BusinessLogicLayer.ModelResponse.Product;
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
        Task<BaseResponse<QuotationDetailResponseForProvider>> GetQuotationDetailByProviderAsync(string quotationCode, int providerId);
        Task<BaseResponse> ConfirmQuotationAsync(string quotationCode, bool isConfirmed);
        Task<BaseResponse> RequestChangeQuotationAsync(string quotationCode, string? changeReason);
        Task<BaseResponse> ApproveChangeQuotationAsync(string quotationCode);
        Task<BaseResponse> RequestCancelQuotationAsync(string quotationCode, int quotationCancelId, string cancelReason);
        Task<BaseResponse> ApproveCancelQuotationAsync(string quotationCode);
        Task<BaseResponse> AddProductToQuotationAsync(string quotationCode, int productId, int quantity);
        Task<BaseResponse> RemoveProductFromQuotationAsync(string quotationCode, int productId);
        Task<BaseResponse<RelatedProductPageResult>> GetPaginatedRelatedProductAsync(PagingRelatedProductRequest request);
        Task<BaseResponse<QuotationCancelDetailResponse>> GetQuotationCancelDetailAsync(string quotationCode);
        Task<BaseResponse<RequestQuotationChangeDetailResponse>> GetRequestQuotationChangeDetailAsync(string quotationCode);
    }
}
