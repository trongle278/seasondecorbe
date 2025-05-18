using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelResponse;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class ScopeOfWorkService : IScopeOfWorkService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ScopeOfWorkService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponse> GetScopeOfWork()
        {
            var response = new BaseResponse();
            try
            {
                var scopeOfWork = await _unitOfWork.ScopeOfWorkRepository.GetAllAsync();

                response.Success = true;
                response.Message = "Scope of Work list retrieved successfully.";
                response.Data = _mapper.Map<List<ScopeOfWorkResponse>>(scopeOfWork);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Errror while getting Scope of Work list!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> GetScopeOfWorkById(int id)
        {
            var response = new BaseResponse();
            try
            {
                var scopeOfWork = await _unitOfWork.ScopeOfWorkRepository.GetByIdAsync(id);

                if (scopeOfWork == null)
                {
                    response.Message = "Scope of Work not found!";
                    return response;
                }

                response.Success = true;
                response.Message = "Scope of Work list retrieved successfully.";
                response.Data = _mapper.Map<ScopeOfWorkResponse>(scopeOfWork);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Errror while getting Scope of Work list!";
                response.Errors.Add(ex.Message);
            }

            return response ;
        }
    }
}
