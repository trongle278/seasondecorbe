using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Repository.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

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
                // Cải thiện query để đảm bảo lấy đầy đủ thông tin Images và Seasons
                var favorites = await _unitOfWork.FavoriteServiceRepository
                    .Query(f => f.AccountId == accountId)
                    .Include(f => f.DecorService)
                        .ThenInclude(ds => ds.DecorImages) // Include images
                    .Include(f => f.DecorService.DecorServiceSeasons) // Include mối quan hệ với Seasons
                        .ThenInclude(dss => dss.Season)
                    .ToListAsync();

                // Tạo danh sách FavoriteServiceResponse với đầy đủ thông tin
                var result = new List<FavoriteServiceResponse>();

                foreach (var favorite in favorites)
                {
                    // Map DecorService sang DecorServiceDTO
                    var decorServiceDTO = _mapper.Map<DecorServiceDTO>(favorite.DecorService);

                    // Tính số favorite cho DecorService này
                    decorServiceDTO.FavoriteCount = await _unitOfWork.FavoriteServiceRepository
                           .Query(f => f.DecorServiceId == favorite.DecorService.Id)
                           .CountAsync();

                    // Map thủ công các collection nếu AutoMapper không hoạt động đúng
                    if (favorite.DecorService.DecorImages != null)
                    {
                        decorServiceDTO.Images = favorite.DecorService.DecorImages.Select(img => new DecorImageResponse
                        {
                            Id = img.Id,
                            ImageURL = img.ImageURL
                        }).ToList();
                    }

                    if (favorite.DecorService.DecorServiceSeasons != null)
                    {
                        decorServiceDTO.Seasons = favorite.DecorService.DecorServiceSeasons
                            .Where(dss => dss.Season != null)
                            .Select(dss => new SeasonResponse
                            {
                                Id = dss.Season.Id,
                                SeasonName = dss.Season.SeasonName
                            }).ToList();
                    }

                    // Tạo và thêm FavoriteServiceResponse vào kết quả
                    result.Add(new FavoriteServiceResponse
                    {
                        FavoriteId = favorite.Id,
                        DecorServiceDetails = decorServiceDTO
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