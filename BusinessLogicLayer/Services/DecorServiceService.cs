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

        public DecorServiceService(IUnitOfWork unitOfWork, IMapper mapper, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
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
            try
            {
                // Kiểm tra số lượng ảnh không vượt quá 5
                if (request.Images != null && request.Images.Count > 5)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Maximum 5 images are allowed."
                    };
                }

                // Tạo entity DecorService mới
                var decorService = new DecorService
                {
                    Style = request.Style,
                    //BasePrice = request.BasePrice,
                    Description = request.Description,
                    Province = request.Province,
                    AccountId = accountId,
                    DecorCategoryId = request.DecorCategoryId,
                    CreateAt = DateTime.UtcNow,
                    DecorImages = new List<DecorImage>()
                };

                // Nếu có ảnh, upload và thêm vào danh sách
                if (request.Images != null && request.Images.Any())
                {
                    foreach (var imageFile in request.Images)
                    {
                        string imageUrl;
                        using (var stream = imageFile.OpenReadStream())
                        {
                            imageUrl = await _cloudinaryService.UploadFileAsync(stream, imageFile.FileName, imageFile.ContentType);
                        }
                        decorService.DecorImages.Add(new DecorImage { ImageURL = imageUrl });
                    }
                }

                await _unitOfWork.DecorServiceRepository.InsertAsync(decorService);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Decor service created successfully."
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error creating decor service.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> UpdateDecorServiceAsync(int id, UpdateDecorServiceRequest request, int accountId)
        {
            try
            {
                var decorService = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == id)
                    .FirstOrDefaultAsync();

                if (decorService == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Decor service not found."
                    };
                }

                decorService.Style = request.Style;
                //decorService.BasePrice = request.BasePrice;
                decorService.Description = request.Description;
                decorService.Province = request.Province;
                decorService.AccountId = accountId;
                decorService.DecorCategoryId = request.DecorCategoryId;

                _unitOfWork.DecorServiceRepository.Update(decorService);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Decor service updated successfully."
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error updating decor service.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> DeleteDecorServiceAsync(int id)
        {
            try
            {
                var decorService = await _unitOfWork.DecorServiceRepository
                    .Query(ds => ds.Id == id)
                    .FirstOrDefaultAsync();

                if (decorService == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Decor service not found."
                    };
                }

                _unitOfWork.DecorServiceRepository.Delete(decorService);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Decor service deleted successfully."
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error deleting decor service.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
