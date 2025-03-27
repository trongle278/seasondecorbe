using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IQuotationService
    {
        Task<BaseResponse> CreateQuotationAsync(CreateQuotationRequest request);
        Task<BaseResponse<QuotationDetailResponse>> GetQuotationDetailAsync(int bookingId);
    }
}
