using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;

namespace BusinessLogicLayer.Interfaces
{
    public interface IFollowService
    {
        Task<BaseResponse> FollowAsync(int followerId, int followingId, bool isNotify = true);
        Task<BaseResponse> UnfollowAsync(int followerId, int followingId);
        Task<BaseResponse> GetFollowersAsync(int userId);
        Task<BaseResponse> GetFollowingsAsync(int userId);
        Task<BaseResponse> GetFollowCountsAsync(int userId);
    }
}
