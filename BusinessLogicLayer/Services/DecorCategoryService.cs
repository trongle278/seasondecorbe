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
            var response = new DecorCategoryListResponse();
            try
            {
                var categories = await _unitOfWork.DecorCategoryRepository
                    .Query(x => true)
                    .ToListAsync();
                response.Data = _mapper.Map<List<DecorCategoryDTO>>(categories);
                response.Success = true;
                response.Message = "Decoration categories retrieved successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving decoration categories";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<DecorCategoryResponse> GetDecorCategoryByIdAsync(int categoryId)
        {
            var response = new DecorCategoryResponse();
            try
            {
                var category = await _unitOfWork.DecorCategoryRepository
                    .Query(x => x.Id == categoryId)
                    .FirstOrDefaultAsync();
                if (category == null)
                {
                    response.Success = false;
                    response.Message = "Decoration category not found";
                }
                else
                {
                    response.Data = _mapper.Map<DecorCategoryDTO>(category);
                    response.Success = true;
                    response.Message = "Decoration category retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving decoration category";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> CreateDecorCategoryAsync(DecorCategoryRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var existingCategory = await _unitOfWork.DecorCategoryRepository
                    .Query(x => x.CategoryName.ToLower() == request.CategoryName.ToLower())
                    .FirstOrDefaultAsync();
                if (existingCategory != null)
                {
                    response.Success = false;
                    response.Message = "Category name already exists";
                    response.Errors.Add("A category with this name already exists");
                    return response;
                }

                var category = _mapper.Map<DecorCategory>(request);
                await _unitOfWork.DecorCategoryRepository.InsertAsync(category);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Decoration category created successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error creating decoration category";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> UpdateDecorCategoryAsync(int categoryId, DecorCategoryRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var category = await _unitOfWork.DecorCategoryRepository
                    .Query(x => x.Id == categoryId)
                    .FirstOrDefaultAsync();
                if (category == null)
                {
                    response.Success = false;
                    response.Message = "Category not found";
                    response.Errors.Add("Category not found");
                    return response;
                }

                var existingCategory = await _unitOfWork.DecorCategoryRepository
                    .Query(x => x.CategoryName.ToLower() == request.CategoryName.ToLower() && x.Id != categoryId)
                    .FirstOrDefaultAsync();
                if (existingCategory != null)
                {
                    response.Success = false;
                    response.Message = "Category name already exists";
                    response.Errors.Add("A category with this name already exists");
                    return response;
                }

                category.CategoryName = request.CategoryName.Trim();
                category.Description = request.Description?.Trim();

                _unitOfWork.DecorCategoryRepository.Update(category);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Category updated successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating category";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> DeleteDecorCategoryAsync(int categoryId)
        {
            var response = new BaseResponse();
            try
            {
                var category = await _unitOfWork.DecorCategoryRepository
                    .Query(x => x.Id == categoryId)
                    .Include(x => x.DecorServices)
                    .FirstOrDefaultAsync();
                if (category == null)
                {
                    response.Success = false;
                    response.Message = "Category not found";
                    response.Errors.Add("Category not found");
                    return response;
                }

                if (category.DecorServices?.Any() == true)
                {
                    response.Success = false;
                    response.Message = "Cannot delete category with existing services";
                    response.Errors.Add("This category has associated services and cannot be deleted");
                    return response;
                }

                _unitOfWork.DecorCategoryRepository.Delete(category);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Category deleted successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error deleting category";
                response.Errors.Add(ex.Message);
            }
            return response;
        }
    }
}
