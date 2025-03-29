using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface ISupportService
    {
        Task<BaseResponse<SupportReplyResponse>> AddReplyAsync(AddSupportReplyRequest request, int accountId, bool isAdmin);
        Task<BaseResponse<SupportResponse>> CreateTicketAsync(CreateSupportRequest request, int accountId);
        Task<BaseResponse<SupportResponse>> GetTicketByIdAsync(int id);
        Task<BaseResponse<List<SupportResponse>>> GetAllTicketsAsync(int? bookingId, int? accountId);
    }
}
