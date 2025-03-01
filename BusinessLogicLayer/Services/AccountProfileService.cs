using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class AccountProfileService : IAccountProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public AccountProfileService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<BaseResponse> UpdateSlug(int accountId, UpdateSlugRequest request)
        {
            var response = new BaseResponse();
            try
            {
                // Validate request
                if (request == null || string.IsNullOrWhiteSpace(request.Slug))
                {
                    response.Success = false;
                    response.Message = "Slug is required";
                    return response;
                }

                // Kiểm tra slug đã tồn tại ở tài khoản khác chưa
                var duplicate = await _unitOfWork.AccountRepository
                    .Query(a => a.Slug == request.Slug && a.Id != accountId)
                    .FirstOrDefaultAsync();

                if (duplicate != null)
                {
                    response.Success = false;
                    response.Message = "Slug already exists. Please choose a different one.";
                    return response;
                }

                // Lấy tài khoản theo accountId
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
                if (account == null)
                {
                    response.Success = false;
                    response.Message = "Account not found.";
                    return response;
                }

                // Cập nhật slug
                account.Slug = request.Slug;
                _unitOfWork.AccountRepository.Update(account);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Slug updated successfully";
                response.Data = account.Slug;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating slug";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> UpdateAvatarAsync(int accountId, Stream fileStream, string fileName)
        {
            try
            {
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
                if (account == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Account not found."
                    };
                }

                var url = await _cloudinaryService.UploadAvatarAsync(fileStream, fileName);
                account.Avatar = url;
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Avatar updated successfully.",
                    Data = url
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error updating avatar.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
