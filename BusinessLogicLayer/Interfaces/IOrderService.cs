﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest.Order;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Order;
using BusinessLogicLayer.ModelResponse.Pagination;

namespace BusinessLogicLayer.Interfaces
{
    public interface IOrderService
    {
        Task<BaseResponse> GetOrderList(int accountId);
        Task<BaseResponse<PageResult<OrderResponse>>> GetPaginateListForCustomer(OrderFilterRequest request, int accountId);
        Task<BaseResponse<PageResult<OrderResponse>>> GetPaginateListForProvider(OrderFilterRequest request, int accountId);
        Task<BaseResponse> GetOrderById(int id);
        Task<BaseResponse> CreateOrder(int cartId, int addressId);
        //Task<BaseResponse> UpdateStatus(int id);
        Task<BaseResponse> CancelOrder(int id);
        Task<BaseResponse> ProcessPayment(int id);
    }
}
