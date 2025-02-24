using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IAddressService
    {
        Task<BaseResponse> GetAddressesAsync(int userAccountId);
        Task<BaseResponse> CreateAddressAsync(AddressRequest request, int userAccountId);
        Task<BaseResponse> UpdateAddressAsync(int addressId, AddressRequest request, int userAccountId);
        Task<BaseResponse> DeleteAddressAsync(int addressId, int userAccountId);
        Task<BaseResponse> SetDefaultAddressAsync(int addressId, int userAccountId);
    }
}
