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
    public class AddressService : IAddressService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AddressService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // Lấy danh sách địa chỉ của user
        public async Task<BaseResponse> GetAddressesAsync(int userAccountId)
        {
            var response = new BaseResponse();
            try
            {
                var addresses = await _unitOfWork.AddressRepository
                    .Query(a => a.AccountId == userAccountId && a.IsDelete == false)
                    .ToListAsync();

                response.Success = true;
                response.Message = "Address list retrieved successfully.";
                response.Data = _mapper.Map<List<AddressResponse>>(addresses);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving address list.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // Tạo mới địa chỉ
        public async Task<BaseResponse> CreateAddressAsync(AddressRequest request, int userAccountId)
        {
            var response = new BaseResponse();
            try
            {
                // Nếu set mặc định => bỏ mặc định của các địa chỉ cũ
                if (request.IsDefault)
                {
                    var addresses = await _unitOfWork.AddressRepository
                        .Query(a => a.AccountId == userAccountId)
                        .ToListAsync();
                    foreach (var addr in addresses)
                    {
                        addr.IsDefault = false;
                        _unitOfWork.AddressRepository.Update(addr);
                    }
                }

                var newAddress = new Address
                {
                    AccountId = userAccountId, // Lấy từ token
                    FullName = request.FullName,
                    Phone = request.Phone,
                    Type = request.Type,
                    IsDefault = request.IsDefault,
                    Province = request.Province,
                    District = request.District,
                    Ward = request.Ward,
                    Street = request.Street,
                    Detail = request.Detail,
                    IsDelete = false
                };

                await _unitOfWork.AddressRepository.InsertAsync(newAddress);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Address created successfully.";
                response.Data = _mapper.Map<AddressResponse>(newAddress);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error creating address.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // Cập nhật địa chỉ (cần addressId + userAccountId)
        public async Task<BaseResponse> UpdateAddressAsync(int addressId, AddressRequest request, int userAccountId)
        {
            var response = new BaseResponse();
            try
            {
                var address = await _unitOfWork.AddressRepository.GetByIdAsync(addressId);
                if (address == null || address.AccountId != userAccountId)
                {
                    response.Success = false;
                    response.Message = "Address not found or you do not own this address.";
                    return response;
                }

                // Nếu cập nhật thành mặc định => bỏ mặc định các địa chỉ khác
                if (request.IsDefault)
                {
                    var addresses = await _unitOfWork.AddressRepository
                        .Query(a => a.AccountId == userAccountId)
                        .ToListAsync();
                    foreach (var addr in addresses)
                    {
                        addr.IsDefault = false;
                        _unitOfWork.AddressRepository.Update(addr);
                    }
                }

                address.FullName = request.FullName;
                address.Phone = request.Phone;
                address.Type = request.Type;
                address.IsDefault = request.IsDefault;
                address.Province = request.Province;
                address.District = request.District;
                address.Ward = request.Ward;
                address.Street = request.Street;
                address.Detail = request.Detail;
                address.IsDelete = false;

                _unitOfWork.AddressRepository.Update(address);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Address updated successfully.";
                response.Data = _mapper.Map<AddressResponse>(address);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating address.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // Xóa địa chỉ
        public async Task<BaseResponse> DeleteAddressAsync(int addressId, int userAccountId)
        {
            var response = new BaseResponse();
            try
            {
                var address = await _unitOfWork.AddressRepository.GetByIdAsync(addressId);
                if (address == null || address.AccountId != userAccountId)
                {
                    response.Success = false;
                    response.Message = "Address not found or you do not own this address.";
                    return response;
                }

                address.IsDelete = true;
                _unitOfWork.AddressRepository.Update(address);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Address deleted successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error deleting address.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // Đặt mặc định
        public async Task<BaseResponse> SetDefaultAddressAsync(int addressId, int userAccountId)
        {
            var response = new BaseResponse();
            try
            {
                var address = await _unitOfWork.AddressRepository.GetByIdAsync(addressId);
                if (address == null || address.AccountId != userAccountId)
                {
                    response.Success = false;
                    response.Message = "Address not found or you do not own this address.";
                    return response;
                }

                // Bỏ mặc định tất cả địa chỉ cũ
                var addresses = await _unitOfWork.AddressRepository
                    .Query(a => a.AccountId == userAccountId)
                    .ToListAsync();
                foreach (var addr in addresses)
                {
                    addr.IsDefault = false;
                    _unitOfWork.AddressRepository.Update(addr);
                }

                address.IsDefault = true;
                _unitOfWork.AddressRepository.Update(address);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Default address set successfully.";
                response.Data = _mapper.Map<AddressResponse>(address);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error setting default address.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }
    }
}
