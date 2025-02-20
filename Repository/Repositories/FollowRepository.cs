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
    public class FollowRepository : GenericRepository<Follow>, IFollowRepository
    {
        public FollowRepository(HomeDecorDBContext context) : base(context) { }

        public async Task<IEnumerable<Follow>> GetFollowsByFollowingIdAsync(int followingId)
        {
            return await _context.Set<Follow>()
                .Include(f => f.Follower)
                .Where(f => f.FollowingId == followingId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Follow>> GetFollowsByFollowerIdAsync(int followerId)
        {
            return await _context.Set<Follow>()
                .Include(f => f.Following)
                .Where(f => f.FollowerId == followerId)
                .ToListAsync();
        }

        public async Task<Follow> GetFollowAsync(int followerId, int followingId)
        {
            return await _context.Set<Follow>()
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
        }
    }
}
