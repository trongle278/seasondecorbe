using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.GenericRepository;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(HomeDecorDBContext context) : base(context) { }

        public async Task<IEnumerable<Notification>> GetNotificationsByAccountIdAsync(int accountId)
        {
            return await _context.Set<Notification>()
                                 .Include(n => n.Account)
                                 .Include(n => n.Sender)
                                 .Where(n => n.AccountId == accountId)
                                 .OrderByDescending(n => n.NotifiedAt)
                                 .ToListAsync();
        }
    }
}
