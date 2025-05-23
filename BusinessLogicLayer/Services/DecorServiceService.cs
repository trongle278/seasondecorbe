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
using BusinessLogicLayer.ModelResponse.Cart;
using Microsoft.Identity.Client;
using BusinessLogicLayer.Utilities.DataMapping;

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
                         .Include(ds => ds.DecorServiceStyles)
                            .ThenInclude(dss => dss.DecorationStyle)
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

                    dtos[i].Designs = service.DecorServiceStyles
                        .Select(dss => new DesignResponse
                        {
                            Id = dss.DecorationStyle.Id,
                            Name = dss.DecorationStyle.Name
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

                if (!string.IsNullOrEmpty(request.DesignName))
                {
                    string keyword = request.DesignName.ToLower();
                    query = query.Where(ds =>
                        ds.DecorServiceStyles.Any(style =>
                            style.DecorationStyle.Name.ToLower().Contains(keyword)));
                }

                var decorServices = await query
                    .Include(ds => ds.DecorCategory)
                    .Include(ds => ds.DecorImages)
                    .Include(ds => ds.DecorServiceSeasons)
                        .ThenInclude(dss => dss.Season)
                    .Include(ds => ds.DecorServiceStyles)
                        .ThenInclude(dss => dss.DecorationStyle) // ✅ Bắt buộc để dùng Name
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
                    }).ToList(),

                    Designs = ds.DecorServiceStyles.Select(dss => new DesignResponse
                    {
                        Id = dss.DecorationStyle.Id,
                        Name = dss.DecorationStyle.Name
                    }).ToList(),

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

                var designs = service.DecorServiceStyles?
                    .Select(s => new DesignResponse
                    {
                        Id = s.DecorationStyle.Id,
                        Name = s.DecorationStyle.Name
                    })
                    .ToList();

                response.Success = true;
                response.Data = designs;
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

                var designs = service.DecorServiceStyles?
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
                    Designs = designs
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
                    Message = "Offerings and designs retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<OfferingAndDesignResponse>
                {
                    Success = false,
                    Message = "Failed to retrieve offerings and designs",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse<ServiceRelatedProductPageResult>> GetRelatedProductsAsync(ServiceRelatedProductRequest request)
        {
            var response = new BaseResponse<ServiceRelatedProductPageResult>();
            try
            {
                var decorService = await _unitOfWork.DecorServiceRepository.Queryable()
                    .Include(ds => ds.DecorCategory)
                    .Include(ds => ds.Account)
                    .Include(ds => ds.DecorServiceSeasons)
                        .ThenInclude(dss => dss.Season)
                    .FirstOrDefaultAsync(ds => ds.Id == request.ServiceId);

                if (decorService == null)
                {
                    response.Message = "Decor service not found!";
                    return response;
                }

                var provider = decorService.Account;
                var providerDecorCategory = decorService.DecorCategory?.CategoryName;
                var seasons = decorService.DecorServiceSeasons
                    .Select(dss => dss.Season.SeasonName)
                    .Distinct()
                    .ToList();

                if (string.IsNullOrEmpty(providerDecorCategory))
                {
                    response.Message = "Decor category is missing!";
                    return response;
                }

                var decorServiceSeasonIds = decorService.DecorServiceSeasons
                    .Select(dss => dss.SeasonId)
                    .ToList();

                // Map decor category -> allowed product categories
                var allowedProductCategories = DecorCategoryMapping.DecorToProductCategoryMap.TryGetValue(
                    providerDecorCategory, out var relatedCategories)
                    ? relatedCategories.Distinct().ToList()
                    : new List<string>();

                // Build product filter
                Expression<Func<Product, bool>> filter = p =>
                    p.AccountId == provider.Id &&
                    allowedProductCategories.Contains(p.Category.CategoryName) &&
                    p.ProductSeasons.Any(ps => decorServiceSeasonIds.Contains(ps.SeasonId)) &&
                    (string.IsNullOrEmpty(request.Category) || p.Category.CategoryName == request.Category);

                // Check account permission
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(request.UserId);

                if ((account != null) && (account.RoleId != 1) && !(account?.RoleId == 2 && account.ProviderVerified == true))
                {
                    // Only show in-stock for regular or unverified users
                    filter = filter.And(p => p.Quantity > 0);
                }

                // Sorting
                Expression<Func<Product, object>> orderBy = request.SortBy switch
                {
                    "ProductName" => p => p.ProductName,
                    "ProductPrice" => p => p.ProductPrice,
                    "CreateAt" => p => p.CreateAt,
                    _ => p => p.Id
                };

                Func<IQueryable<Product>, IQueryable<Product>> customQuery = query =>
                    query.Include(p => p.ProductImages)
                         .Include(p => p.Category)
                         .Include(p => p.ProductSeasons)
                            .ThenInclude(ps => ps.Season);

                var (products, totalCount) = await _unitOfWork.ProductRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderBy,
                    request.Descending,
                    null,
                    customQuery
                );

                var relatedProducts = new List<ServiceRelatedProductResponse>();

                foreach (var product in products)
                {
                    var orderDetails = await _unitOfWork.OrderDetailRepository.Queryable()
                        .Where(od => od.ProductId == product.Id && od.Order.Status == Order.OrderStatus.Paid)
                        .Include(od => od.Order)
                            .ThenInclude(o => o.Reviews)
                        .ToListAsync();

                    var reviews = orderDetails.SelectMany(od => od.Order.Reviews).ToList();
                    var averageRate = reviews.Any() ? reviews.Average(r => r.Rate) : 0;
                    var totalSold = orderDetails.Sum(od => od.Quantity);

                    relatedProducts.Add(new ServiceRelatedProductResponse
                    {
                        Id = product.Id,
                        ProductName = product.ProductName,
                        Description = product.Description,
                        ProductPrice = product.ProductPrice,
                        Rate = averageRate,
                        TotalSold = totalSold,
                        Quantity = product.Quantity,
                        Status = product.Quantity > 0
                            ? Product.ProductStatus.InStock.ToString()
                            : Product.ProductStatus.OutOfStock.ToString(),
                        ImageUrls = product.ProductImages?.Select(img => img.ImageUrl).ToList() ?? new List<string>(),
                        Category = product.Category.CategoryName,
                        Seasons = product.ProductSeasons?
                             .Select(ps => ps.Season.SeasonName)
                             .Distinct()
                             .ToList() ?? new List<string>()
                    });
                }

                response.Success = true;
                response.Message = "Related products retrieved successfully.";
                response.Data = new ServiceRelatedProductPageResult
                {
                    Category = providerDecorCategory,
                    Data = relatedProducts,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving related products!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> AddRelatedProductAsync(int serviceId, int accountId, int productId, int quantity)
        {
            var response = new BaseResponse();
            try
            {
                var relatedProduct = await _unitOfWork.RelatedProductRepository.Queryable()
                                                    .Include(rp => rp.RelatedProductItems)
                                                    .Where(rp => rp.ServiceId == serviceId && rp.AccountId == accountId)
                                                    .FirstOrDefaultAsync();

                if (relatedProduct == null)
                {
                    // Create related product holder
                    relatedProduct = new RelatedProduct
                    {
                        ServiceId = serviceId,
                        AccountId = accountId,
                        RelatedProductItems = new List<RelatedProductItem>()
                    };

                    await _unitOfWork.RelatedProductRepository.InsertAsync(relatedProduct);
                    await _unitOfWork.CommitAsync();
                }

                var product = await _unitOfWork.ProductRepository.Queryable()
                                                    .Include(p => p.ProductImages)
                                                    .Where(p => p.Id == productId)
                                                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    response.Message = "Product not found!";
                    return response;
                }

                // Check existing product quantity
                if (product.Quantity < quantity)
                {
                    response.Success = false;
                    response.Message = "Not enough existing product";
                    return response;
                }

                if (product.Quantity < 0)
                {
                    response.Success = false;
                    response.Message = "Product quantity has to be > 0";
                    return response;
                }

                decimal unitPrice = product.ProductPrice;

                var item = relatedProduct.RelatedProductItems
                        .Where(ri => ri.ProductId == productId)
                        .FirstOrDefault();

                if (item == null)
                {
                    // Add item to related product holder
                    item = new RelatedProductItem
                    {
                        ProductId = productId,
                        ProductName = product.ProductName,
                        Quantity = quantity,
                        UnitPrice = unitPrice * quantity,
                        Image = product.ProductImages?.FirstOrDefault()?.ImageUrl
                    };

                    relatedProduct.RelatedProductItems.Add(item);
                    await _unitOfWork.RelatedProductItemRepository.InsertAsync(item);
                }
                else
                {
                    if (product.Quantity < item.Quantity + quantity)
                    {
                        response.Message = "Not enough existing product";
                        return response;
                    }

                    item.Quantity += quantity;
                    item.UnitPrice = item.Quantity * unitPrice;
                    _unitOfWork.RelatedProductItemRepository.Update(item);
                }

                // Update related product holder
                relatedProduct.TotalItem = relatedProduct.RelatedProductItems.Sum(i => i.Quantity);
                relatedProduct.TotalPrice = relatedProduct.RelatedProductItems.Sum(i => i.UnitPrice);

                _unitOfWork.RelatedProductRepository.Update(relatedProduct);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product added to related product successfully.";
                response.Data = relatedProduct;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error while adding related product";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> UpdateQuantityAsync(int relatedProductId, int productId, int quantity)
        {
            var response = new BaseResponse();
            try
            {
                var relatedProduct = await _unitOfWork.RelatedProductRepository.Queryable()
                                            .Where(r => r.Id == relatedProductId)
                                            .FirstOrDefaultAsync();

                if (relatedProduct == null)
                {
                    response.Message = "Related product holder not found!";
                    return response;
                }

                var item = await _unitOfWork.RelatedProductItemRepository.Queryable()
                                            .Where(rp => rp.RelatedProductId == relatedProductId && rp.ProductId == productId)
                                            .FirstOrDefaultAsync();

                if (item == null || quantity <= 0)
                {
                    response.Message = "Product not found!";
                    return response;
                }

                var product = await _unitOfWork.ProductRepository.GetByIdAsync(productId);

                if (product == null)
                {
                    response.Message = "Product not found!";
                    return response;
                }

                // Check existing product before update item
                if (product.Quantity < quantity)
                {
                    response.Message = "Not enough existing product";
                    return response;
                }

                if (product.Quantity < 0)
                {
                    response.Message = "Product quantity cannot be negative";
                    return response;
                }

                decimal unitPrice = product.ProductPrice;

                // Save old item value before update
                int oldQuantity = item.Quantity;
                decimal oldUnitPrice = item.UnitPrice;

                // Update item
                item.Quantity = quantity;
                item.UnitPrice = quantity * product.ProductPrice;

                // Update holder using old value
                relatedProduct.TotalItem += quantity - oldQuantity;
                relatedProduct.TotalPrice += (item.UnitPrice - oldUnitPrice);

                _unitOfWork.RelatedProductItemRepository.Update(item);

                _unitOfWork.RelatedProductRepository.Update(relatedProduct);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product in related product holder updated successfully.";
                response.Data = relatedProduct;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating product in related product holder!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> RemoveRelatedProductAsync(int relatedProductId, int productId)
        {
            var response = new BaseResponse();
            try
            {
                var relatedProduct = await _unitOfWork.RelatedProductRepository.Queryable()
                                            .Where(rp => rp.Id == relatedProductId)
                                            .FirstOrDefaultAsync();

                if (relatedProduct == null)
                {
                    response.Message = "Related product holder not found!";
                    return response;
                }

                var item = await _unitOfWork.RelatedProductItemRepository.Queryable()
                                            .Where(rp => rp.RelatedProductId == relatedProductId && rp.ProductId == productId)
                                            .FirstOrDefaultAsync();

                if (item == null)
                {
                    response.Message = "Product not found!";
                    return response;
                }

                // Update holder
                relatedProduct.TotalItem -= item.Quantity;
                relatedProduct.TotalPrice -= item.UnitPrice;

                _unitOfWork.RelatedProductItemRepository.Delete(item.Id);
                _unitOfWork.RelatedProductRepository.Update(relatedProduct);

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product in related product holder removed successfully.";
                response.Data = relatedProduct;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error removing product in related product holder!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> GetAddedProductServiceAsync(int serviceId, int accountId)
        {
            var response = new BaseResponse();
            try
            {
                var relatedProduct = await _unitOfWork.RelatedProductRepository.Queryable()
                    .Where(rp => rp.ServiceId == serviceId && rp.AccountId == accountId)
                    .FirstOrDefaultAsync();

                if (relatedProduct == null)
                {
                    response.Success = true;
                    response.Message = "No related product was added.";
                    response.Data = new List<RelatedProductItemResponse>();
                    return response;
                }

                var relatedProductItem = await _unitOfWork.RelatedProductItemRepository.Queryable()
                    .Where(rpi => rpi.RelatedProductId == relatedProduct.Id)
                    .ToListAsync();

                response.Success = true;
                response.Message = "Added related product retrieved successfully.";
                response.Data = _mapper.Map<List<RelatedProductItemResponse>>(relatedProductItem);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Added related product holder retrieving cart";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
