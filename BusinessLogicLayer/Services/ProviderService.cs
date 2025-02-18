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

        public async Task<BaseResponse> SendProviderInvitationEmailAsync(string email)
        {
            try
            {
                const string subject = "Welcome to Seasonal Home Decor - Become a Provider!";
                string registrationLink = "https://example.com/become-decorator"; // FE link

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
                provider.Account.Address = request.Address;

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
                provider.Avatar = request.Avatar;
                provider.Account.Phone = request.Phone;
                provider.Account.Address = request.Address;

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

        //Dùng để Đổi trang Customer <=> Provider
        public async Task<BaseResponse> ChangeProviderStatusByAccountIdAsync(int accountId, bool isProvider)
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

                provider.IsProvider = isProvider;

                _unitOfWork.ProviderRepository.Update(provider);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Provider status updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "Failed to update provider status", ex.Message }
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
