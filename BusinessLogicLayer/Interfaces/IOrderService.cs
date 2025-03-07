using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest.Order;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IOrderService
    {
        Task<BaseResponse> GetOrderList();
        Task<BaseResponse> GetOrderById(int id);
        Task<BaseResponse> CreateOrder(int cartId, int addressId);
        Task<BaseResponse> UpdateStatus(int id);
        Task<BaseResponse> CancelOrder(int id);
    }
}
