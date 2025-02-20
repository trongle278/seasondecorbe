using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Repository.GenericRepository;

namespace Repository.Interfaces
{
    public interface IFollowRepository : IGenericRepository<Follow>
    {
        Task<IEnumerable<Follow>> GetFollowsByFollowingIdAsync(int followingId);
        Task<IEnumerable<Follow>> GetFollowsByFollowerIdAsync(int followerId);
        Task<Follow> GetFollowAsync(int followerId, int followingId);
    }
}
