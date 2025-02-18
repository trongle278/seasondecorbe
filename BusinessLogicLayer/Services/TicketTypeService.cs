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
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class TicketTypeService : ITicketTypeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TicketTypeService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // Get a single TicketType by id
        public async Task<BaseResponse> GetTicketTypeByIdAsync(int id)
        {
            var response = new BaseResponse();
            try
            {
                var ticketType = await _unitOfWork.TicketTypeRepository.GetByIdAsync(id);
                if (ticketType == null)
                {
                    response.Success = false;
                    response.Message = "Ticket type not found";
                    return response;
                }
                response.Success = true;
                response.Message = "Ticket type retrieved successfully";
                response.Data = _mapper.Map<TicketTypeResponse>(ticketType);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving ticket type";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // Get all TicketTypes
        public async Task<BaseResponse> GetAllTicketTypesAsync()
        {
            var response = new BaseResponse();
            try
            {
                var ticketTypes = await _unitOfWork.TicketTypeRepository.GetAllAsync();
                response.Success = true;
                response.Message = "Ticket types retrieved successfully";
                response.Data = _mapper.Map<List<TicketTypeResponse>>(ticketTypes);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving ticket types";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // Create a new TicketType
        public async Task<BaseResponse> CreateTicketTypeAsync(TicketTypeRequest request)
        {
            var response = new BaseResponse();
            try
            {
                if (request == null)
                {
                    response.Success = false;
                    response.Message = "Invalid ticket type request";
                    return response;
                }
                if (string.IsNullOrWhiteSpace(request.Type))
                {
                    response.Success = false;
                    response.Message = "Ticket type is required";
                    return response;
                }

                var ticketTypeEntity = _mapper.Map<TicketType>(request);
                await _unitOfWork.TicketTypeRepository.InsertAsync(ticketTypeEntity);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Ticket type created successfully";
                response.Data = _mapper.Map<TicketTypeResponse>(ticketTypeEntity);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error creating ticket type";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // Update an existing TicketType by id
        public async Task<BaseResponse> UpdateTicketTypeAsync(int id, TicketTypeRequest request)
        {
            var response = new BaseResponse();
            try
            {
                if (request == null)
                {
                    response.Success = false;
                    response.Message = "Invalid ticket type request";
                    return response;
                }
                if (string.IsNullOrWhiteSpace(request.Type))
                {
                    response.Success = false;
                    response.Message = "Ticket type is required";
                    return response;
                }

                var ticketTypeEntity = await _unitOfWork.TicketTypeRepository.GetByIdAsync(id);
                if (ticketTypeEntity == null)
                {
                    response.Success = false;
                    response.Message = "Ticket type not found";
                    return response;
                }

                _mapper.Map(request, ticketTypeEntity);
                _unitOfWork.TicketTypeRepository.Update(ticketTypeEntity);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Ticket type updated successfully";
                response.Data = _mapper.Map<TicketTypeResponse>(ticketTypeEntity);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating ticket type";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // Delete a TicketType by id
        public async Task<BaseResponse> DeleteTicketTypeAsync(int id)
        {
            var response = new BaseResponse();
            try
            {
                var ticketTypeEntity = await _unitOfWork.TicketTypeRepository.GetByIdAsync(id);
                if (ticketTypeEntity == null)
                {
                    response.Success = false;
                    response.Message = "Ticket type not found";
                    return response;
                }

                _unitOfWork.TicketTypeRepository.Delete(ticketTypeEntity.Id);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Ticket type deleted successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error deleting ticket type";
                response.Errors.Add(ex.Message);
            }
            return response;
        }
    }
}
