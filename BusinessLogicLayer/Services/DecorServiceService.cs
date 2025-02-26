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

namespace BusinessLogicLayer.Services
{
    public class DecorServiceService : IDecorServiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IElasticClientService _elasticClientService;

        public DecorServiceService(IUnitOfWork unitOfWork, IMapper mapper, ICloudinaryService cloudinaryService, IElasticClientService elasticClientService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
            _elasticClientService = elasticClientService;
        }

        public async Task<DecorServiceResponse> GetDecorServiceByIdAsync(int id)
        {
            try
            {
                var decorService = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == id)
                    .Include(ds => ds.DecorCategory)
                    .Include(ds => ds.DecorImages)
                    .FirstOrDefaultAsync();

                if (decorService == null)
                {
                    return new DecorServiceResponse
                    {
                        Success = false,
                        Message = "Decor service not found."
                    };
                }

                var dto = _mapper.Map<DecorServiceDTO>(decorService);
                dto.ImageUrls = decorService.DecorImages.Select(di => di.ImageURL).ToList();

                return new DecorServiceResponse
                {
                    Success = true,
                    Data = dto
                };
            }
            catch (Exception ex)
            {
                return new DecorServiceResponse
                {
                    Success = false,
                    Message = "Error retrieving decor service.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<DecorServiceListResponse> GetAllDecorServicesAsync()
        {
            try
            {
                var services = await _unitOfWork.DecorServiceRepository
                    .Query(ds => true)
                    .Include(ds => ds.DecorCategory)
                    .Include(ds => ds.DecorImages)
                    .ToListAsync();

                var dtos = _mapper.Map<List<DecorServiceDTO>>(services);
                for (int i = 0; i < services.Count; i++)
                {
                    dtos[i].ImageUrls = services[i].DecorImages.Select(di => di.ImageURL).ToList();
                }

                return new DecorServiceListResponse
                {
                    Success = true,
                    Data = dtos
                };
            }
            catch (Exception ex)
            {
                return new DecorServiceListResponse
                {
                    Success = false,
                    Message = "Error retrieving decor services.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> CreateDecorServiceAsync(CreateDecorServiceRequest request, int accountId)
        {
            var response = new BaseResponse();
            try
            {
                // Kiểm tra số lượng ảnh
                if (request.Images != null && request.Images.Count > 5)
                {
                    response.Success = false;
                    response.Message = "Maximum 5 images are allowed.";
                    return response;
                }

                // Tạo DecorService entity
                var decorService = new DecorService
                {
                    Style = request.Style,
                    Description = request.Description,
                    Province = request.Province,
                    AccountId = accountId,
                    DecorCategoryId = request.DecorCategoryId,
                    CreateAt = DateTime.UtcNow,
                    DecorImages = new List<DecorImage>()
                };

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

                // Lưu DB
                await _unitOfWork.DecorServiceRepository.InsertAsync(decorService);
                await _unitOfWork.CommitAsync();

                // *** Index lên Elasticsearch
                await _elasticClientService.IndexDecorServiceAsync(decorService);

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
                var decorService = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == id)
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

                _unitOfWork.DecorServiceRepository.Update(decorService);
                await _unitOfWork.CommitAsync();

                // *** Cập nhật index trên Elasticsearch
                await _elasticClientService.IndexDecorServiceAsync(decorService);

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

        public async Task<BaseResponse> DeleteDecorServiceAsync(int id)
        {
            var response = new BaseResponse();
            try
            {
                var decorService = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == id)
                    .FirstOrDefaultAsync();

                if (decorService == null)
                {
                    response.Success = false;
                    response.Message = "Decor service not found.";
                    return response;
                }

                _unitOfWork.DecorServiceRepository.Delete(decorService);
                await _unitOfWork.CommitAsync();

                // *** Xoá luôn trên Elasticsearch
                await _elasticClientService.DeleteDecorServiceAsync(id);

                response.Success = true;
                response.Message = "Decor service deleted successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error deleting decor service.";
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
    }
}
