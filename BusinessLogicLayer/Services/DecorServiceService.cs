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

        public async Task<DecorServiceResponse> GetDecorServiceByIdAsync(int id)
        {
            var response = new DecorServiceResponse();
            try
            {
                var decorService = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == id)
                    .Include(ds => ds.DecorCategory)
                    .Include(ds => ds.DecorImages)
                    .Include(ds => ds.DecorServiceSeasons)
                        .ThenInclude(dss => dss.Season)

                    .Include(ds => ds.Account)
                        .ThenInclude(a => a.Followers)
                    .Include(ds => ds.Account)
                        .ThenInclude(a => a.Followings)

                    .FirstOrDefaultAsync();

                if (decorService == null)
                {
                    response.Success = false;
                    response.Message = "Decor service not found.";
                }
                else
                {
                    // Map các trường cơ bản của DecorService sang DecorServiceDTO
                    var dto = _mapper.Map<DecorServiceDTO>(decorService);

                    //Hiện Category Name
                    dto.CategoryName = decorService.DecorCategory.CategoryName;

                    // Hiện số lượng yêu thích
                    var favoriteCount = await _unitOfWork.FavoriteServiceRepository
                        .Query(f => f.DecorServiceId == id)
                        .CountAsync();

                    dto.FavoriteCount = favoriteCount;

                    // Thay vì ImageUrls = [...], ta map sang List<DecorImageDTO>
                    dto.Images = decorService.DecorImages
                        .Select(img => new DecorImageResponse
                        {
                            Id = img.Id,
                            ImageURL = img.ImageURL
                        })
                        .ToList();

                    dto.Seasons = decorService.DecorServiceSeasons
                        .Select(dss => new SeasonResponse
                        {
                            Id = dss.Season.Id,
                            SeasonName = dss.Season.SeasonName
                        })
                        .ToList();

                    dto.Provider = new ProviderResponse
                    {
                        Id = decorService.Account.Id,
                        BusinessName = decorService.Account.BusinessName,
                        Avatar = decorService.Account.Avatar,
                        JoinedDate = decorService.Account.JoinedDate.ToString("dd/MM/yyyy"),
                        FollowersCount = decorService.Account.Followers?.Count ?? 0,
                        FollowingsCount = decorService.Account.Followings?.Count ?? 0
                    };

                    response.Success = true;
                    response.Data = dto;
                    response.Message = "Decor service retrieved successfully.";
                }
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
                    .Query(ds => !ds.IsDeleted)
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

        public async Task<DecorServiceResponse> GetDecorServiceBySlugAsync(string slug)
        {
            var response = new DecorServiceResponse();
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

                // Then get the decor service for this account
                var decorService = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.AccountId == account.Id && !ds.IsDeleted)
                    .Include(ds => ds.DecorCategory)
                    .Include(ds => ds.DecorImages)
                    .Include(ds => ds.DecorServiceSeasons)
                        .ThenInclude(dss => dss.Season)

                    .Include(ds => ds.Account)
                        .ThenInclude(a => a.Followers)
                    .Include(ds => ds.Account)
                        .ThenInclude(a => a.Followings)

                    .FirstOrDefaultAsync();

                if (decorService == null)
                {
                    response.Success = false;
                    response.Message = "Decor service not found for this provider.";
                    return response;
                }

                // Map to DTO
                var dto = _mapper.Map<DecorServiceDTO>(decorService);

                //Get CategiryName
                dto.CategoryName = decorService.DecorCategory.CategoryName;

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

                // Map Provider
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

        public async Task<BaseResponse<PageResult<DecorServiceDTO>>> GetFilterDecorServicesAsync(DecorServiceFilterRequest request)
        {
            var response = new BaseResponse<PageResult<DecorServiceDTO>>();
            try
            {
                // Filter
                Expression<Func<DecorService, bool>> filter = decorService =>
                    decorService.IsDeleted == false &&
                    (string.IsNullOrEmpty(request.Style) || decorService.Style.Contains(request.Style)) &&
                    (string.IsNullOrEmpty(request.Province) || decorService.Province.Contains(request.Province)) &&
                    (!request.MinPrice.HasValue || decorService.BasePrice >= request.MinPrice.Value) &&
                    (!request.MaxPrice.HasValue || decorService.BasePrice <= request.MaxPrice.Value) &&
                    (!request.DecorCategoryId.HasValue || decorService.DecorCategoryId == request.DecorCategoryId.Value);

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
                    "Province" => decorService => decorService.Province,
                    "CreateAt" => decorService => decorService.CreateAt,
                    "Favorite" => decorService => decorService.FavoriteServices.Count,
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

                // Map mỗi service sang DecorServiceDTO
                var dtos = _mapper.Map<List<DecorServiceDTO>>(decorServices);

                // Map DecorImages -> DecorImageDTO
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

        public async Task<BaseResponse> CreateDecorServiceAsync(CreateDecorServiceRequest request, int accountId)
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
                    Province = request.Province,
                    AccountId = accountId,
                    DecorCategoryId = request.DecorCategoryId,
                    CreateAt = DateTime.Now,
                    DecorImages = new List<DecorImage>(),
                    DecorServiceSeasons = new List<DecorServiceSeason>()
                };

                // Thêm mùa vào dịch vụ
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

                //// *** Index lên Elasticsearch (không throw exception nếu lỗi)
                //try
                //{
                //    await _elasticClientService.IndexDecorServiceAsync(decorService);
                //}
                //catch (Exception ex)
                //{
                //    // Log lỗi (nếu cần) nhưng không trả về lỗi cho client
                //    // Ví dụ: _logger.LogError(ex, "Elastic index error in CreateDecorServiceAsync");
                //}

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
                decorService.Province = request.Province;
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
                var query = _unitOfWork.DecorServiceRepository.Query(ds => !ds.IsDeleted);

                if (!string.IsNullOrEmpty(request.Style))
                    query = query.Where(ds => ds.Style.Contains(request.Style));

                if (!string.IsNullOrEmpty(request.Province))
                    query = query.Where(ds => ds.Province.Contains(request.Province));

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
                    Province = ds.Province,
                    CreateAt = ds.CreateAt,
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
    }
}
