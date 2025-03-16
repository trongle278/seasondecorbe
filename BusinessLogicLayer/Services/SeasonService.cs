using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class SeasonService : ISeasonService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SeasonService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<IEnumerable<Season>>> GetAllSeasonsAsync()
        {
            try
            {
                var seasons = await _unitOfWork.SeasonRepository.GetAllAsync();
                return new BaseResponse<IEnumerable<Season>>
                {
                    Data = seasons,
                    Message = "Retrieving all season success.",
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<Season>>
                {
                    Success = false,
                    Message = "Error retrieving seasons.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse<Season>> GetSeasonByIdAsync(int id)
        {
            try
            {
                var season = await _unitOfWork.SeasonRepository.GetByIdAsync(id);
                if (season == null)
                {
                    return new BaseResponse<Season>
                    {
                        Success = false,
                        Message = "Season not found"
                    };
                }
                return new BaseResponse<Season>
                {
                    Data = season,
                    Message = "Retrieving season success.",
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<Season>
                {
                    Success = false,
                    Message = "Error retrieving season.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> CreateSeasonAsync(SeasonRequest request)
        {
            try
            {
                // Map the request to a Season entity
                var season = new Season
                {
                    SeasonName = request.SeasonName
                };

                await _unitOfWork.SeasonRepository.InsertAsync(season);
                await _unitOfWork.CommitAsync();
                return new BaseResponse
                {
                    Success = true,
                    Message = "Season created successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error creating season.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> UpdateSeasonAsync(int id, SeasonRequest request)
        {
            try
            {
                var existingSeason = await _unitOfWork.SeasonRepository.GetByIdAsync(id);
                if (existingSeason == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Season not found"
                    };
                }

                // Map incoming request to the existing entity
                existingSeason.SeasonName = request.SeasonName;
                _unitOfWork.SeasonRepository.Update(existingSeason);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Season updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error updating season.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> DeleteSeasonAsync(int id)
        {
            try
            {
                var season = await _unitOfWork.SeasonRepository.GetByIdAsync(id);
                if (season == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Season not found"
                    };
                }

                _unitOfWork.SeasonRepository.Delete(id);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Season deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error deleting season.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
