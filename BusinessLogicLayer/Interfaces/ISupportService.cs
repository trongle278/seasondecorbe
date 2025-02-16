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
        Task<SupportReplyResponse> AddReplyAsync(AddSupportReplyRequest request, bool isAdmin);
        Task<SupportResponse> CreateTicketAsync(CreateSupportRequest request);
        Task<SupportResponse> GetTicketByIdAsync(int id);
    }
}
