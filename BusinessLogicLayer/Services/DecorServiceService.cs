using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using LinqKit;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;
using BusinessLogicLayer.ModelResponse.Product;
using CloudinaryDotNet;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;
using BusinessLogicLayer.ModelResponse.Review;
using static BusinessLogicLayer.ModelResponse.DecorServiceReviewResponse;
using iText.Layout;

namespace BusinessLogicLayer.Services
{
    public class DecorServiceService : IDecorServiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IElasticClientService _elasticClientService;

        public DecorServiceService(IUnitOfWork unitOfWork,
                                   IMapper mapper,
                                   ICloudinaryService cloudinaryService,
                                   IElasticClientService elasticClientService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
            _elasticClientService = elasticClientService;
        }

        public async Task<DecorServiceByIdResponse> GetDecorServiceByIdAsync(int id, int accountId)
        {
            var response = new DecorServiceByIdResponse();
            try
            {
                var decorService = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == id &&
                                 ds.Status == DecorService.DecorServiceStatus.Available)
                    .Include(ds => ds.DecorCategory)
                    .Include(ds => ds.DecorImages)
                    .Include(ds => ds.DecorServiceSeasons)
                        .ThenInclude(dss => dss.Season)
                    .Include(ds => ds.Account)
                        .ThenInclude(a => a.Followers)
                    .Include(ds => ds.Account)
                        .ThenInclude(a => a.Followings)
                    .Include(ds => ds.Reviews)
                        .ThenInclude(r => r.ReviewImages)
                    .Include(ds => ds.Reviews)
                        .ThenInclude(r => r.Account)

                    .Include(ds => ds.DecorServiceThemeColors)
                        .ThenInclude(dstc => dstc.ThemeColor)
                    .Include(ds => ds.DecorServiceStyles)
                        .ThenInclude(dss => dss.DecorationStyle)
                    .Include(ds => ds.DecorServiceOfferings)
                        .ThenInclude(dso => dso.Offering)
                    .FirstOrDefaultAsync();

                if (decorService == null)
                {
                    response.Success = false;
                    response.Message = "Decor service not found.";
                    return response;
                }

                var dto = _mapper.Map<DecorServiceById>(decorService);

                dto.CategoryName = decorService.DecorCategory?.CategoryName;

                var favoriteCount = await _unitOfWork.FavoriteServiceRepository
                    .Query(f => f.DecorServiceId == id)
                    .CountAsync();

                dto.FavoriteCount = favoriteCount;

                dto.Images = decorService.DecorImages?
                    .Select(img => new DecorImageResponse
                    {
                        Id = img.Id,
                        ImageURL = img.ImageURL
                    })
                    .ToList();

                dto.Seasons = decorService.DecorServiceSeasons?
                    .Select(dss => new SeasonResponse
                    {
                        Id = dss.Season.Id,
                        SeasonName = dss.Season.SeasonName
                    })
                    .ToList();

                dto.ThemeColors = decorService.DecorServiceThemeColors?
                    .Select(tc => new ThemeColorResponse
                    {
                        Id = tc.ThemeColor.Id,
                        ColorCode = tc.ThemeColor.ColorCode
                    })
                    .ToList();

                dto.Designs = decorService.DecorServiceStyles?
                    .Select(s => new DesignResponse
                    {
                        Id = s.DecorationStyle.Id,
                        Name = s.DecorationStyle.Name
                    })
                    .ToList();

                dto.Offerings = decorService.DecorServiceOfferings?
                    .Select(o => new OfferingResponse
                    {
                        Id = o.Offering.Id,
                        Name = o.Offering.Name,
                        Description = o.Offering.Description,
                    })
                    .ToList();

                dto.Provider = new ProviderResponse
                {
                    Id = decorService.Account.Id,
                    BusinessName = decorService.Account.BusinessName,
                    Avatar = decorService.Account.Avatar,
                    Slug = decorService.Account.Slug,
                    JoinedDate = decorService.Account.JoinedDate.ToString("dd/MM/yyyy"),
                    FollowersCount = decorService.Account.Followers?.Count ?? 0,
                    FollowingsCount = decorService.Account.Followings?.Count ?? 0
                };

                dto.IsBooked = await _unitOfWork.BookingRepository.Query(b =>
                    b.DecorServiceId == id &&
                    b.AccountId == accountId &&
                    b.IsBooked == true
                ).AnyAsync();

                // Map Reviews
                dto.Reviews = decorService.Reviews?
                    .OrderByDescending(r => r.CreateAt)
                    .Select(r => new DecorServiceReviewResponse
                    {
                        Id = r.Id,
                        Rate = r.Rate,
                        Comment = r.Comment,
                        CreateAt = r.CreateAt.ToString("dd/MM/yyyy"),
                        FullName = r.Account != null ? $"{r.Account.LastName} {r.Account.FirstName}" : "",
                        Avatar = r.Account?.Avatar,
                        ReviewImages = r.ReviewImages?.Select(img => new DecorServiceReviewImageResponse
                        {
                            Id = img.Id,
                            ImageUrl = img.ImageUrl
                        }).ToList() ?? new List<DecorServiceReviewImageResponse>()
                    })
                    .ToList();

                response.Success = true;
                response.Data = dto;
                response.Message = "Decor service retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving decor service.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<DecorServiceListResponse> GetAllDecorServicesAsync()
        {
            var response = new DecorServiceListResponse();
            try
            {
                var services = await _unitOfWork.DecorServiceRepository
                    .Query(ds => !ds.IsDeleted && 
                           ds.StartDate <= DateTime.Now &&
                           ds.Status == DecorService.DecorServiceStatus.Available)

                    .Include(ds => ds.DecorCategory)
                    .Include(ds => ds.DecorImages)
                    .Include(ds => ds.DecorServiceSeasons)
                        .ThenInclude(dss => dss.Season)

                    .Include(ds => ds.Account)
                        .ThenInclude(a => a.Followers)
                    .Include(ds => ds.Account)
                        .ThenInclude(a => a.Followings)

                    .ToListAsync();

                // Map mỗi service sang DecorServiceDTO
                var dtos = _mapper.Map<List<DecorServiceDTO>>(services);

                // Hiện số lượng yêu thích
                var favoriteCounts = await _unitOfWork.FavoriteServiceRepository
                    .Query(f => services.Select(s => s.Id).Contains(f.DecorServiceId))
                    .GroupBy(f => f.DecorServiceId)
                    .Select(g => new { ServiceId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ServiceId, x => x.Count);

                // Map DecorImages -> DecorImageDTO
                for (int i = 0; i < services.Count; i++)
                {
                    var service = services[i];

                    dtos[i].CategoryName = services[i].DecorCategory.CategoryName;

                    dtos[i].Images = services[i].DecorImages
                        .Select(img => new DecorImageResponse
                        {
                            Id = img.Id,
                            ImageURL = img.ImageURL
                        })
                        .ToList();
                    
                    dtos[i].FavoriteCount = favoriteCounts.ContainsKey(services[i].Id) 
                                          ? favoriteCounts[services[i].Id] 
                                          : 0;

                    dtos[i].Seasons = services[i].DecorServiceSeasons
                        .Select(dss => new SeasonResponse
                        {
                            Id = dss.Season.Id,
                            SeasonName = dss.Season.SeasonName
                        })
                        .ToList();

                    dtos[i].Provider = new ProviderResponse
                    {
                        Id = service.Account.Id,
                        BusinessName = service.Account.BusinessName,
                        Bio = service.Account.Bio,
                        Avatar = service.Account.Avatar,
                        Phone = service.Account.Phone,
                        Slug = service.Account.Slug,
                        Address = service.Account.BusinessAddress,
                        JoinedDate = service.Account.JoinedDate.ToString("dd/MM/yyyy"),
                        FollowersCount = service.Account.Followers?.Count ?? 0,
                        FollowingsCount = service.Account.Followings?.Count ?? 0
                    };
                }

                response.Success = true;
                response.Data = dtos;
                response.Message = "Decor services retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving decor services.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<DecorServiceBySlugResponse> GetDecorServiceBySlugAsync(string slug)
        {
            var response = new DecorServiceBySlugResponse();
            try
            {
                // First, find the account by slug
                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Slug == slug && a.ProviderVerified == true)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    response.Success = false;
                    response.Message = "Provider not found.";
                    return response;
                }

                // Get all decor services of the account
                var decorServices = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.AccountId == account.Id &&
                        !ds.IsDeleted && ds.Status == DecorService.DecorServiceStatus.Available)
                    .Include(ds => ds.DecorCategory)
                    .Include(ds => ds.DecorImages)
                    .Include(ds => ds.DecorServiceSeasons)
                        .ThenInclude(dss => dss.Season)
                    .Include(ds => ds.Account)
                        .ThenInclude(a => a.Followers)
                    .Include(ds => ds.Account)
                        .ThenInclude(a => a.Followings)
                    .ToListAsync();

                if (decorServices == null || decorServices.Count == 0)
                {
                    response.Success = false;
                    response.Message = "No decor services found for this provider.";
                    return response;
                }

                var decorServiceDtos = new List<DecorServiceDTO>();

                foreach (var decorService in decorServices)
                {
                    var dto = _mapper.Map<DecorServiceDTO>(decorService);

                    // Get Category Name
                    dto.CategoryName = decorService.DecorCategory?.CategoryName;

                    // Get favorite count
                    var favoriteCount = await _unitOfWork.FavoriteServiceRepository
                        .Query(f => f.DecorServiceId == decorService.Id)
                        .CountAsync();
                    dto.FavoriteCount = favoriteCount;

                    // Map images
                    dto.Images = decorService.DecorImages
                        .Select(img => new DecorImageResponse
                        {
                            Id = img.Id,
                            ImageURL = img.ImageURL
                        })
                        .ToList();

                    // Map seasons
                    dto.Seasons = decorService.DecorServiceSeasons
                        .Select(dss => new SeasonResponse
                        {
                            Id = dss.Season.Id,
                            SeasonName = dss.Season.SeasonName
                        })
                        .ToList();

                    // Map provider information
                    dto.Provider = new ProviderResponse
                    {
                        Id = decorService.Account.Id,
                        BusinessName = decorService.Account.BusinessName,
                        Bio = decorService.Account.Bio,
                        Avatar = decorService.Account.Avatar,
                        Phone = decorService.Account.Phone,
                        Address = decorService.Account.BusinessAddress,
                        JoinedDate = decorService.Account.JoinedDate.ToString("dd/MM/yyyy"),
                        FollowersCount = decorService.Account.Followers?.Count ?? 0,
                        FollowingsCount = decorService.Account.Followings?.Count ?? 0
                    };

                    decorServiceDtos.Add(dto);
                }

                response.Success = true;
                response.Data = decorServiceDtos; // Trả về danh sách decor service DTOs
                response.Message = "Decor services retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving decor services.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<PageResult<DecorServiceDTO>>> GetFilterDecorServicesAsync(DecorServiceFilterRequest request)
        {
            var response = new BaseResponse<PageResult<DecorServiceDTO>>();
            try
            {
                //string? userLocation = null;

                //// ✅ Nếu user đăng nhập, lấy Location từ Account
                //if (accountId.HasValue)
                //{
                //    var userAccount = await _unitOfWork.AccountRepository.GetByIdAsync(accountId.Value);
                //    if (userAccount != null && !string.IsNullOrEmpty(userAccount.Location))
                //    {
                //        userLocation = userAccount.Location;
                //    }
                //}

                //// ✅ Nếu user có chọn Sublocation, dùng Sublocation. Nếu không có, dùng Location từ Account.
                //string locationFilter = !string.IsNullOrEmpty(request.Sublocation) ? request.Sublocation : userLocation;

                // ✅ Nếu user chưa đăng nhập, hoặc không có Location, thì không lọc theo Sublocation
                Expression<Func<DecorService, bool>> filter = decorService =>
                    decorService.IsDeleted == false &&
                    decorService.Status == DecorService.DecorServiceStatus.Available && // Chỉ lấy những service Available
                    (string.IsNullOrEmpty(request.Style) || decorService.Style.Contains(request.Style)) &&
                    //(string.IsNullOrEmpty(locationFilter) || decorService.Sublocation.Contains(locationFilter)) && // Mặc định theo Location hoặc Sublocation
                    (string.IsNullOrEmpty(request.Sublocation) || decorService.Sublocation.Contains(request.Sublocation)) &&
                    (!request.MinPrice.HasValue || decorService.BasePrice >= request.MinPrice.Value) &&
                    (!request.MaxPrice.HasValue || decorService.BasePrice <= request.MaxPrice.Value) &&
                    (!request.DecorCategoryId.HasValue || decorService.DecorCategoryId == request.DecorCategoryId.Value) &&
                    (!request.StartDate.HasValue || decorService.StartDate >= request.StartDate.Value); ;

                if (request.SeasonIds != null && request.SeasonIds.Any())
                {
                    filter = filter.And(decorService =>
                        decorService.DecorServiceSeasons.Any(ds => request.SeasonIds.Contains(ds.SeasonId))
                    );
                }

                // Sort
                Expression<Func<DecorService, object>> orderByExpression = request.SortBy switch
                {
                    "Style" => decorService => decorService.Style,
                    "Sublocation" => decorService => decorService.Sublocation,
                    "CreateAt" => decorService => decorService.CreateAt,
                    "Favorite" => decorService => decorService.FavoriteServices.Count,
                    "StartDate" => decorService => decorService.StartDate,
                    _ => decorService => decorService.Id
                };

                // Include Entities
                Func<IQueryable<DecorService>, IQueryable<DecorService>> customQuery = query =>
                    query.Include(ds => ds.DecorImages)
                         .Include(ds => ds.DecorCategory)
                         .Include(ds => ds.FavoriteServices)
                         .Include(ds => ds.DecorServiceSeasons)
                             .ThenInclude(dss => dss.Season)
                         .Include(ds => ds.Account)
                            .ThenInclude(a => a.Followers)
                         .Include(ds => ds.Account)
                            .ThenInclude(a => a.Followings);

                // Get paginated data and filter
                (IEnumerable<DecorService> decorServices, int totalCount) = await _unitOfWork.DecorServiceRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    null,
                    customQuery
                );

                var dtos = _mapper.Map<List<DecorServiceDTO>>(decorServices);

                // Map dữ liệu
                for (int i = 0; i < decorServices.Count(); i++)
                {
                    var service = decorServices.ElementAt(i);
                    dtos[i].CategoryName = service.DecorCategory.CategoryName;

                    dtos[i].Images = service.DecorImages
                        .Select(img => new DecorImageResponse
                        {
                            Id = img.Id,
                            ImageURL = img.ImageURL
                        })
                        .ToList();

                    dtos[i].Seasons = service.DecorServiceSeasons
                        .Select(dss => new SeasonResponse
                        {
                            Id = dss.Season.Id,
                            SeasonName = dss.Season.SeasonName
                        })
                        .ToList();

                    dtos[i].FavoriteCount = service.FavoriteServices.Count;

                    dtos[i].Provider = new ProviderResponse
                    {
                        Id = service.Account.Id,
                        BusinessName = service.Account.BusinessName,
                        Bio = service.Account.Bio,
                        Avatar = service.Account.Avatar,
                        Phone = service.Account.Phone,
                        Address = service.Account.BusinessAddress,
                        JoinedDate = service.Account.JoinedDate.ToString("dd/MM/yyyy"),
                        FollowersCount = service.Account.Followers?.Count ?? 0,
                        FollowingsCount = service.Account.Followings?.Count ?? 0
                    };
                }

                var pageResult = new PageResult<DecorServiceDTO>
                {
                    Data = dtos,
                    TotalCount = totalCount
                };

                response.Success = true;
                response.Data = pageResult;
                response.Message = "Decor services retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving decor services.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }


        public async Task<BaseResponse<PageResult<DecorServiceDTO>>> GetDecorServiceListByProvider(int accountId, ProviderServiceFilterRequest request)
        {
            var response = new BaseResponse<PageResult<DecorServiceDTO>>();
            try
            {
                // 1. Verify provider account
                var isProvider = await _unitOfWork.AccountRepository
                    .Query(a => a.Id == accountId && a.ProviderVerified == true)
                    .AnyAsync();

                if (!isProvider)
                {
                    response.Success = false;
                    response.Message = "Account is not a provider";
                    return response;
                }

                // 2. Build filter expression
                Expression<Func<DecorService, bool>> filter = decorService =>
                    decorService.AccountId == accountId && // Critical: only get services of this provider
                    //decorService.IsDeleted == false && // Only non-deleted services
                    (string.IsNullOrEmpty(request.Style) || decorService.Style.Contains(request.Style)) &&
                    (string.IsNullOrEmpty(request.Sublocation) || decorService.Sublocation.Contains(request.Sublocation)) &&
                    ((!request.Status.HasValue || decorService.Status == request.Status.Value)) &&
                    (!request.DecorCategoryId.HasValue || decorService.DecorCategoryId == request.DecorCategoryId.Value) &&
                    (!request.MinPrice.HasValue || decorService.BasePrice >= request.MinPrice.Value) &&
                    (!request.MaxPrice.HasValue || decorService.BasePrice <= request.MaxPrice.Value);

                if (request.SeasonIds != null && request.SeasonIds.Any())
                {
                    filter = filter.And(ds =>
                        ds.DecorServiceSeasons.Any(dss => request.SeasonIds.Contains(dss.SeasonId))
                    );
                }

                // 3. Sorting configuration
                Expression<Func<DecorService, object>> orderByExpression = request.SortBy switch
                {
                    "Style" => ds => ds.Style,
                    "Sublocation" => ds => ds.Sublocation,
                    "Status" => ds => ds.Status,
                    "CreateAt" => ds => ds.CreateAt,
                    "BasePrice" => ds => ds.BasePrice,
                    "Favorite" => ds => ds.FavoriteServices.Count,
                    _ => ds => ds.CreateAt // Default sort by creation date
                };

                // 4. Include relationships
                Func<IQueryable<DecorService>, IQueryable<DecorService>> customQuery = query =>
                    query.AsSplitQuery()
                         .Include(ds => ds.DecorCategory)
                         .Include(ds => ds.DecorImages)
                         .Include(ds => ds.DecorServiceSeasons)
                             .ThenInclude(dss => dss.Season)
                         .Include(ds => ds.FavoriteServices)
                         .Include(ds => ds.Account)
                            .ThenInclude(a => a.Followers)
                         .Include(ds => ds.Account)
                            .ThenInclude(a => a.Followings);

                // 5. Get paginated data
                (var services, int totalCount) = await _unitOfWork.DecorServiceRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    null,
                    customQuery
                );

                // 6. Mapping to DTOs
                var dtos = _mapper.Map<List<DecorServiceDTO>>(services);

                // 7. Enrich DTOs with additional data
                for (int i = 0; i < services.Count(); i++)
                {
                    var service = services.ElementAt(i);
                    dtos[i].CategoryName = service.DecorCategory.CategoryName;
                    dtos[i].FavoriteCount = service.FavoriteServices.Count;

                    dtos[i].Images = service.DecorImages.Select(img => new DecorImageResponse
                    {
                        Id = img.Id,
                        ImageURL = img.ImageURL
                    }).ToList();

                    dtos[i].Seasons = service.DecorServiceSeasons.Select(dss => new SeasonResponse
                    {
                        Id = dss.Season.Id,
                        SeasonName = dss.Season.SeasonName
                    }).ToList();

                    // Kiểm tra dịch vụ này đã được đặt chưa
            dtos[i].IsBooked = await _unitOfWork.BookingRepository.Query(b =>
                b.DecorServiceId == service.Id &&
                b.IsBooked == true
            ).AnyAsync();

                }

                // 8. Return paginated result
                response.Data = new PageResult<DecorServiceDTO>
                {
                    Data = dtos,
                    TotalCount = totalCount
                };
                response.Success = true;
                response.Message = "Services retrieved successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving services";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<PageResult<DecorServiceDTO>>> GetDecorServiceListForCustomerAsync(int? providerId, DecorServiceFilterRequest request)
         {
            var response = new BaseResponse<PageResult<DecorServiceDTO>>();
            try
            {
                // 1. Build filter expression (giống GetFilterDecorServicesAsync nhưng không cần accountId)
                Expression<Func<DecorService, bool>> filter = ds =>
                    ds.IsDeleted == false &&
                    ds.StartDate <= DateTime.Now &&
                    ds.Status == DecorService.DecorServiceStatus.Available &&
                    (string.IsNullOrEmpty(request.Style) || ds.Style.Contains(request.Style)) &&
                    (string.IsNullOrEmpty(request.Sublocation) || ds.Sublocation.Contains(request.Sublocation)) &&
                    (!request.DecorCategoryId.HasValue || ds.DecorCategoryId == request.DecorCategoryId.Value) &&
                    (!request.MinPrice.HasValue || ds.BasePrice >= request.MinPrice.Value) &&
                    (!request.MaxPrice.HasValue || ds.BasePrice <= request.MaxPrice.Value) &&
                    (!providerId.HasValue || ds.AccountId == providerId.Value);

                if (request.SeasonIds != null && request.SeasonIds.Any())
                {
                    filter = filter.And(ds =>
                        ds.DecorServiceSeasons.Any(dss => request.SeasonIds.Contains(dss.SeasonId))
                    );
                }

                // 2. Sorting config
                Expression<Func<DecorService, object>> orderByExpression = request.SortBy switch
                {
                    "Style" => ds => ds.Style,
                    "Sublocation" => ds => ds.Sublocation,
                    "CreateAt" => ds => ds.CreateAt,
                    "BasePrice" => ds => ds.BasePrice,
                    "Favorite" => ds => ds.FavoriteServices.Count,
                    _ => ds => ds.CreateAt
                };

                // 3. Include relationships
                Func<IQueryable<DecorService>, IQueryable<DecorService>> customQuery = query =>
                    query.AsSplitQuery()
                         .Include(ds => ds.DecorCategory)
                         .Include(ds => ds.DecorImages)
                         .Include(ds => ds.DecorServiceSeasons)
                             .ThenInclude(dss => dss.Season)
                         .Include(ds => ds.FavoriteServices)
                         .Include(ds => ds.Account)
                            .ThenInclude(a => a.Followers)
                         .Include(ds => ds.Account)
                            .ThenInclude(a => a.Followings);

                // 4. Get paginated data
                (var services, int totalCount) = await _unitOfWork.DecorServiceRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    null,
                    customQuery
                );

                // 5. Mapping to DTOs
                var dtos = _mapper.Map<List<DecorServiceDTO>>(services);

                for (int i = 0; i < services.Count(); i++)
                {
                    var service = services.ElementAt(i);
                    dtos[i].CategoryName = service.DecorCategory.CategoryName;
                    dtos[i].FavoriteCount = service.FavoriteServices.Count;

                    dtos[i].Images = service.DecorImages.Select(img => new DecorImageResponse
                    {
                        Id = img.Id,
                        ImageURL = img.ImageURL
                    }).ToList();

                    dtos[i].Seasons = service.DecorServiceSeasons.Select(dss => new SeasonResponse
                    {
                        Id = dss.Season.Id,
                        SeasonName = dss.Season.SeasonName
                    }).ToList();

                    dtos[i].Provider = new ProviderResponse
                    {
                        Id = service.Account.Id,
                        BusinessName = service.Account.BusinessName,
                        Bio = service.Account.Bio,
                        Avatar = service.Account.Avatar,
                        Phone = service.Account.Phone,
                        Address = service.Account.BusinessAddress,
                        Slug = service.Account.Slug,
                        JoinedDate = service.Account.JoinedDate.ToString("dd/MM/yyyy"),
                        FollowersCount = service.Account.Followers?.Count ?? 0,
                        FollowingsCount = service.Account.Followings?.Count ?? 0
                    };
                }

                response.Data = new PageResult<DecorServiceDTO>
                {
                    Data = dtos,
                    TotalCount = totalCount
                };
                response.Success = true;
                response.Message = "Services retrieved successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving services";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> CreateDecorServiceAsync(CreateDecorServiceRequest request, int accountId)
        {
            var response = new BaseResponse();
            try
            {
                var account = await _unitOfWork.AccountRepository
                        .Query(a => a.Id == accountId && a.RoleId == 2)
                        .FirstOrDefaultAsync();

                if (account == null)
                {
                    response.Success = false;
                    response.Message = "Only a Provider is allowed to create a decor service.";
                    return response;
                }

                if (request.Images != null && request.Images.Count > 5)
                {
                    response.Success = false;
                    response.Message = "Maximum 5 images are allowed.";
                    return response;
                }

                if (request.SeasonIds == null || !request.SeasonIds.Any())
                {
                    response.Success = false;
                    response.Message = "Please choose at least one season";
                    return response;
                }

                var decorService = new DecorService
                {
                    Style = request.Style,
                    Description = request.Description,
                    Sublocation = request.Sublocation,
                    AccountId = accountId,
                    DecorCategoryId = request.DecorCategoryId,
                    CreateAt = DateTime.Now,
                    StartDate = request.StartDate,
                    Status = DecorService.DecorServiceStatus.Incoming,
                    DecorImages = new List<DecorImage>(),
                    DecorServiceSeasons = new List<DecorServiceSeason>(),

                    DecorServiceThemeColors = new List<DecorServiceThemeColor>(),
                    DecorServiceStyles = new List<DecorServiceStyle>(),
                    DecorServiceOfferings = new List<DecorServiceOffering>()    
                };

                // Thêm tag mùa vào dịch vụ
                if (request.SeasonIds != null && request.SeasonIds.Any())
                {
                    foreach (var seasonId in request.SeasonIds)
                    {
                        decorService.DecorServiceSeasons.Add(new DecorServiceSeason
                        {
                            SeasonId = seasonId
                        });
                    }
                }

                // Thêm style đã seed (nhiều style)
                if (request.StyleIds != null && request.StyleIds.Any())
                {
                    foreach (var styleId in request.StyleIds.Distinct())
                    {
                        decorService.DecorServiceStyles.Add(new DecorServiceStyle
                        {
                            DecorationStyleId = styleId
                        });
                    }
                }

                // Thêm màu người dùng nhập
                if (request.ThemeColorNames != null && request.ThemeColorNames.Any())
                {
                    foreach (var colorName in request.ThemeColorNames.Distinct())
                    {
                        var trimmedName = colorName.Trim();

                        var existingColor = await _unitOfWork.ThemeColorRepository
                            .Query(t => t.ColorCode.ToLower() == trimmedName.ToLower())
                            .FirstOrDefaultAsync();

                        if (existingColor == null)
                        {
                            existingColor = new ThemeColor 
                            { 
                                ColorCode = trimmedName 
                            };

                            await _unitOfWork.ThemeColorRepository.InsertAsync(existingColor);
                            await _unitOfWork.CommitAsync(); // Để lấy Id
                        }

                        decorService.DecorServiceThemeColors.Add(new DecorServiceThemeColor
                        {
                            ThemeColorId = existingColor.Id
                        });
                    }
                }

                // Thêm offerings đã chọn (chỉ ID)
                if (request.OfferingIds != null && request.OfferingIds.Any())
                {
                    foreach (var offeringId in request.OfferingIds.Distinct())
                    {
                        decorService.DecorServiceOfferings.Add(new DecorServiceOffering
                        {
                            OfferingId = offeringId
                        });
                    }
                }

                // Nếu có ảnh, upload
                if (request.Images != null && request.Images.Any())
                {
                    foreach (var imageFile in request.Images)
                    {
                        using var stream = imageFile.OpenReadStream();
                        var imageUrl = await _cloudinaryService.UploadFileAsync(
                            stream,
                            imageFile.FileName,
                            imageFile.ContentType
                        );
                        decorService.DecorImages.Add(new DecorImage { ImageURL = imageUrl });
                    }
                }

                await _unitOfWork.DecorServiceRepository.InsertAsync(decorService);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Decor service created successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error creating decor service.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> UpdateDecorServiceAsync(int id, UpdateDecorServiceRequest request, int accountId)
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
                    response.Message = "Only a Provider is allowed to update a decor service.";
                    return response;
                }

                var decorService = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == id)
                    .Include(ds => ds.DecorServiceSeasons) // Include danh sách mùa
                    .FirstOrDefaultAsync();

                if (decorService == null)
                {
                    response.Success = false;
                    response.Message = "Decor service not found.";
                    return response;
                }

                decorService.Style = request.Style;
                decorService.Description = request.Description;
                decorService.Sublocation = request.Sublocation;
                decorService.AccountId = accountId;
                decorService.DecorCategoryId = request.DecorCategoryId;

                // Cập nhật danh sách mùa
                if (request.SeasonIds != null)
                {
                    // Xóa tất cả mùa cũ
                    decorService.DecorServiceSeasons.Clear();

                    // Thêm mùa mới
                    foreach (var seasonId in request.SeasonIds)
                    {
                        decorService.DecorServiceSeasons.Add(new DecorServiceSeason
                        {
                            SeasonId = seasonId
                        });
                    }
                }

                _unitOfWork.DecorServiceRepository.Update(decorService);
                await _unitOfWork.CommitAsync();

                // *** Cập nhật index trên Elasticsearch (không throw exception nếu lỗi)
                try
                {
                    await _elasticClientService.IndexDecorServiceAsync(decorService);
                }
                catch (Exception ex)
                {
                    // Log lỗi nếu cần
                }

                response.Success = true;
                response.Message = "Decor service updated successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating decor service.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        //public async Task<BaseResponse> UpdateDecorServiceAsyncWithImage(int id, UpdateDecorServiceRequest request, int accountId)
        //{
        //    var response = new BaseResponse();
        //    try
        //    {
        //        var decorService = await _unitOfWork.DecorServiceRepository
        //            .Query(ds => ds.Id == id)
        //            .Include(ds => ds.DecorImages)
        //            .FirstOrDefaultAsync();

        //        if (decorService == null)
        //        {
        //            response.Success = false;
        //            response.Message = "Decor service not found.";
        //            return response;
        //        }

        //        decorService.Style = request.Style;
        //        decorService.Description = request.Description;
        //        decorService.Province = request.Province;
        //        decorService.DecorCategoryId = request.DecorCategoryId;
        //        decorService.AccountId = accountId;

        //        if (request.ImageIdsToRemove != null && request.ImageIdsToRemove.Any())
        //        {
        //            var imagesToRemove = decorService.DecorImages
        //                .Where(img => request.ImageIdsToRemove.Contains(img.Id))
        //                .ToList();

        //            foreach (var img in imagesToRemove)
        //            {
        //                decorService.DecorImages.Remove(img);
        //            }
        //        }

        //        if (request.ImagesToAdd != null && request.ImagesToAdd.Any())
        //        {
        //            foreach (var imageFile in request.ImagesToAdd)
        //            {
        //                using var stream = imageFile.OpenReadStream();
        //                var imageUrl = await _cloudinaryService.UploadFileAsync(
        //                    stream,
        //                    imageFile.FileName,
        //                    imageFile.ContentType
        //                );
        //                var newImage = new DecorImage
        //                {
        //                    ImageURL = imageUrl

        //                };
        //                decorService.DecorImages.Add(newImage);
        //            }
        //        }

        //        _unitOfWork.DecorServiceRepository.Update(decorService);
        //        await _unitOfWork.CommitAsync();

        //        try
        //        {
        //            await _elasticClientService.IndexDecorServiceAsync(decorService);
        //        }
        //        catch (Exception ex)
        //        {
        //        }
        //        response.Success = true;
        //        response.Message = "Decor service updated successfully.";
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Success = false;
        //        response.Message = "Error updating decor service.";
        //        response.Errors.Add(ex.Message);
        //    }
        //    return response;
        //}

        public async Task<BaseResponse> DeleteDecorServiceAsync(int id, int accountId)
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
                    response.Message = "Only a Provider is allowed to delete a decor service.";
                    return response;
                }

                var decorService = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == id)
                    .FirstOrDefaultAsync();

                if (decorService == null)
                {
                    response.Success = false;
                    response.Message = "Decor service not found.";
                    return response;
                }

                // Hard-delete cũ:
                // _unitOfWork.DecorServiceRepository.Delete(decorService);

                // Thay bằng soft-delete:
                decorService.IsDeleted = true;
                _unitOfWork.DecorServiceRepository.Update(decorService);

                await _unitOfWork.CommitAsync();

                // Xoá luôn trên Elasticsearch (nếu muốn ẩn hẳn trên ES),
                // hoặc bạn có thể update document "IsDeleted = true" tuỳ logic.
                try
                {
                    // Hard-delete document trên ES
                    await _elasticClientService.DeleteDecorServiceAsync(id);
                }
                catch (Exception ex)
                {
                    // Ghi log nếu cần
                }

                response.Success = true;
                response.Message = "Decor service soft-deleted successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error soft-deleting decor service.";
                response.Errors.Add(ex.ToString());
            }
            return response;
        }

        //option khôi phục service
        public async Task<BaseResponse> RestoreDecorServiceAsync(int id, int accountId)
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
                    response.Message = "Only a Provider is allowed to restore a decor service.";
                    return response;
                }

                var decorService = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == id)
                    .FirstOrDefaultAsync();

