using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Repository.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogicLayer.Services
{
    public class FavoriteServiceService : IFavoriteServiceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FavoriteServiceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<List<FavoriteServiceResponse>>> GetFavoriteServicesAsync(int accountId)
        {
            var response = new BaseResponse<List<FavoriteServiceResponse>>();
            try
            {
                var favorites = await _unitOfWork.FavoriteServiceRepository
                    .Query(f => f.AccountId == accountId)
                    .Include(f => f.DecorService)
                    .Select(f => new FavoriteServiceResponse
                    {
                        Id = f.Id,
                        AccountId = f.AccountId,
                        DecorServiceId = f.DecorServiceId,
                        DecorServiceName = f.DecorService.Style
                    })
                    .ToListAsync();

                response.Success = true;
                response.Data = favorites;
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

                _unitOfWork.FavoriteServiceRepository.Delete(favorite);
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