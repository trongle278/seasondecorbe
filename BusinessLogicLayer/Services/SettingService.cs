using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Azure;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Order;
using BusinessLogicLayer.ModelResponse.Pagination;
using BusinessLogicLayer.ModelResponse.Product;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class SettingService : ISettingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SettingService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<BaseResponse<PageResult<SettingResponse>>> GetSettingAsync(SettingFilterRequest request)
        {
            var response = new BaseResponse<PageResult<SettingResponse>>();
            try
            {
                // Filter
                Expression<Func<Setting, bool>> filter = setting =>
                    !request.Commission.HasValue || setting.Commission == request.Commission;

                // Sort
                Expression<Func<Setting, object>> orderByExpression = request.SortBy?.ToLower() switch
                {
                    "commission" => setting => setting.Commission,
                    _ => setting => setting.Id
                };

                // Get paginated data and filter
                (IEnumerable<Setting> settings, int totalCount) = await _unitOfWork.SettingRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending
                );

                var setting = _mapper.Map<List<SettingResponse>>(settings);

                var pageResult = new PageResult<SettingResponse>
                {
                    Data = setting,
                    TotalCount = totalCount
                };

                response.Success = true;
                response.Message = "Setting list retrieved successfully";
                response.Data = pageResult;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving setting list";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> GetSettingByIdAsync(int id)
        {
            var response = new BaseResponse();
            try
            {
                var setting = await _unitOfWork.SettingRepository.Queryable()
                                                .Where(s => s.Id == id)
                                                .FirstOrDefaultAsync();

                if (setting == null)
                {
                    response.Message = "Setting not found!";
                    return response;
                }

                response.Success = true;
                response.Message = "Setting retrieved successfully.";
                response.Data = _mapper.Map<SettingResponse>(setting);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving setting";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> UpdateSettingAsync(int id, SettingRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var setting = await _unitOfWork.SettingRepository.Queryable()
                                                .Where(s => s.Id == id)
                                                .FirstOrDefaultAsync();

                if (setting == null)
                {
                    response.Message = "Setting not found!";
                    return response;
                }

                if (request == null)
                {
                    response.Message = "Commission has to have value";
                    return response;
                }

                if (request.Commission <= 0)
                {
                    response.Message = "Commission value has to be > 0";
                    return response;
                }

                setting.Commission = request.Commission;

                _unitOfWork.SettingRepository.Update(setting);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Setting updated successfully";
                response.Data = _mapper.Map<SettingResponse>(setting);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating setting";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
