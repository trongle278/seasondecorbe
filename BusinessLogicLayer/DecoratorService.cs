using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Common.Enums;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;
using System.IO;

namespace BusinessLogicLayer
{
    public class DecoratorService : IDecoratorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;

        public DecoratorService(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _emailService = emailService;
        }

        public async Task<BaseResponse> SendDecoratorInvitationEmailAsync(string email)
        {
            try
            {
                const string subject = "Welcome to Seasonal Home Decor - Become a Decorator!";
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

        public async Task<BaseResponse> CreateDecoratorProfileAsync(int accountId, BecomeDecoratorRequest request)
        {
            try
            {
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
                if (account == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Account not found" }
                    };
                }

                var existingProfile = await _unitOfWork.DecoratorRepository
                    .Query(d => d.AccountId == accountId)
                    .FirstOrDefaultAsync();

                if (existingProfile != null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "You already have a decorator profile" }
                    };
                }

                var decorator = _mapper.Map<Decorator>(request);
                decorator.AccountId = accountId;

                await _unitOfWork.DecoratorRepository.InsertAsync(decorator);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Your decorator profile has been created and is pending approval"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "Failed to create decorator profile", ex.Message }
                };
            }
        }

        public async Task<BaseResponse> UpdateDecoratorStatusAsync(int accountId, DecoratorApplicationStatus newStatus)
        {
            try
            {
                var decorator = await _unitOfWork.DecoratorRepository
                                                 .Query(d => d.AccountId == accountId)
                                                 .FirstOrDefaultAsync();
                if (decorator == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Decorator not found" }
                    };
                }

                decorator.Status = newStatus;
                await _unitOfWork.CommitAsync();

                string statusMessage = newStatus switch
                {
                    DecoratorApplicationStatus.Approved => "Your application has been approved",
                    DecoratorApplicationStatus.Rejected => "Your application has been rejected",
                    _ => "Your application status has been updated"
                };

                return new BaseResponse
                {
                    Success = true,
                    Message = statusMessage
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "Failed to update decorator status", ex.Message }
                };
            }
        }

        public async Task<DecoratorResponse> GetDecoratorProfileAsync(int accountId)
        {
            try
            {
                var decorator = await _unitOfWork.DecoratorRepository
                    .Query(d => d.AccountId == accountId)
                    .Include(d => d.Account)
                    .FirstOrDefaultAsync();

                if (decorator == null)
                {
                    return new DecoratorResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Decorator profile not found" }
                    };
                }

                var response = _mapper.Map<DecoratorResponse>(decorator);
                response.Success = true;
                response.Message = "Get decorator profile successfully";

                return response;
            }
            catch (Exception ex)
            {
                return new DecoratorResponse
                {
                    Success = false,
                    Errors = new List<string> { "Failed to get decorator profile", ex.Message }
                };
            }
        }
    }
}
