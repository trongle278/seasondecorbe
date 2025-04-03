using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.Interfaces
{
    public interface IQuotationService
    {
        Task<BaseResponse> CreateQuotationAsync(string bookingCode, CreateQuotationRequest request);
        Task<BaseResponse> UploadQuotationFileAsync(string bookingCode, IFormFile quotationFile);
        Task<BaseResponse> GetQuotationByBookingCodeAsync(string bookingCode);
    }
}
