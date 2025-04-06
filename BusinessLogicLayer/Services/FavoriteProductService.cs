using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Favorite;
using BusinessLogicLayer.ModelResponse.Product;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class FavoriteProductService : IFavoriteProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public FavoriteProductService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponse<List<FavoriteProductResponse>>> GetFavoriteProduct(int accountId)
        {
            var response = new BaseResponse<List<FavoriteProductResponse>>();
            try
            {
                var favorites = await _unitOfWork.FavoriteProductRepository
                                                .Query(f => f.AccountId == accountId)
                                                    .Include(f => f.Product)
                                                        .ThenInclude(ds => ds.ProductImages)
                                                .ToListAsync();

                var result = new List<FavoriteProductResponse>();

                foreach (var favorite in favorites)
                {
                    var product = _mapper.Map<ProductDetailResponse>(favorite.Product);

                    product.FavoriteCount = await _unitOfWork.FavoriteProductRepository
                           .Query(f => f.ProductId == favorite.Product.Id)
                           .CountAsync();

                    if (favorite.Product.ProductImages != null)
                    {
                        product.ImageUrls = favorite.Product.ProductImages.Select(img => img.ImageUrl).ToList();
                    }

                    result.Add(new FavoriteProductResponse
                    {
                        id = favorite.Id,
                        ProductDetail = product
                    });
                }

                response.Success = true;
                response.Data = result;
                response.Message = "Favorite products retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving favorite products.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> AddToFavorite(int accountId, int productId)
        {
            var response = new BaseResponse();
            try
            {
                var product = await _unitOfWork.ProductRepository.Query(d => d.Id == productId).FirstOrDefaultAsync();
                if (product == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product";
                    return response;
                }

                if (product.AccountId == accountId)
                {
                    response.Success = false;
                    response.Message = "Cannot favorite your own product.";
                    return response;
                }

                var existingFavorite = await _unitOfWork.FavoriteProductRepository
                    .Query(f => f.AccountId == accountId && f.ProductId == productId)
                    .FirstOrDefaultAsync();

                if (existingFavorite != null)
                {
                    response.Success = false;
                    response.Message = "Product is already in favorite list.";
                    return response;
                }

                var favorite = new FavoriteProduct
                {
                    AccountId = accountId,
                    ProductId = productId
                };

                await _unitOfWork.FavoriteProductRepository.InsertAsync(favorite);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Added to favorite list";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error adding to favorite list";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> RemoveFromFavorite(int accountId, int productId)
        {
            var response = new BaseResponse();
            try
            {
                var favorite = await _unitOfWork.FavoriteProductRepository
                    .Query(f => f.AccountId == accountId && f.ProductId == productId)
                    .FirstOrDefaultAsync();

                if (favorite == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product";
                    return response;
                }

                _unitOfWork.FavoriteProductRepository.Delete(favorite.Id);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Removed from favorite list";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error removing from favorite list";
                response.Errors.Add(ex.Message);
            }
            return response;
        }
    }
}
