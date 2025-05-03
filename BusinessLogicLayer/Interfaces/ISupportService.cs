using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;

namespace BusinessLogicLayer.Interfaces
{
    public interface ISupportService
    {
        Task<BaseResponse<SupportReplyResponse>> AddReplyAsync(AddSupportReplyRequest request, int supportId, int accountId);
        Task<BaseResponse<SupportResponse>> CreateTicketAsync(CreateSupportRequest request, int accountId);
        Task<BaseResponse<SupportResponse>> GetSupportByIdAsync(int supportId);
        Task<BaseResponse<List<SupportResponse>>> GetAllTicketsByRoleUser(int? bookingId, int? accountId);
        Task<BaseResponse<PageResult<ProviderSupportPaginateResponse>>> GetPaginatedSupportForProviderAsync(SupportFilterRequest request);
        Task<BaseResponse<PageResult<SupportResponse>>> GetPaginatedTicketsForCustomerAsync(SupportFilterRequest request, int accountId);
    }
}
