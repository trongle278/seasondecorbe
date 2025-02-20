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
    public class DeviceTokenRepository : GenericRepository<DeviceToken>, IDeviceTokenRepository
    {
        public DeviceTokenRepository(HomeDecorDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<DeviceToken>> GetTokensByAccountIdAsync(int accountId)
        {
            return await _context.Set<DeviceToken>()
                                 .Where(dt => dt.AccountId == accountId)
                                 .ToListAsync();
        }
    }
}
