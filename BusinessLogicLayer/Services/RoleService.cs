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
using Microsoft.EntityFrameworkCore;

namespace BusinessLogicLayer.Services
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RoleService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<RoleListResponse> GetAllRolesAsync()
        {
            try
            {
                var roles = await _unitOfWork.RoleRepository
                    .GetAllAsync();

                return new RoleListResponse
                {
                    Success = true,
                    Data = _mapper.Map<List<RoleDTO>>(roles)
                };
            }
            catch (Exception ex)
            {
                return new RoleListResponse
                {
                    Success = false,
                    Message = "Error retrieving roles",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<RoleResponse> GetRoleByIdAsync(int roleId)
        {
            try
            {
                var role = await _unitOfWork.RoleRepository.GetByIdAsync(roleId);

                if (role == null)
                {
                    return new RoleResponse
                    {
                        Success = false,
                        Message = "Role not found"
                    };
                }

                return new RoleResponse
                {
                    Success = true,
                    Data = _mapper.Map<RoleDTO>(role)
                };
            }
            catch (Exception ex)
            {
                return new RoleResponse
                {
                    Success = false,
                    Message = "Error retrieving role",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> CreateRoleAsync(CreateRoleRequest request)
        {
            try
            {
                var existingRole = await _unitOfWork.RoleRepository
                    .Query(x => x.RoleName.ToLower() == request.RoleName.ToLower())
                    .FirstOrDefaultAsync();

                if (existingRole != null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Role name already exists"
                    };
                }

                var role = new Role
                {
                    RoleName = request.RoleName
                };

                await _unitOfWork.RoleRepository.InsertAsync(role);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Role created successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error creating role",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> UpdateRoleAsync(int roleId, UpdateRoleRequest request)
        {
            try
            {
                var role = await _unitOfWork.RoleRepository.GetByIdAsync(roleId);

                if (role == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Role not found"
                    };
                }

                var existingRole = await _unitOfWork.RoleRepository
                    .Query(x => x.RoleName.ToLower() == request.RoleName.ToLower() && x.Id != roleId)
                    .FirstOrDefaultAsync();

                if (existingRole != null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Role name already exists"
                    };
                }

                role.RoleName = request.RoleName;
                _unitOfWork.RoleRepository.Update(role);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Role updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error updating role",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> DeleteRoleAsync(int roleId)
        {
            try
            {
                var role = await _unitOfWork.RoleRepository.GetByIdAsync(roleId);

                if (role == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Role not found"
                    };
                }

                // Hard delete vì không có IsDisable
                _unitOfWork.RoleRepository.Delete(role);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Role deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error deleting role",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
