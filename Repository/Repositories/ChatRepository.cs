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
            return await GetAllAsync(
                limit: 1000,
                filter: c => (c.SenderId == senderId && c.ReceiverId == receiverId) ||
                             (c.SenderId == receiverId && c.ReceiverId == senderId),
                orderBy: q => q.OrderBy(c => c.SentTime),
                includeProperties: new Expression<Func<Chat, object>>[] { c => c.Sender, c => c.Receiver, c => c.ChatFiles }
            );
        }

        public async Task<IEnumerable<Chat>> GetUnreadMessagesAsync(int receiverId)
        {
            return await GetAllAsync(
                limit: int.MaxValue,
                filter: c => c.ReceiverId == receiverId && c.IsRead == false,
                orderBy: q => q.OrderBy(c => c.SentTime),
                includeProperties: new Expression<Func<Chat, object>>[] { c => c.Sender, c => c.ChatFiles }
            );
        }

        public async Task MarkMessagesAsReadAsync(int receiverId, int senderId)
        {
            var unreadMessages = await _context.Chats
                .Where(c => c.ReceiverId == receiverId && c.SenderId == senderId && !c.IsRead)
                .ToListAsync();

            if (!unreadMessages.Any()) return;

            unreadMessages.ForEach(m => m.IsRead = true);

            _context.Chats.UpdateRange(unreadMessages); // Dùng UpdateRange thay vì cập nhật từng cái một
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Chat>> GetUserChatAsync(int userId)
        {
            return await GetAllAsync(
                limit: int.MaxValue,
                filter: c => c.SenderId == userId || c.ReceiverId == userId,
                orderBy: q => q.OrderByDescending(x => x.SentTime),
                includeProperties: new Expression<Func<Chat, object>>[]
                {
            c => c.Sender,
            c => c.Receiver
                }
            );
        }
    }
}
