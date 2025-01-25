using DataAccessObject.Models;
using Repository.GenericRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IChatRepository : IGenericRepository<Chat>
    {
        Task<IEnumerable<Chat>> GetChatHistoryAsync(int senderId, int receiverId);
        Task<IEnumerable<Chat>> GetUnreadMessagesAsync(int receiverId, int senderId);
    }
}
