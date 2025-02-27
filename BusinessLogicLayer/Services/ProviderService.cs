using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class ProviderService : IProviderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ICloudinaryService _cloudinaryService; // Use CloudinaryService

        public ProviderService(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _emailService = emailService;
            _cloudinaryService = cloudinaryService;
        }
        public async Task<BaseResponse> GetProviderProfileByAccountIdAsync(int accountId)
        {
            var response = new BaseResponse();
            try
            {
                // Lấy provider kèm thông tin Account
                var provider = await _unitOfWork.ProviderRepository
                    .Query(p => p.AccountId == accountId)
                    .Include(p => p.Account)
                    .FirstOrDefaultAsync();

                if (provider == null)
                {
                    response.Success = false;
                    response.Message = "Provider not found for the given account";
                    return response;
                }

                // Tính số người theo dõi (followers) và đang theo dõi (followings)
                int followersCount = await _unitOfWork.FollowRepository
                    .Query(f => f.FollowingId == accountId)
                    .CountAsync();

                int followingsCount = await _unitOfWork.FollowRepository
                    .Query(f => f.FollowerId == accountId)
                    .CountAsync();

                // Ánh xạ từ Provider sang ProviderResponse (đã cấu hình trong AutoMapper)
                var providerData = _mapper.Map<ProviderResponse>(provider);
                providerData.FollowersCount = followersCount;
                providerData.FollowingsCount = followingsCount;

                response.Success = true;
                response.Message = "Provider profile retrieved successfully";
                response.Data = providerData;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to get provider profile";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> SendProviderInvitationEmailAsync(string email)
        {
            try
            {
                const string subject = "Welcome to Seasonal Home Decor - Become a Provider!";
                string registrationLink = "http://localhost:3000/seller/registration"; // FE link

                // Sử dụng đường dẫn tương đối
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "DecoratorInvitationTemplate.html");
                string body = File.ReadAllText(templatePath);

                // Thay thế các placeholder
                body = body.Replace("{registrationLink}", registrationLink)
                           .Replace("{email}", email);

                await _emailService.SendEmailAsync(email, subject, body);

                return new BaseResponse
                {
                    Success = true,
                    Message = "Invitation email sent successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "Failed to send invitation email", ex.Message }
                };
            }
        }

        public async Task<BaseResponse> CreateProviderProfileAsync(int accountId, BecomeProviderRequest request)
        {
            try
            {
                // Retrieve the existing account
                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Id == accountId)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Account not found" }
                    };
                }

                var existingProfile = await _unitOfWork.ProviderRepository
                    .Query(p => p.AccountId == accountId)
                    .FirstOrDefaultAsync();

                if (existingProfile != null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "You already have a provider profile" }
                    };
                }

                var provider = _mapper.Map<Provider>(request);
                provider.AccountId = accountId;

                // Use the existing account
                provider.Account = account;
                provider.Account.Phone = request.Phone;

                await _unitOfWork.ProviderRepository.InsertAsync(provider);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Your provider profile has been created successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "Failed to create provider profile", ex.Message }
                };
            }
        }

        public async Task<BaseResponse> UpdateProviderProfileByAccountIdAsync(int accountId, UpdateProviderRequest request)
        {
            try
            {
                var provider = await _unitOfWork.ProviderRepository
                    .Query(p => p.AccountId == accountId)
                    .Include(p => p.Account) // Include related account information
                    .FirstOrDefaultAsync();

                if (provider == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Provider not found for the given account" }
                    };
                }

                // Update provider details
                provider.Name = request.Name;
                provider.Bio = request.Bio;
                provider.Account.Phone = request.Phone;
                provider.Address = request.Address;

                _unitOfWork.ProviderRepository.Update(provider);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Provider profile updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "Failed to update provider profile", ex.Message }
                };
            }
        }

        // Switch between Customer <--> Provider
        public async Task<BaseResponse> ChangeProviderStatusByAccountIdAsync(int accountId, bool isProvider)
        {
            try
            {
                // Tìm Provider tương ứng với accountId
                var provider = await _unitOfWork.ProviderRepository
                    .Query(p => p.AccountId == accountId)
                    .FirstOrDefaultAsync();

                // Nếu chưa có Provider record => chưa đăng ký
                if (isProvider && provider == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string>
                {
                    "Please apply your provider registration first!"
                }
                    };
                }

                // Nếu isProvider = true và provider != null => Chuyển sang provider
                // Nếu isProvider = false => Chuyển về customer (dù có provider hay chưa)
                //   - Trường hợp provider == null mà isProvider = false => có thể coi như 
                //     user vốn đã là customer, nên return success hoặc tuỳ ý
                if (provider == null)
                {
                    // Vẫn thành công do user vốn là customer
                    return new BaseResponse
                    {
                        Success = true,
                        Message = "You remain in customer mode."
                    };
                }

                // Tới đây là provider != null
                provider.IsProvider = isProvider;

                _unitOfWork.ProviderRepository.Update(provider);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = isProvider
                        ? "Welcome to the Provider Dashboard!"
                        : "Welcome back to Customer"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Errors = new List<string>
            {
                "An error occurred while changing your provider status.",
                ex.Message
            }
                };
            }
        }

        public async Task<BaseResponse> UploadProviderAvatarAsync(int accountId, Stream fileStream, string fileName)
        {
            try
            {
                var provider = await _unitOfWork.ProviderRepository
                    .Query(p => p.AccountId == accountId)
                    .FirstOrDefaultAsync();

                if (provider == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Provider not found for the given account" }
                    };
                }

                var avatarUrl = await _cloudinaryService.UploadAvatarAsync(fileStream, fileName);
                provider.Avatar = avatarUrl;

                _unitOfWork.ProviderRepository.Update(provider);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Avatar uploaded successfully",
                    Data = avatarUrl
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "Failed to upload avatar", ex.Message }
                };
            }
        }
    }
}
