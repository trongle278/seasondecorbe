using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelResponse;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.X509;
using Repository.Interfaces;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class CancelTypeService : ICancelTypeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CancelTypeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<List<CancelTypeListResponse>>> GetAllCancelTypeAsync()
        {
            var response = new BaseResponse<List<CancelTypeListResponse>>();
            try
            {
                var cancelTypes = _unitOfWork.CancelTypeRepository.Queryable().ToList();

                response.Success = true;
                response.Data = cancelTypes.Select(ct => new CancelTypeListResponse
                {
                    Id = ct.Id,
                    Type = ct.Type
                }).ToList();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to retrieve cancel types.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
