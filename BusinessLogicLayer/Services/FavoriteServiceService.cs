using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Repository.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using static BusinessLogicLayer.ModelResponse.DecorServiceReviewResponse;

namespace BusinessLogicLayer.Services
{
    public class FavoriteServiceService : IFavoriteServiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public FavoriteServiceService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponse<List<FavoriteServiceResponse>>> GetFavoriteServicesAsync(int accountId)
        {
            var response = new BaseResponse<List<FavoriteServiceResponse>>();
            try
            {
                var favorites = await _unitOfWork.FavoriteServiceRepository
                    .Query(f => f.AccountId == accountId)
                    .Include(f => f.DecorService)
                        .ThenInclude(ds => ds.DecorImages)
                    .Include(f => f.DecorService)
                        .ThenInclude(ds => ds.DecorCategory)
                    .Include(f => f.DecorService)
                        .ThenInclude(ds => ds.DecorServiceSeasons)
                            .ThenInclude(dss => dss.Season)
                    .Include(f => f.DecorService)
                        .ThenInclude(ds => ds.Account)
                            .ThenInclude(a => a.Followers)
                    .Include(f => f.DecorService)
                        .ThenInclude(ds => ds.Account)
                            .ThenInclude(a => a.Followings)
                    .Include(f => f.DecorService)
                        .ThenInclude(ds => ds.Reviews)
                            .ThenInclude(r => r.ReviewImages)
                    .Include(f => f.DecorService)
                        .ThenInclude(ds => ds.Reviews)
                            .ThenInclude(r => r.Account)
                    .ToListAsync();

                var result = new List<FavoriteServiceResponse>();

                foreach (var favorite in favorites)
                {
                    var ds = favorite.DecorService;
                    var dto = _mapper.Map<DecorServiceById>(ds);

                    dto.CategoryName = ds.DecorCategory?.CategoryName;

                    dto.FavoriteCount = await _unitOfWork.FavoriteServiceRepository
                        .Query(f => f.DecorServiceId == ds.Id)
                        .CountAsync();

                    dto.Images = ds.DecorImages?
                        .Select(img => new DecorImageResponse
                        {
                            Id = img.Id,
                            ImageURL = img.ImageURL
                        })
                        .ToList();

                    dto.Seasons = ds.DecorServiceSeasons?
                        .Select(dss => new SeasonResponse
                        {
                            Id = dss.Season.Id,
                            SeasonName = dss.Season.SeasonName
                        })
                        .ToList();

                    dto.Provider = new ProviderResponse
                    {
                        Id = ds.Account.Id,
                        BusinessName = ds.Account.BusinessName,
                        Avatar = ds.Account.Avatar,
                        Slug = ds.Account.Slug,
                        JoinedDate = ds.Account.JoinedDate.ToString("dd/MM/yyyy"),
                        FollowersCount = ds.Account.Followers?.Count ?? 0,
                        FollowingsCount = ds.Account.Followings?.Count ?? 0
                    };

                    dto.Reviews = ds.Reviews?
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

                    result.Add(new FavoriteServiceResponse
                    {
                        FavoriteId = favorite.Id,
                        DecorServiceDetails = dto
                    });
                }

                response.Success = true;
                response.Data = result;
                response.Message = "Favorite services retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving favorite services.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }


        public async Task<BaseResponse> AddToFavoritesAsync(int accountId, int decorServiceId)
        {
            var response = new BaseResponse();
            try
            {
                var decorService = await _unitOfWork.DecorServiceRepository.Query(d => d.Id == decorServiceId).FirstOrDefaultAsync();
                if (decorService == null)
                {
                    response.Success = false;
                    response.Message = "Decor service not found.";
                    return response;
                }

                if (decorService.AccountId == accountId)
                {
                    response.Success = false;
                    response.Message = "Cannot favorite your own decor service.";
                    return response;
                }

                var existingFavorite = await _unitOfWork.FavoriteServiceRepository
                    .Query(f => f.AccountId == accountId && f.DecorServiceId == decorServiceId)
                    .FirstOrDefaultAsync();

                if (existingFavorite != null)
                {
                    response.Success = false;
                    response.Message = "Service is already in favorites.";
                    return response;
                }

                var favorite = new FavoriteService
                {
                    AccountId = accountId,
                    DecorServiceId = decorServiceId
                };

                await _unitOfWork.FavoriteServiceRepository.InsertAsync(favorite);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Added to favorites.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error adding to favorites.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> RemoveFromFavoritesAsync(int accountId, int decorServiceId)
        {
            var response = new BaseResponse();
            try
            {
                var favorite = await _unitOfWork.FavoriteServiceRepository
                    .Query(f => f.AccountId == accountId && f.DecorServiceId == decorServiceId)
                    .FirstOrDefaultAsync();

                if (favorite == null)
                {
                    response.Success = false;
                    response.Message = "Service not found in favorites.";
                    return response;
                }

                _unitOfWork.FavoriteServiceRepository.Delete(favorite.Id);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Removed from favorites.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error removing from favorites.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }
    }
} 