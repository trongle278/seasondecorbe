using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IRoleService
    {
        Task<RoleListResponse> GetAllRolesAsync();
        Task<RoleResponse> GetRoleByIdAsync(int roleId);
        Task<BaseResponse> CreateRoleAsync(CreateRoleRequest request);
        Task<BaseResponse> UpdateRoleAsync(int roleId, UpdateRoleRequest request);
        Task<BaseResponse> DeleteRoleAsync(int roleId);
    }
}
