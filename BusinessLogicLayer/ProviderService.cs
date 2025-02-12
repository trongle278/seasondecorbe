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

namespace BusinessLogicLayer
{
    public class ProviderService: IProviderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;

        public ProviderService(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _emailService = emailService;
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
                provider.SubscriptionId = 1; // Set a default SubscriptionId

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
    }
}
