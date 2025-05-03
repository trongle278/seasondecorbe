using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;
using CloudinaryDotNet;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Nest;
using Repository.UnitOfWork;
using static System.Net.WebRequestMethods;

namespace BusinessLogicLayer.Services
{
    public class ProviderService : IProviderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ICloudinaryService _cloudinaryService; // Use CloudinaryService
        private readonly INotificationService _notificationService;

        public ProviderService(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService, ICloudinaryService cloudinaryService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _emailService = emailService;
            _cloudinaryService = cloudinaryService;
            _notificationService = notificationService;
        }

        public async Task<BaseResponse> GetAllProvidersAsync()
        {
            var response = new BaseResponse();
            try
            {
                var providers = await _unitOfWork.AccountRepository
                    .Query(a => a.ProviderVerified == true)
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
                    .Query(a => a.Id == accountId && a.ProviderVerified == true)
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
                    .Query(a => a.Slug == slug && a.ProviderVerified == true)
                    .Include(a => a.Skill)
                    .Include(a => a.DecorationStyle)
                    .Include(a => a.CertificateImages)
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
                string body = System.IO.File.ReadAllText(templatePath);

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
                        Errors = new List<string> { "Account not found." }
                    };
                }

                if (account.ProviderVerified == true)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "You are already registered as a provider." }
                    };
                }

                if (request.CertificateImages != null && request.CertificateImages.Count > 5)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "You can upload up to 5 certificate images only." }
                    };
                }

                if (request.CertificateImages == null || request.CertificateImages.Count == 0)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "At least one certificate image is required." }
                    };
                }

                var skillExists = await _unitOfWork.SkillRepository.AnyAsync(s => s.Id == request.SkillId);
                if (!skillExists)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Skill not found." }
                    };
                }

                var styleExists = await _unitOfWork.DecorationStyleRepository.AnyAsync(s => s.Id == request.DecorationStyleId);
                if (!styleExists)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Decoration style not found." }
                    };
                }

                // Cập nhật tài khoản thành provider
                //account.IsProvider = false;
                //account.RoleId = 2;
                account.ProviderVerified = false;
                account.BusinessName = request.Name;
                account.Bio = request.Bio;
                account.Phone = request.Phone;
                account.BusinessAddress = request.Address;

                account.YearsOfExperience = request.YearsOfExperience;
                account.PastWorkPlaces = request.PastWorkPlaces;
                account.PastProjects = request.PastProjects;
                account.SkillId = request.SkillId;
                account.DecorationStyleId = request.DecorationStyleId;
                account.ApplicationCreateAt = DateTime.Now;
               
                _unitOfWork.AccountRepository.Update(account);

                // Upload ảnh chứng chỉ
                foreach (var image in request.CertificateImages)
                {
                    try
                    {
                        // Convert IFormFile to Stream
                        using (var stream = image.OpenReadStream())  // IFormFile -> Stream
                        {
                            var fileName = $"{Guid.NewGuid()}.jpg"; // Or use the original name if preferred
                            var imageUrl = await _cloudinaryService.UploadFileAsync(stream, fileName, "certificate-images");

                            var certificate = new CertificateImage
                            {
                                AccountId = account.Id,
                                ImageUrl = imageUrl,
                            };
                            await _unitOfWork.CertificateImageRepository.InsertAsync(certificate);
                        }
                    }
                    catch (Exception ex)
                    {
                        return new BaseResponse
                        {
                            Success = false,
                            Errors = new List<string> { $"Failed to upload certificate image: {ex.Message}" }
                        };
                    }
                }

                await _unitOfWork.CommitAsync();

                // Gửi thông báo cho user
                await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                {
                    AccountId = accountId,
                    Title = "Provider Application Submitted",
                    Content = "Your provider application has been submitted and waited for approve.",
                    Url = null // URL đến trang xem đơn
                });

                // Gửi thông báo cho admin
                var adminIds = await _unitOfWork.AccountRepository
                    .Query(a => a.RoleId == 1) // role 1 là admin
                    .Select(a => a.Id)
                    .ToListAsync();

                foreach (var adminId in adminIds)
                {
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = adminId,
                        Title = "New Provider Application",
                        Content = $"New provider application needs review.",
                        Url = "http://localhost:3000/admin/manage/application/"
                    });
                }

                return new BaseResponse
                {
                    Success = true,
                    Message = "Your application is waiting to approve"
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
                        Message = "Please apply your provider registration first!"
                    };
                }

                account.IsProvider = isProvider;

                _unitOfWork.AccountRepository.Update(account);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = isProvider ? "Welcome to the Provider Dashboard!"
                                         : "Welcome back to Customer"
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

        //---------------------------------------------------------------------------------------
        public async Task<BaseResponse> ApproveProviderAsync(int accountId)
        {
            var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);

            if (account == null)
                return new BaseResponse
                {
                    Success = false,
                    Message = "Account not found"
                };

            if (account.ProviderVerified == true)
                return new BaseResponse
                {
                    Success = false,
                    Message = "Account is not awaiting approval"
                };

            account.ProviderVerified = true;
            account.IsProvider = true;
            account.RoleId = 2; // Gán role Provider

            _unitOfWork.AccountRepository.Update(account);
            await _unitOfWork.CommitAsync();

            // Gửi thông báo cho provider
            await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
            {
                AccountId = accountId,
                Title = "Provider Application Approved",
                Content = "Congratulations! You now have become a provider.",
                Url = null
            });

            return new BaseResponse 
            { 
                Success = true, 
                Message = "Provider approved successfully" 
            };
        }

        public async Task<BaseResponse> RejectProviderAsync(int accountId, string reason)
        {
            var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);

            if (account == null)
                return new BaseResponse
                {
                    Success = false,
                    Message = "Account not found"
                };

            if (account.ProviderVerified == true)
                return new BaseResponse
                {
                    Success = false,
                    Message = "Account is not in a pending provider state"
                };

            // Hủy trạng thái provider và reset lại thông tin
            account.IsProvider = null;
            account.ProviderVerified = null;
            account.BusinessName = null;
            account.Bio = null;
            account.Phone = null;
            account.BusinessAddress = null;

            account.YearsOfExperience = null;
            account.PastWorkPlaces = null;
            account.PastProjects = null;
            account.SkillId = null;
            account.DecorationStyleId = null;
            account.ApplicationCreateAt = null;

            // Lấy tất cả ảnh chứng chỉ và xóa
            var certificates = await _unitOfWork.CertificateImageRepository
                .Query(c => c.AccountId == accountId)
                .ToListAsync();

            if (certificates != null && certificates.Any())
            {
                _unitOfWork.CertificateImageRepository.RemoveRange(certificates); // Xóa tất cả ảnh chứng chỉ
            }

            // Cập nhật thông tin Account
            _unitOfWork.AccountRepository.Update(account);

            // Lưu thông tin từ chối vào ApplicationHistory
            var rejection = new ApplicationHistory
            {
                AccountId = accountId,
                Reason = reason,
                RejectedAt = DateTime.Now,
            };

            await _unitOfWork.ApplicationHistoryRepository.InsertAsync(rejection);
            await _unitOfWork.CommitAsync();

            // Gửi thông báo cho provider
            await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
            {
                AccountId = accountId,
                Title = "Provider Application Rejected",
                Content = $"Your provider application was rejected.",
                Url = null
            });

            return new BaseResponse
            {
                Success = true,
                Message = $"Provider registration has been rejected and related certificates deleted."
            };
        }


        public async Task<BaseResponse<List<PendingProviderResponse>>> GetPendingProviderApplicationListAsync()
        {
            var pendingAccounts = await _unitOfWork.AccountRepository
                .Query(a => a.ProviderVerified == false)
                .Include(a => a.Skill)  // Lấy kỹ năng của provider
                .Include(a => a.DecorationStyle)  // Lấy phong cách trang trí
                .Include(a => a.CertificateImages)
                .ToListAsync();

            var response = pendingAccounts.Select(a => new PendingProviderResponse
            {
                AccountId = a.Id,
                Email = a.Email,
                FullName = $"{a.LastName} {a.FirstName}",
                Avatar = a.Avatar,
                Phone = a.Phone,
                BusinessName = a.BusinessName,
                Bio = a.Bio,
                BusinessAddress = a.BusinessAddress,
                IsProvider = a.IsProvider,
                ProviderVerified = a.ProviderVerified,

                SkillName = a.Skill?.Name,
                DecorationStyleName = a.DecorationStyle?.Name,
                YearsOfExperience = a.YearsOfExperience,
                PastWorkPlaces = a.PastWorkPlaces,
                PastProjects = a.PastProjects,
                CertificateImageUrls = a.CertificateImages?.Select(ci => ci.ImageUrl).ToList() ?? new List<string>()

            }).ToList();

            return new BaseResponse<List<PendingProviderResponse>>
            {
                Success = true,
                Message = "Pending provider applications retrieved successfully",
                Data = response
            };
        }

        public async Task<BaseResponse<PendingProviderResponse>> GetPendingProviderByIdAsync(int accountId)
        {
            var account = await _unitOfWork.AccountRepository
                .Query(a => a.Id == accountId && a.ProviderVerified == false)
                .Include(a => a.Skill)  // Lấy kỹ năng của provider
                .Include(a => a.DecorationStyle)  // Lấy phong cách trang trí
                .Include(a => a.CertificateImages)
                .FirstOrDefaultAsync();

            if (account == null)
            {
                return new BaseResponse<PendingProviderResponse>
                {
                    Success = false,
                    Message = "Pending provider not found"
                };
            }

            var response = new PendingProviderResponse
            {
                AccountId = account.Id,
                Email = account.Email,
                FullName = $"{account.LastName} {account.FirstName}",
                Avatar = account.Avatar,
                Phone = account.Phone,
                BusinessName = account.BusinessName,
                Bio = account.Bio,
                BusinessAddress = account.BusinessAddress,
                IsProvider = account.IsProvider,
                ProviderVerified = account.ProviderVerified,

                SkillName = account.Skill?.Name,
                DecorationStyleName = account.DecorationStyle?.Name,
                YearsOfExperience = account.YearsOfExperience,
                PastWorkPlaces = account.PastWorkPlaces,
                PastProjects = account.PastProjects,
                CertificateImageUrls = account.CertificateImages?.Select(ci => ci.ImageUrl).ToList() ?? new List<string>()
            };

            return new BaseResponse<PendingProviderResponse>
            {
                Success = true,
                Message = "Pending provider retrieved successfully",
                Data = response
            };
        }

        public async Task<BaseResponse<SkillsAndStylesResponse>> GetAllSkillsAndStylesAsync()
        {
            try
            {
                // Sử dụng GenericRepository thông qua UnitOfWork
                var skills = await _unitOfWork.SkillRepository.Queryable()
                    .Select(s => new SkillResponse
                    {
                        Id = s.Id,
                        Name = s.Name
                    })
                    .ToListAsync();

                var styles = await _unitOfWork.DecorationStyleRepository.Queryable()
                    .Select(ds => new DecorationStyleResponse
                    {
                        Id = ds.Id,
                        Name = ds.Name
                    })
                    .ToListAsync();

                // Tạo response object
                var response = new SkillsAndStylesResponse
                {
                    Skills = skills,
                    DecorationStyles = styles
                };

                return new BaseResponse<SkillsAndStylesResponse>
                {
                    Success = true,
                    Message = "Skills and decoration styles retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<SkillsAndStylesResponse>
                {
                    Success = false,
                    Message = "Failed to retrieve skills and decoration styles",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse<List<VerifiedProviderResponse>>> GetVerifiedProvidersApplicationListAsync()
        {
            var response = new BaseResponse<List<VerifiedProviderResponse>>();

            try
            {
                var providers = await _unitOfWork.AccountRepository
                    .Query(a => a.RoleId == 2 && a.ProviderVerified == true)
                    .Include(a => a.Skill)
                    .Include(a => a.DecorationStyle)
                    .Include(a => a.CertificateImages)
                    .ToListAsync();

                if (!providers.Any())
                {
                    response.Message = "No verified providers found";
                    return response;
                }

                var result = providers.Select(p => new VerifiedProviderResponse
                {
                    AccountId = p.Id,
                    Email = p.Email,
                    FullName = $"{p.LastName} {p.FirstName}",
                    Avatar = p.Avatar,
                    Phone = p.Phone,
                    IsProvider = p.IsProvider,
                    ProviderVerified = p.ProviderVerified,

                    BusinessName = p.BusinessName,
                    Bio = p.Bio,
                    BusinessAddress = p.BusinessAddress,
                    SkillName = p.Skill?.Name,
                    DecorationStyleName = p.DecorationStyle?.Name,
                    CertificateImageUrls = p.CertificateImages?.Select(ci => ci.ImageUrl).ToList() ?? new()
                }).ToList();

                response.Success = true;
                response.Data = result;
                response.Message = "Verified providers retrieved successfully";
            }
            catch (Exception ex)
            {
                response.Message = $"Error: {ex.Message}";
                // Log error here
            }
            return response;
        }

        public async Task<BaseResponse<VerifiedProviderResponse>> GetVerifiedProviderByIdAsync(int accountId)
        {
            var account = await _unitOfWork.AccountRepository
                .Query(a => a.Id == accountId && a.ProviderVerified == true && a.RoleId == 2)
                .Include(a => a.Skill)  // Lấy kỹ năng của provider
                .Include(a => a.DecorationStyle)  // Lấy phong cách trang trí
                .Include(a => a.CertificateImages)
                .FirstOrDefaultAsync();

            if (account == null)
            {
                return new BaseResponse<VerifiedProviderResponse>
                {
                    Success = false,
                    Message = "Verified provider not found"
                };
            }

            var response = new VerifiedProviderResponse
            {
                AccountId = account.Id,
                Email = account.Email,
                FullName = $"{account.LastName} {account.FirstName}",
                Avatar = account.Avatar,
                Phone = account.Phone,
                BusinessName = account.BusinessName,
                Bio = account.Bio,
                BusinessAddress = account.BusinessAddress,
                IsProvider = account.IsProvider,
                ProviderVerified = account.ProviderVerified,

                SkillName = account.Skill?.Name,
                DecorationStyleName = account.DecorationStyle?.Name,
                YearsOfExperience = account.YearsOfExperience,
                PastWorkPlaces = account.PastWorkPlaces,
                PastProjects = account.PastProjects,
                CertificateImageUrls = account.CertificateImages?.Select(ci => ci.ImageUrl).ToList() ?? new List<string>()
            };

            return new BaseResponse<VerifiedProviderResponse>
            {
                Success = true,
                Message = "Verified provider retrieved successfully",
                Data = response
            };
        }

        public async Task<BaseResponse<PageResult<VerifiedProviderResponse>>> GetProviderApplicationFilter(ProviderApplicationFilterRequest request)
        {
            var response = new BaseResponse<PageResult<VerifiedProviderResponse>>();
            try
            {
                // Base filter
                Expression<Func<DataAccessObject.Models.Account, bool>> filter = account =>
                    account.RoleId != 1 &&
                    (request.ProviderVerified == null || account.ProviderVerified == request.ProviderVerified) &&
                    (string.IsNullOrEmpty(request.Fullname) ||
                     (account.LastName + " " + account.FirstName).Contains(request.Fullname));

                // Sort
                Expression<Func<DataAccessObject.Models.Account, object>> orderByExpression = request.SortBy switch
                {
                    "FullName" => account => account.LastName + account.FirstName,
                    "BusinessName" => account => account.BusinessName,
                    "CreateAt" => account => account.ApplicationCreateAt,
                    _ => account => account.Id
                };

                // Include related entities
                Func<IQueryable<DataAccessObject.Models.Account>, IQueryable<DataAccessObject.Models.Account>> customQuery = query =>
                    query.Include(a => a.Skill)
                         .Include(a => a.DecorationStyle)
                         .Include(a => a.CertificateImages);

                // Get paginated data
                (IEnumerable<DataAccessObject.Models.Account> providers, int totalCount) = await _unitOfWork.AccountRepository
                    .GetPagedAndFilteredAsync(
                        filter,
                        request.PageIndex,
                        request.PageSize,
                        orderByExpression,
                        request.Descending,
                        null,
                        customQuery
                    );

                // Map to DTO
                var dtos = providers.Select(p => new VerifiedProviderResponse
                {
                    AccountId = p.Id,
                    Email = p.Email,
                    FullName = $"{p.LastName} {p.FirstName}",
                    Avatar = p.Avatar,
                    Phone = p.Phone,
                    IsProvider = p.IsProvider,
                    ProviderVerified = p.ProviderVerified,

                    BusinessName = p.BusinessName,
                    Bio = p.Bio,
                    BusinessAddress = p.BusinessAddress,
                    SkillName = p.Skill?.Name,
                    DecorationStyleName = p.DecorationStyle?.Name,
                    YearsOfExperience = p.YearsOfExperience,
                    PastWorkPlaces = p.PastWorkPlaces,
                    PastProjects = p.PastProjects,
                    CertificateImageUrls = p.CertificateImages?.Select(ci => ci.ImageUrl).ToList() ?? new()
                }).ToList();

                var pageResult = new PageResult<VerifiedProviderResponse>
                {
                    Data = dtos,
                    TotalCount = totalCount
                };

                response.Success = true;
                response.Data = pageResult;
                response.Message = "Providers retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving providers.";
                response.Errors.Add(ex.Message);
                // Log error here
            }
            return response;
        }
    }
}
