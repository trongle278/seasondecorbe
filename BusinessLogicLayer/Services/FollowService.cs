using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Repository.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using BusinessLogicLayer.Interfaces;
using AutoMapper;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelRequest;

namespace BusinessLogicLayer.Services
{
    public class FollowService : IFollowService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public FollowService(IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<BaseResponse> FollowAsync(int followerId, int followingId, bool isNotify = true)
        {
            var response = new BaseResponse();
            try
            {
                if (followerId == followingId)
                    throw new Exception("Cannot follow yourself.");

                var existingFollow = await _unitOfWork.FollowRepository.GetFollowAsync(followerId, followingId);
                if (existingFollow != null)
                    throw new Exception("You are already following this user.");

                var follow = new Follow
                {
                    FollowerId = followerId,
                    FollowingId = followingId,
                    IsNotify = isNotify,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.FollowRepository.InsertAsync(follow);
                await _unitOfWork.CommitAsync();

                // Sau khi follow thành công, tạo notification gửi tới người được follow
                var notification = new Notification
                {
                    Title = "New Follower",
                    // Nội dung thông báo có thể được cải tiến để hiển thị tên của người follow.
                    Content = "You have a new follower.",
                    AccountId = followingId,   // Người nhận thông báo là người được follow
                    NotifiedAt = DateTime.UtcNow,
                    // Giả sử Notification.NotificationType được định nghĩa sẵn trong entity Notification.
                    Type = Notification.NotificationType.System,
                    SenderId = followerId      // Người gửi thông báo là người follow
                };

                // Gửi thông báo qua NotificationService
                var notifResponse = await _notificationService.SendNotificationAsync(notification);

                response.Success = true;
                response.Message = "Follow successful. Notification sent.";
                response.Data = follow; // Hoặc bạn có thể map sang DTO nếu cần
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error occurred while following.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> UnfollowAsync(int followerId, int followingId)
        {
            var response = new BaseResponse();
            try
            {
                var follow = await _unitOfWork.FollowRepository.GetFollowAsync(followerId, followingId);
                if (follow == null)
                    throw new Exception("Follow relationship does not exist.");

                _unitOfWork.FollowRepository.Delete(follow.Id);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Unfollow successful.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error occurred while unfollowing.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> GetFollowersAsync(int userId)
        {
            var response = new BaseResponse();
            try
            {
                var followers = await _unitOfWork.FollowRepository.GetFollowsByFollowingIdAsync(userId);
                response.Success = true;
                response.Message = "Successfully retrieved followers.";
                response.Data = followers;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error occurred while retrieving followers.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> GetFollowingsAsync(int userId)
        {
            var response = new BaseResponse();
            try
            {
                var followings = await _unitOfWork.FollowRepository.GetFollowsByFollowerIdAsync(userId);
                response.Success = true;
                response.Message = "Successfully retrieved followings.";
                response.Data = followings;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error occurred while retrieving followings.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> GetFollowCountsAsync(int userId)
        {
            var response = new BaseResponse();
            try
            {
                var followersCount = await _unitOfWork.FollowRepository.Query(f => f.FollowingId == userId).CountAsync();
                var followingsCount = await _unitOfWork.FollowRepository.Query(f => f.FollowerId == userId).CountAsync();
                response.Success = true;
                response.Message = "Successfully retrieved follow counts.";
                response.Data = new { followersCount, followingsCount };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error occurred while retrieving follow counts.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }
    }
}
