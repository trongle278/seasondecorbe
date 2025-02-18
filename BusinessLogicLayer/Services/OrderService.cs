using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Order;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Order;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponse> GetOrderList()
        {
            var response = new BaseResponse();
            try
            {
                var order = await _unitOfWork.OrderRepository.GetAllAsync();
                response.Success = true;
                response.Message = "Order list retrieved successfully";
                response.Data = _mapper.Map<List<OrderResponse>>(order);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving order list";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> GetOrderById(int id)
        {
            var response = new BaseResponse();
            try
            {
                var order = await _unitOfWork.OrderRepository
                                            .Query(o => o.Id == id)
                                            .Include(o => o.ProductOrders)
                                            .FirstOrDefaultAsync();

                if (order == null)
                {
                    response.Success = false;
                    response.Message = "Invalid order";
                    return response;
                }

                response.Success = true;
                response.Message = "Order retrieved successfully";
                response.Data = _mapper.Map<OrderResponse>(order);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving order";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> CreateOrder(int cartId, OrderRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var cart = await _unitOfWork.CartRepository
                                            .Query(c => c.Id == cartId)
                                            .Include(c => c.CartItems)
                                                .ThenInclude(ci => ci.Product)
                                            .Include(c => c.Account)
                                            .FirstOrDefaultAsync();

                if (cart == null)
                {
                    response.Success = false;
                    response.Message = "Invalid cart";
                    return response;
                }

                // Check available product quantity
                foreach (var item in cart.CartItems)
                {
                    var product = item.Product;

                    if (product == null || product.Quantity < item.Quantity)
                    {
                        response.Success = false;
                        response.Message = "Invalid item";
                        return response;
                    }
                }

                var orderItems = cart.CartItems.ToList();

                var order = new Order
                {
                    AccountId = cart.Account.Id,
                    DeliverAddress = request.DeliverAddress,
                    Phone = request.Phone,
                    FullName = request.FullName,
                    PaymentMethod = "Online Banking",
                    OrderDate = DateTime.Now,
                    TotalPrice = orderItems.Sum(item => item.UnitPrice),
                    OrderStatus = Order.Status.Pending,
                    ProductOrders = orderItems.Select(item => new ProductOrder
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                    }).ToList()
                };

                _unitOfWork.CartItemRepository.Delete(cart.CartItems);
                await _unitOfWork.CommitAsync();

                await _unitOfWork.OrderRepository.InsertAsync(order);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Order created successfully";
                response.Data = _mapper.Map<OrderResponse>(order);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error creating order";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> UpdateStatus(int id)
        {
            var response = new BaseResponse();
            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(id);

                if (order == null || order.OrderStatus == Order.Status.Cancelled)
                {
                    response.Success = false;
                    response.Message = "Invalid order";
                    return response;
                }

                switch (order.OrderStatus)
                {
                    case Order.Status.Pending:
                        order.OrderStatus = Order.Status.Processing;
                        _unitOfWork.OrderRepository.Update(order);
                        break;

                    case Order.Status.Processing:
                        order.OrderStatus = Order.Status.Shipping;
                        _unitOfWork.OrderRepository.Update(order);
                        break;

                    case Order.Status.Shipping:
                        order.OrderStatus = Order.Status.Completed;
                        _unitOfWork.OrderRepository.Update(order);
                        break;
                }
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Order updated succesfully";
                response.Data = _mapper.Map<OrderResponse>(order);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating status";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> CancelOrder(int id)
        {
            var response = new BaseResponse();
            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(id);

                if (order == null)
                {
                    response.Success = false;
                    response.Message = "Invalid order";
                    return response;
                }

                if (order.OrderStatus != Order.Status.Pending)
                {
                    response.Success = false;
                    response.Message = "Invalid status";
                    return response;
                }

                order.OrderStatus = Order.Status.Cancelled;

                _unitOfWork.OrderRepository.Update(order);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Order cancelled successfully";
                response.Data = _mapper.Map<OrderResponse>(order);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error cancel order";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
