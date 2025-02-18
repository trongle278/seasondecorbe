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
    public class DecorCategoryService : IDecorCategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DecorCategoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<DecorCategoryListResponse> GetAllDecorCategoriesAsync()
        {
            try
            {
                var categories = await _unitOfWork.DecorCategoryRepository
                    .Query(x => true)
                    .ToListAsync();

                var categoriesDTO = _mapper.Map<List<DecorCategoryDTO>>(categories);

                return new DecorCategoryListResponse
                {
                    Success = true,
                    Data = categoriesDTO
                };
            }
            catch (Exception ex)
            {
                return new DecorCategoryListResponse
                {
                    Success = false,
                    Message = "Error retrieving decoration categories",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<DecorCategoryResponse> GetDecorCategoryByIdAsync(int categoryId)
        {
            try
            {
                var category = await _unitOfWork.DecorCategoryRepository
                    .Query(x => x.Id == categoryId)
                    .FirstOrDefaultAsync();

                if (category == null)
                {
                    return new DecorCategoryResponse
                    {
                        Success = false,
                        Message = "Decoration category not found"
                    };
                }

                var categoryDTO = _mapper.Map<DecorCategoryDTO>(category);

                return new DecorCategoryResponse
                {
                    Success = true,
                    Data = categoryDTO
                };
            }
            catch (Exception ex)
            {
                return new DecorCategoryResponse
                {
                    Success = false,
                    Message = "Error retrieving decoration category",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> CreateDecorCategoryAsync(DecorCategoryRequest request)
        {
            try
            {
                var existingCategory = await _unitOfWork.DecorCategoryRepository
                    .Query(x => x.CategoryName.ToLower() == request.CategoryName.ToLower())
                    .FirstOrDefaultAsync();

                if (existingCategory != null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Category name already exists",
                        Errors = new List<string> { "A category with this name already exists" }
                    };
                }

                var category = _mapper.Map<DecorCategory>(request);
                await _unitOfWork.DecorCategoryRepository.InsertAsync(category);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Decoration category created successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error creating decoration category",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> UpdateDecorCategoryAsync(int categoryId, DecorCategoryRequest request)
        {
            try
            {
                var category = await _unitOfWork.DecorCategoryRepository
                    .Query(x => x.Id == categoryId)
                    .FirstOrDefaultAsync();

                if (category == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Category not found",
                        Errors = new List<string> { "Category not found" }
                    };
                }

                // Check for duplicate name but exclude current category
                var existingCategory = await _unitOfWork.DecorCategoryRepository
                    .Query(x => x.CategoryName.ToLower() == request.CategoryName.ToLower()
                               && x.Id != categoryId)
                    .FirstOrDefaultAsync();

                if (existingCategory != null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Category name already exists",
                        Errors = new List<string> { "A category with this name already exists" }
                    };
                }

                category.CategoryName = request.CategoryName.Trim();
                category.Description = request.Description?.Trim();

                _unitOfWork.DecorCategoryRepository.Update(category);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Category updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error updating category",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> DeleteDecorCategoryAsync(int categoryId)
        {
            try
            {
                var category = await _unitOfWork.DecorCategoryRepository
                    .Query(x => x.Id == categoryId)
                    .Include(x => x.DecorServices) // Check for related services
                    .FirstOrDefaultAsync();

                if (category == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Category not found",
                        Errors = new List<string> { "Category not found" }
                    };
                }

                // Check if category has related services
                if (category.DecorServices?.Any() == true)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Cannot delete category with existing services",
                        Errors = new List<string> { "This category has associated services and cannot be deleted" }
                    };
                }

                _unitOfWork.DecorCategoryRepository.Delete(category);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Category deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error deleting category",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
