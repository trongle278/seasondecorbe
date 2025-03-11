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

        public async Task<BaseResponse> GetAllProvidersAsync()
        {
            var response = new BaseResponse();
            try
            {
                var providers = await _unitOfWork.AccountRepository
                    .Query(a => a.IsProvider == true)
                    .ToListAsync();

                var providerList = _mapper.Map<List<ProviderResponse>>(providers);

                response.Success = true;
                response.Message = "Providers retrieved successfully.";
                response.Data = providerList;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving providers.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> GetProviderProfileByAccountIdAsync(int accountId)
        {
            var response = new BaseResponse();
            try
            {
                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Id == accountId && a.IsProvider == true)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    response.Success = false;
                    response.Message = "Provider not found for the given account";
                    return response;
                }

                int followersCount = await _unitOfWork.FollowRepository
                    .Query(f => f.FollowingId == accountId)
                    .CountAsync();

                int followingsCount = await _unitOfWork.FollowRepository
                    .Query(f => f.FollowerId == accountId)
                    .CountAsync();

                var providerData = _mapper.Map<ProviderResponse>(account);
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

        public async Task<BaseResponse> GetProviderProfileBySlugAsync(string slug)
        {
            var response = new BaseResponse();
            try
            {
                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Slug == slug && a.IsProvider == true)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    response.Success = false;
                    response.Message = "Provider not found for the given slug";
                    return response;
                }

                int followersCount = await _unitOfWork.FollowRepository
                    .Query(f => f.FollowingId == account.Id)
                    .CountAsync();

                int followingsCount = await _unitOfWork.FollowRepository
                    .Query(f => f.FollowerId == account.Id)
                    .CountAsync();

                var providerResponse = _mapper.Map<ProviderResponse>(account);
                providerResponse.FollowersCount = followersCount;
                providerResponse.FollowingsCount = followingsCount;

                response.Success = true;
                response.Message = "Provider profile retrieved successfully";
                response.Data = providerResponse;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving provider profile by slug";
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

                if (account.IsProvider == true)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "You are already registered as a provider" }
                    };
                }

                // Cập nhật tài khoản thành provider
                account.IsProvider = true;
                account.BusinessName = request.Name;
                account.Bio = request.Bio;
                account.Phone = request.Phone;
                account.BusinessAddress = request.Address;
                account.JoinedDate = DateTime.UtcNow.ToLocalTime();

                _unitOfWork.AccountRepository.Update(account);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Provider profile created successfully"
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
                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Id == accountId && a.IsProvider == true)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Provider not found for the given account" }
                    };
                }

                account.FirstName = request.Name;
                account.Bio = request.Bio;
                account.Phone = request.Phone;
                account.BusinessAddress = request.Address;

                _unitOfWork.AccountRepository.Update(account);
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
                
                if (account.IsProvider == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Please complete your provider registration first!"
                    };
                }

                account.IsProvider = isProvider;

                _unitOfWork.AccountRepository.Update(account);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = isProvider ? "Account is now a Provider!" : "Account is now a Customer!"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while changing provider status", ex.Message }
                };
            }
        }
    }
}