                if (decorService == null)
                {
                    response.Success = false;
                    response.Message = "Decor service not found.";
                    return response;
                }

                // Lật cờ IsDeleted
                if (!decorService.IsDeleted)
                {
                    response.Success = false;
                    response.Message = "Decor service is not deleted.";
                    return response;
                }

                decorService.IsDeleted = false;
                _unitOfWork.DecorServiceRepository.Update(decorService);
                await _unitOfWork.CommitAsync();

                // Index lại document trên ES
                try
                {
                    await _elasticClientService.IndexDecorServiceAsync(decorService);
                }
                catch (Exception ex)
                {
                    // Log nếu cần
                }

                response.Success = true;
                response.Message = "Decor service restored successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error restoring decor service.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<DecorServiceListResponse> SearchDecorServices(string keyword)
        {
            var response = new DecorServiceListResponse();
            try
            {
                // Gọi hàm search bên ElasticClientService
                var results = await _elasticClientService.SearchDecorServicesAsync(keyword);

                // Chuyển về DTO
                var dtos = _mapper.Map<List<DecorServiceDTO>>(results);

                response.Success = true;
                response.Data = dtos;
                response.Message = "Search completed successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error searching decor services.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<DecorServiceListResponse> SearchMultiCriteriaDecorServices(SearchDecorServiceRequest request)
        {
            var response = new DecorServiceListResponse();
            try
            {
                var query = _unitOfWork.DecorServiceRepository.Query(ds => !ds.IsDeleted &&
                    ds.StartDate <= DateTime.Now &&
                    ds.Status == DecorService.DecorServiceStatus.Available);

                if (!string.IsNullOrEmpty(request.Style))
                    query = query.Where(ds => ds.Style.Contains(request.Style));

                if (!string.IsNullOrEmpty(request.Sublocation))
                    query = query.Where(ds => ds.Sublocation.Contains(request.Sublocation));

                if (!string.IsNullOrEmpty(request.CategoryName))
                    query = query.Where(ds => ds.DecorCategory.CategoryName.Contains(request.CategoryName));

                if (request.SeasonNames != null && request.SeasonNames.Any())
                {
                    query = query.Where(ds => ds.DecorServiceSeasons.Any(dss => request.SeasonNames.Contains(dss.Season.SeasonName)));
                }

                var decorServices = await query
                    .Include(ds => ds.DecorCategory)
                    .Include(ds => ds.DecorImages)
                    .Include(ds => ds.DecorServiceSeasons)
                        .ThenInclude(dss => dss.Season)
                    .ToListAsync();

                var dtos = decorServices.Select(ds => new DecorServiceDTO
                {
                    Id = ds.Id,
                    Style = ds.Style,
                    Description = ds.Description,
                    Sublocation = ds.Sublocation,
                    CreateAt = ds.CreateAt,
                    StartDate = ds.StartDate,
                    AccountId = ds.AccountId,
                    FavoriteCount = 0,
                    CategoryName = ds.DecorCategory.CategoryName,
                    Images = ds.DecorImages.Select(img => new DecorImageResponse
                    {
                        Id = img.Id,
                        ImageURL = img.ImageURL
                    }).ToList(),
                    Seasons = ds.DecorServiceSeasons.Select(dss => new SeasonResponse
                    {
                        Id = dss.Season.Id,
                        SeasonName = dss.Season.SeasonName
                    }).ToList()
                }).ToList();

                response.Success = true;
                response.Data = dtos;
                response.Message = "Multi-criteria search completed successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error performing multi-criteria search.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> ChangeStartDateAsync(int decorServiceId, ChangeStartDateRequest request, int accountId)
        {
            var response = new BaseResponse();
            try
            {
                var decorService = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == decorServiceId && ds.AccountId == accountId)
                    .FirstOrDefaultAsync();

                if (decorService == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Decor service not found or you do not have permission to modify it."
                    };
                }

                if (request.StartDate < DateTime.Now.Date)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Start date cannot be in the past."
                    };
                }

                decorService.StartDate = request.StartDate;
                decorService.Status = DecorService.DecorServiceStatus.Incoming;
                _unitOfWork.DecorServiceRepository.Update(decorService);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Start date updated successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating start date.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<DecorServiceListResponse> GetIncomingDecorServiceListAsync()
        {
            var response = new DecorServiceListResponse();
            try
            {
                // Lọc theo trạng thái Incoming
                var services = await _unitOfWork.DecorServiceRepository
                    .Query(ds => !ds.IsDeleted && ds.Status == DecorService.DecorServiceStatus.Incoming)  // Trạng thái Incoming

                    .Include(ds => ds.DecorCategory)
                    .Include(ds => ds.DecorImages)
                    .Include(ds => ds.DecorServiceSeasons)
                        .ThenInclude(dss => dss.Season)
                    .Include(ds => ds.Account)
                        .ThenInclude(a => a.Followers)
                    .Include(ds => ds.Account)
                        .ThenInclude(a => a.Followings)

                    .ToListAsync();

                // Map mỗi service sang DecorServiceDTO
                var dtos = _mapper.Map<List<DecorServiceDTO>>(services);

                //// Hiện số lượng yêu thích
                //var favoriteCounts = await _unitOfWork.FavoriteServiceRepository
                //    .Query(f => services.Select(s => s.Id).Contains(f.DecorServiceId))
                //    .GroupBy(f => f.DecorServiceId)
                //    .Select(g => new { ServiceId = g.Key, Count = g.Count() })
                //    .ToDictionaryAsync(x => x.ServiceId, x => x.Count);

                // Map DecorImages -> DecorImageDTO
                for (int i = 0; i < services.Count; i++)
                {
                    var service = services[i];

                    dtos[i].CategoryName = services[i].DecorCategory.CategoryName;

                    dtos[i].Images = services[i].DecorImages
                        .Select(img => new DecorImageResponse
                        {
                            Id = img.Id,
                            ImageURL = img.ImageURL
                        })
                        .ToList();

                    //dtos[i].FavoriteCount = favoriteCounts.ContainsKey(services[i].Id)
                    //                      ? favoriteCounts[services[i].Id]
                    //                      : 0;

                    dtos[i].Seasons = services[i].DecorServiceSeasons
                        .Select(dss => new SeasonResponse
                        {
                            Id = dss.Season.Id,
                            SeasonName = dss.Season.SeasonName
                        })
                        .ToList();

                    dtos[i].Provider = new ProviderResponse
                    {
                        Id = service.Account.Id,
                        BusinessName = service.Account.BusinessName,
                        Bio = service.Account.Bio,
                        Avatar = service.Account.Avatar,
                        Phone = service.Account.Phone,
                        Slug = service.Account.Slug,
                        Address = service.Account.BusinessAddress,
                        JoinedDate = service.Account.JoinedDate.ToString("dd/MM/yyyy"),
                        FollowersCount = service.Account.Followers?.Count ?? 0,
                        FollowingsCount = service.Account.Followings?.Count ?? 0
                    };
                }

                response.Success = true;
                response.Data = dtos;
                response.Message = "Decor services retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving decor services.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<List<ThemeColorResponse>>> GetThemeColorsByDecorServiceIdAsync(int decorServiceId)
        {
            var response = new BaseResponse<List<ThemeColorResponse>>();
            try
            {
                var service = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == decorServiceId)
                    .Include(ds => ds.DecorServiceThemeColors)
                        .ThenInclude(dstc => dstc.ThemeColor)
                    .FirstOrDefaultAsync();

                if (service == null)
                {
                    response.Success = false;
                    response.Message = "Decor service not found.";
                    return response;
                }

                var themeColors = service.DecorServiceThemeColors?
                    .Select(tc => new ThemeColorResponse
                    {
                        Id = tc.ThemeColor.Id,
                        ColorCode = tc.ThemeColor.ColorCode
                    })
                    .ToList();

                response.Success = true;
                response.Data = themeColors;
                response.Message = "Theme colors fetched successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error fetching theme colors.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<List<DesignResponse>>> GetStylesByDecorServiceIdAsync(int decorServiceId)
        {
            var response = new BaseResponse<List<DesignResponse>>();
            try
            {
                var service = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == decorServiceId)
                    .Include(ds => ds.DecorServiceStyles)
                        .ThenInclude(dss => dss.DecorationStyle)
                    .FirstOrDefaultAsync();

                if (service == null)
                {
                    response.Success = false;
                    response.Message = "Decor service not found.";
                    return response;
                }

                var styles = service.DecorServiceStyles?
                    .Select(s => new DesignResponse
                    {
                        Id = s.DecorationStyle.Id,
                        Name = s.DecorationStyle.Name
                    })
                    .ToList();

                response.Success = true;
                response.Data = styles;
                response.Message = "Styles fetched successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error fetching styles.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<DecorServiceDetailsResponse>> GetStyleNColorByServiceIdAsync(int decorServiceId)
        {
            var response = new BaseResponse<DecorServiceDetailsResponse>();
            try
            {
                var service = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == decorServiceId)
                    .Include(ds => ds.DecorServiceThemeColors)
                        .ThenInclude(dstc => dstc.ThemeColor)
                    .Include(ds => ds.DecorServiceStyles)
                        .ThenInclude(dss => dss.DecorationStyle)
                    .FirstOrDefaultAsync();

                if (service == null)
                {
                    response.Success = false;
                    response.Message = "Decor service not found.";
                    return response;
                }

                var themeColors = service.DecorServiceThemeColors?
                    .Select(tc => new ThemeColorResponse
                    {
                        Id = tc.ThemeColor.Id,
                        ColorCode = tc.ThemeColor.ColorCode
                    })
                    .ToList();

                var styles = service.DecorServiceStyles?
                    .Select(s => new DesignResponse
                    {
                        Id = s.DecorationStyle.Id,
                        Name = s.DecorationStyle.Name
                    })
                    .ToList();

                response.Success = true;
                response.Data = new DecorServiceDetailsResponse
                {
                    ThemeColors = themeColors,
                    Designs = styles
                };
                response.Message = "Theme colors and styles fetched successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error fetching theme colors and styles.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<OfferingAndDesignResponse>> GetAllOfferingAndStylesAsync()
        {
            try
            {
                // Sử dụng GenericRepository thông qua UnitOfWork
                var offerings = await _unitOfWork.OfferingRepository.Queryable()
                    .Select(s => new OfferingResponse
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description
                    })
                    .ToListAsync();

                var styles = await _unitOfWork.DecorationStyleRepository.Queryable()
                    .Select(ds => new DesignResponse
                    {
                        Id = ds.Id,
                        Name = ds.Name
                    })
                    .ToListAsync();

                // Tạo response object
                var response = new OfferingAndDesignResponse
                {
                    Offerings = offerings,
                    Designs = styles
                };

                return new BaseResponse<OfferingAndDesignResponse>
                {
                    Success = true,
                    Message = "Skills and decoration styles retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<OfferingAndDesignResponse>
                {
                    Success = false,
                    Message = "Failed to retrieve offerings and decoration styles",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> SetUserPreferencesAsync(SetPreferenceRequest request, int accountId)
        {
            var response = new BaseResponse();
            try
            {
                await _unitOfWork.AccountStylePreferenceRepository.DeleteAsync(p => p.AccountId == accountId);
                await _unitOfWork.AccountSeasonPreferenceRepository.DeleteAsync(p => p.AccountId == accountId);
                await _unitOfWork.AccountCategoryPreferenceRepository.DeleteAsync(p => p.AccountId == accountId);

                await _unitOfWork.AccountStylePreferenceRepository.InsertRangeAsync(
                    request.StyleIds.Select(id => new AccountStylePreference
                    {
                        AccountId = accountId,
                        DecorationStyleId = id
                    }));

                await _unitOfWork.AccountSeasonPreferenceRepository.InsertRangeAsync(
                    request.SeasonIds.Select(id => new AccountSeasonPreference
                    {
                        AccountId = accountId,
                        SeasonId = id
                    }));

                await _unitOfWork.AccountCategoryPreferenceRepository.InsertRangeAsync(
                    request.CategoryIds.Select(id => new AccountCategoryPreference
                    {
                        AccountId = accountId,
                        DecorCategoryId = id
                    }));

                var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
                if (account != null)
                {
                    account.IsFilterEnabled = true;
                    _unitOfWork.AccountRepository.Update(account);
                }

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Preferences saved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to save preferences.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<PageResult<DecorServiceDTO>>> GetFilterDecorServicesAsync(int? accountId, DecorServiceFilterRequest request)
        {
            var response = new BaseResponse<PageResult<DecorServiceDTO>>();

            try
            {
                // 🔍 1. Lấy preference nếu có accountId
                List<int> styleIds = new();
                List<int> seasonIds = new();
                List<int> categoryIds = new();

                if (accountId.HasValue)
                {
                    var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId.Value);
                    if (account?.IsFilterEnabled == true)
                    {
                        styleIds = await _unitOfWork.AccountStylePreferenceRepository
                            .Query(p => p.AccountId == accountId.Value)
                            .Select(p => p.DecorationStyleId)
                            .ToListAsync();

                        seasonIds = await _unitOfWork.AccountSeasonPreferenceRepository
                            .Query(p => p.AccountId == accountId.Value)
                            .Select(p => p.SeasonId)
                            .ToListAsync();

                        categoryIds = await _unitOfWork.AccountCategoryPreferenceRepository
                            .Query(p => p.AccountId == accountId.Value)
                            .Select(p => p.DecorCategoryId)
                            .ToListAsync();
                    }
                }

                // 🔍 2. Tạo filter expression
                Expression<Func<DecorService, bool>> filter = ds =>
                    ds.IsDeleted == false &&
                    ds.Status == DecorService.DecorServiceStatus.Available &&
                    (string.IsNullOrEmpty(request.Style) || ds.Style.Contains(request.Style)) &&
                    (string.IsNullOrEmpty(request.Sublocation) || ds.Sublocation.Contains(request.Sublocation)) &&
                    (!request.MinPrice.HasValue || ds.BasePrice >= request.MinPrice.Value) &&
                    (!request.MaxPrice.HasValue || ds.BasePrice <= request.MaxPrice.Value) &&
                    (!request.DecorCategoryId.HasValue || ds.DecorCategoryId == request.DecorCategoryId.Value) &&
                    (!request.StartDate.HasValue || ds.StartDate >= request.StartDate.Value);

                if (request.SeasonIds != null && request.SeasonIds.Any())
                {
                    filter = filter.And(ds => ds.DecorServiceSeasons.Any(s => request.SeasonIds.Contains(s.SeasonId)));
                }

                // 🔍 3. Thêm personalization nếu có
                if (styleIds.Any())
                {
                    filter = filter.And(ds => ds.DecorServiceStyles.Any(s => styleIds.Contains(s.DecorationStyleId)));
                }

                if (seasonIds.Any())
                {
                    filter = filter.And(ds => ds.DecorServiceSeasons.Any(s => seasonIds.Contains(s.SeasonId)));
                }

                if (categoryIds.Any())
                {
                    filter = filter.And(ds => categoryIds.Contains(ds.DecorCategoryId));
                }

                // 🔍 4. Sắp xếp
                Expression<Func<DecorService, object>> orderByExpression = request.SortBy switch
                {
                    "Style" => ds => ds.Style,
                    "Sublocation" => ds => ds.Sublocation,
                    "CreateAt" => ds => ds.CreateAt,
                    "Favorite" => ds => ds.FavoriteServices.Count,
                    "StartDate" => ds => ds.StartDate,
                    _ => ds => ds.Id
                };

                // 🔍 5. Include liên quan
                Func<IQueryable<DecorService>, IQueryable<DecorService>> customQuery = query =>
                    query.Include(ds => ds.DecorImages)
                         .Include(ds => ds.DecorCategory)
                         .Include(ds => ds.FavoriteServices)
                         .Include(ds => ds.DecorServiceSeasons)
                            .ThenInclude(s => s.Season)
                         .Include(ds => ds.Account)
                            .ThenInclude(a => a.Followers)
                         .Include(ds => ds.Account)
                            .ThenInclude(a => a.Followings);

                // 🔍 6. Phân trang
                var (decorServices, totalCount) = await _unitOfWork.DecorServiceRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    null,
                    customQuery
                );

                // 🔁 7. Map DTO
                var dtos = _mapper.Map<List<DecorServiceDTO>>(decorServices);

                for (int i = 0; i < decorServices.Count(); i++)
                {
                    var service = decorServices.ElementAt(i);
                    dtos[i].CategoryName = service.DecorCategory?.CategoryName;
                    dtos[i].Images = service.DecorImages
                        .Select(img => new DecorImageResponse { Id = img.Id, ImageURL = img.ImageURL }).ToList();
                    dtos[i].Seasons = service.DecorServiceSeasons
                        .Select(s => new SeasonResponse { Id = s.Season.Id, SeasonName = s.Season.SeasonName }).ToList();
                    dtos[i].FavoriteCount = service.FavoriteServices.Count;
                    dtos[i].Provider = new ProviderResponse
                    {
                        Id = service.Account.Id,
                        BusinessName = service.Account.BusinessName,
                        Bio = service.Account.Bio,
                        Avatar = service.Account.Avatar,
                        Phone = service.Account.Phone,
                        Address = service.Account.BusinessAddress,
                        JoinedDate = service.Account.JoinedDate.ToString("dd/MM/yyyy"),
                        FollowersCount = service.Account.Followers?.Count ?? 0,
                        FollowingsCount = service.Account.Followings?.Count ?? 0
                    };
                }

                // ✅ Trả kết quả
                response.Success = true;
                response.Message = "Decor services retrieved successfully.";
                response.Data = new PageResult<DecorServiceDTO>
                {
                    Data = dtos,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving decor services.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }


        //public async Task<BaseResponse<PageResult<DecorServiceDTO>>> GetFilterDecorServicesAsync(DecorServiceFilterRequest request)
        //{
        //    var response = new BaseResponse<PageResult<DecorServiceDTO>>();
        //    try
        //    {
        //        //string? userLocation = null;

        //        //// ✅ Nếu user đăng nhập, lấy Location từ Account
        //        //if (accountId.HasValue)
        //        //{
        //        //    var userAccount = await _unitOfWork.AccountRepository.GetByIdAsync(accountId.Value);
        //        //    if (userAccount != null && !string.IsNullOrEmpty(userAccount.Location))
        //        //    {
        //        //        userLocation = userAccount.Location;
        //        //    }
        //        //}

        //        //// ✅ Nếu user có chọn Sublocation, dùng Sublocation. Nếu không có, dùng Location từ Account.
        //        //string locationFilter = !string.IsNullOrEmpty(request.Sublocation) ? request.Sublocation : userLocation;

        //        // ✅ Nếu user chưa đăng nhập, hoặc không có Location, thì không lọc theo Sublocation
        //        Expression<Func<DecorService, bool>> filter = decorService =>
        //            decorService.IsDeleted == false &&
        //            decorService.Status == DecorService.DecorServiceStatus.Available && // Chỉ lấy những service Available
        //            (string.IsNullOrEmpty(request.Style) || decorService.Style.Contains(request.Style)) &&
        //            //(string.IsNullOrEmpty(locationFilter) || decorService.Sublocation.Contains(locationFilter)) && // Mặc định theo Location hoặc Sublocation
        //            (string.IsNullOrEmpty(request.Sublocation) || decorService.Sublocation.Contains(request.Sublocation)) &&
        //            (!request.MinPrice.HasValue || decorService.BasePrice >= request.MinPrice.Value) &&
        //            (!request.MaxPrice.HasValue || decorService.BasePrice <= request.MaxPrice.Value) &&
        //            (!request.DecorCategoryId.HasValue || decorService.DecorCategoryId == request.DecorCategoryId.Value) &&
        //            (!request.StartDate.HasValue || decorService.StartDate >= request.StartDate.Value); ;

        //        if (request.SeasonIds != null && request.SeasonIds.Any())
        //        {
        //            filter = filter.And(decorService =>
        //                decorService.DecorServiceSeasons.Any(ds => request.SeasonIds.Contains(ds.SeasonId))
        //            );
        //        }

        //        // Sort
        //        Expression<Func<DecorService, object>> orderByExpression = request.SortBy switch
        //        {
        //            "Style" => decorService => decorService.Style,
        //            "Sublocation" => decorService => decorService.Sublocation,
        //            "CreateAt" => decorService => decorService.CreateAt,
        //            "Favorite" => decorService => decorService.FavoriteServices.Count,
        //            "StartDate" => decorService => decorService.StartDate,
        //            _ => decorService => decorService.Id
        //        };

        //        // Include Entities
        //        Func<IQueryable<DecorService>, IQueryable<DecorService>> customQuery = query =>
        //            query.Include(ds => ds.DecorImages)
        //                 .Include(ds => ds.DecorCategory)
        //                 .Include(ds => ds.FavoriteServices)
        //                 .Include(ds => ds.DecorServiceSeasons)
        //                     .ThenInclude(dss => dss.Season)
        //                 .Include(ds => ds.Account)
        //                    .ThenInclude(a => a.Followers)
        //                 .Include(ds => ds.Account)
        //                    .ThenInclude(a => a.Followings);

        //        // Get paginated data and filter
        //        (IEnumerable<DecorService> decorServices, int totalCount) = await _unitOfWork.DecorServiceRepository.GetPagedAndFilteredAsync(
        //            filter,
        //            request.PageIndex,
        //            request.PageSize,
        //            orderByExpression,
        //            request.Descending,
        //            null,
        //            customQuery
        //        );

        //        var dtos = _mapper.Map<List<DecorServiceDTO>>(decorServices);

        //        // Map dữ liệu
        //        for (int i = 0; i < decorServices.Count(); i++)
        //        {
        //            var service = decorServices.ElementAt(i);
        //            dtos[i].CategoryName = service.DecorCategory.CategoryName;

        //            dtos[i].Images = service.DecorImages
        //                .Select(img => new DecorImageResponse
        //                {
        //                    Id = img.Id,
        //                    ImageURL = img.ImageURL
        //                })
        //                .ToList();

        //            dtos[i].Seasons = service.DecorServiceSeasons
        //                .Select(dss => new SeasonResponse
        //                {
        //                    Id = dss.Season.Id,
        //                    SeasonName = dss.Season.SeasonName
        //                })
        //                .ToList();

        //            dtos[i].FavoriteCount = service.FavoriteServices.Count;

        //            dtos[i].Provider = new ProviderResponse
        //            {
        //                Id = service.Account.Id,
        //                BusinessName = service.Account.BusinessName,
        //                Bio = service.Account.Bio,
        //                Avatar = service.Account.Avatar,
        //                Phone = service.Account.Phone,
        //                Address = service.Account.BusinessAddress,
        //                JoinedDate = service.Account.JoinedDate.ToString("dd/MM/yyyy"),
        //                FollowersCount = service.Account.Followers?.Count ?? 0,
        //                FollowingsCount = service.Account.Followings?.Count ?? 0
        //            };
        //        }

        //        var pageResult = new PageResult<DecorServiceDTO>
        //        {
        //            Data = dtos,
        //            TotalCount = totalCount
        //        };

        //        response.Success = true;
        //        response.Data = pageResult;
        //        response.Message = "Decor services retrieved successfully.";
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Success = false;
        //        response.Message = "Error retrieving decor services.";
        //        response.Errors.Add(ex.Message);
        //    }
        //    return response;
        //}
    }
}
