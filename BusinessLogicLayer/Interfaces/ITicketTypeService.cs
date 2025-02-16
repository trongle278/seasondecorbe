using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface ITicketTypeService
    {
        Task<BaseResponse> GetTicketTypeByIdAsync(int id);
        Task<BaseResponse> GetAllTicketTypesAsync();
        Task<BaseResponse> CreateTicketTypeAsync(TicketTypeRequest request);
        Task<BaseResponse> UpdateTicketTypeAsync(int id, TicketTypeRequest request);
        Task<BaseResponse> DeleteTicketTypeAsync(int id);
    }
}
