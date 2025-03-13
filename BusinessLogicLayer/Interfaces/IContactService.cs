using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IContactService
    {
        Task<BaseResponse> GetAllContactsAsync(int userId);
        Task AddToContactListAsync(int userId, int contactId);
    }
}
