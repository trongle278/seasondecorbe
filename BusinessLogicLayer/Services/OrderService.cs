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
                response.Message = "Order list retrieved successfully.";
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
                response.Message = "Order retrieved successfully.";
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

        public async Task<BaseResponse> CreateOrder(int cartId, int addressId)
        {
            var response = new BaseResponse();
            try
            {
                var cart = await _unitOfWork.CartRepository
                                            .Query(c => c.Id == cartId)
                                            .Include(c => c.CartItems)
                                                .ThenInclude(ci => ci.Product)
                                                    .ThenInclude(p => p.ProductImages)
                                            .Include(c => c.Account)
                                            .FirstOrDefaultAsync();

                if (cart == null)
                {
                    response.Success = false;
                    response.Message = "Invalid cart";
                    return response;
                }

                var address = await _unitOfWork.AddressRepository
                                                .Query(a => a.Id == addressId)
                                                .FirstOrDefaultAsync();

                if (address == null)
                {
                    response.Success = false;
                    response.Message = "Invalid address";
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
                    AddressId = address.Id,
                    Phone = address.Phone,
                    FullName = address.FullName,
                    PaymentMethod = "Online Banking",
                    OrderDate = DateTime.Now.ToLocalTime(),
                    TotalPrice = orderItems.Sum(item => item.UnitPrice),
                    Status = Order.OrderStatus.Pending,
                    ProductOrders = orderItems.Select(item => new ProductOrder
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Image = item.Image,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    }).ToList()
                };

                await _unitOfWork.OrderRepository.InsertAsync(order);

                // Update Product Quantity
                foreach (var item in orderItems)
                {
                    var product = item.Product;
                    if (product != null)
                    {
                        product.Quantity -= item.Quantity;
                        _unitOfWork.ProductRepository.Update(product);
                    }
                }

                // Remove products from cart
                _unitOfWork.CartItemRepository.RemoveRange(cart.CartItems);

                // Clear totalItem in Cart
                cart.TotalItem = 0;
                cart.TotalPrice = 0;
                _unitOfWork.CartRepository.Update(cart);

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Order created successfully.";
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

                if (order == null || order.Status == Order.OrderStatus.Cancelled)
                {
                    response.Success = false;
                    response.Message = "Invalid order";
                    return response;
                }

                switch (order.Status)
                {
                    case Order.OrderStatus.Pending:
                        order.Status = Order.OrderStatus.Processing;
                        _unitOfWork.OrderRepository.Update(order);
                        break;

                    case Order.OrderStatus.Processing:
                        order.Status = Order.OrderStatus.Shipping;
                        _unitOfWork.OrderRepository.Update(order);
                        break;

                    case Order.OrderStatus.Shipping:
                        order.Status = Order.OrderStatus.Completed;
                        _unitOfWork.OrderRepository.Update(order);
                        break;
                }
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Order updated succesfully.";
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

                if (order.Status != Order.OrderStatus.Pending)
                {
                    response.Success = false;
                    response.Message = "Invalid status";
                    return response;
                }

                order.Status = Order.OrderStatus.Cancelled;

                _unitOfWork.OrderRepository.Update(order);

                // Update Product quantity
                foreach (var productOrder in order.ProductOrders)
                {
                    var product = await _unitOfWork.ProductRepository.GetByIdAsync(productOrder.ProductId);
                    if (product != null)
                    {
                        product.Quantity += productOrder.Quantity;
                        _unitOfWork.ProductRepository.Update(product);
                    }
                }

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Order cancelled successfully.";
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
