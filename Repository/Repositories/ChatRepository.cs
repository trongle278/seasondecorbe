using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.GenericRepository;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Repositories
{
    public class ChatRepository : GenericRepository<Chat>, IChatRepository
    {
        public ChatRepository(HomeDecorDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Chat>> GetChatHistoryAsync(int senderId, int receiverId)
        {
            Expression<Func<Chat, bool>> filter = c =>
                 c.SenderId == senderId && c.ReceiverId == receiverId ||
                 c.SenderId == receiverId && c.ReceiverId == senderId;

            // Include ChatFiles để load file đính kèm
            return await GetAllAsync(
            limit: 100,
            filter: filter,
            orderBy: q => q.OrderByDescending(x => x.SentTime),
            includeProperties: new Expression<Func<Chat, object>>[]
            {
                c => c.ChatFiles,
                c => c.Sender,
                c => c.Receiver
            });
        }

        public async Task<IEnumerable<Chat>> GetUnreadMessagesAsync(int receiverId, int senderId)
        {
            Expression<Func<Chat, bool>> filter = c =>
                c.ReceiverId == receiverId &&
                c.SenderId == senderId &&
                !c.IsRead;

            return await GetAllAsync(100, filter,  // Thêm limit parameter
                orderBy: q => q.OrderByDescending(x => x.SentTime));
        }
    }
}
