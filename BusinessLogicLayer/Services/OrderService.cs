using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Order;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Order;
using BusinessLogicLayer.ModelResponse.Pagination;
using CloudinaryDotNet;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class OrderService : IOrderService
    {
        private readonly IPaymentService _paymentService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OrderService(IPaymentService paymentService, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _paymentService = paymentService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponse> GetOrderList(int accountId)
        {
            var response = new BaseResponse();
            try
            {
                var order = await _unitOfWork.OrderRepository.Queryable()
                                        .Where(o => o.AccountId == accountId)
                                        .ToListAsync();
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

        public async Task<BaseResponse<PageResult<OrderResponse>>> GetPaginateListForCustomer(OrderFilterRequest request, int accountId)
        {
            var response = new BaseResponse<PageResult<OrderResponse>>();
            try
            {
                // Filter
                Expression<Func<Order, bool>> filter = order =>
                    order.AccountId == accountId &&
                    (string.IsNullOrEmpty(request.OrderCode) || order.OrderCode.Contains(request.OrderCode)) &&
                    (!request.Status.HasValue || order.Status == request.Status);

                // Sort
                Expression<Func<Order, object>> orderByExpression = request.SortBy switch
                {
                    "OrderCode" => order => order.OrderCode,
                    "OrderDate" => order => order.OrderDate,
                    _ => order => order.Id
                };

                Func<IQueryable<Order>, IQueryable<Order>> customQuery = query =>
                query.Include(o => o.OrderDetails);

                // Get paginated data and filter
                (IEnumerable<Order> orders, int totalCount) = await _unitOfWork.OrderRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    null,
                    customQuery
                );

                var order = _mapper.Map<List<OrderResponse>>(orders);

                var pageResult = new PageResult<OrderResponse>
                {
                    Data = order,
                    TotalCount = totalCount
                };

                response.Success = true;
                response.Message = "Order list retrieved successfully";
                response.Data = pageResult;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving order list";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<PageResult<OrderResponse>>> GetPaginateListForProvider(OrderFilterRequest request, int accountId)
        {
            var response = new BaseResponse<PageResult<OrderResponse>>();
            try
            {
                // Filter
                Expression<Func<Order, bool>> filter = order =>
                    order.OrderDetails.Any(od => od.Product.AccountId == accountId) &&
                    (string.IsNullOrEmpty(request.OrderCode) || order.OrderCode.Contains(request.OrderCode)) &&
                    (!request.Status.HasValue || order.Status == request.Status);

                // Sort
                Expression<Func<Order, object>> orderByExpression = request.SortBy switch
                {
                    "OrderCode" => order => order.OrderCode,
                    "OrderDate" => order => order.OrderDate,
                    _ => order => order.Id
                };

                Func<IQueryable<Order>, IQueryable<Order>> customQuery = query =>
                query.Include(o => o.OrderDetails);

                // Get paginated data
                (IEnumerable<Order> orders, int totalCount) = await _unitOfWork.OrderRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    null,
                    customQuery
                );

                var orderResponses = _mapper.Map<List<OrderResponse>>(orders);

                var pageResult = new PageResult<OrderResponse>
                {
                    Data = orderResponses,
                    TotalCount = totalCount
                };

                response.Success = true;
                response.Message = "Order list for provider retrieved successfully";
                response.Data = pageResult;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving order list for provider";
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
                                            .Include(o => o.OrderDetails)
                                            .Include(o => o.Address)
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
                                                .Query(a => a.Id == addressId && a.IsDelete == false)
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
                    OrderCode = GenerateOrderCode(),
                    AccountId = cart.Account.Id,
                    AddressId = address.Id,
                    Phone = address.Phone,
                    FullName = address.FullName,
                    PaymentMethod = "Wallet Transaction",
                    OrderDate = DateTime.Now.ToLocalTime(),
                    TotalPrice = orderItems.Sum(item => item.UnitPrice),
                    Status = Order.OrderStatus.Pending,
                    OrderDetails = orderItems.Select(item => new OrderDetail
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

                var orderResponse = _mapper.Map<OrderResponse>(order);
                orderResponse.Address = _mapper.Map<OrderAddressResponse>(address);

                response.Success = true;
                response.Message = "Order created successfully.";
                response.Data = orderResponse;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error creating order";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        //public async Task<BaseResponse> UpdateStatus(int id)
        //{
        //    var response = new BaseResponse();
        //    try
        //    {
        //        var order = await _unitOfWork.OrderRepository.GetByIdAsync(id);

        //        if (order == null || order.Status == Order.OrderStatus.Cancelled)
        //        {
        //            response.Success = false;
        //            response.Message = "Invalid order";
        //            return response;
        //        }

        //        switch (order.Status)
        //        {
        //            case Order.OrderStatus.OrderPayment:
        //                order.Status = Order.OrderStatus.Shipping;
        //                _unitOfWork.OrderRepository.Update(order);
        //                break;

        //            case Order.OrderStatus.Shipping:
        //                order.Status = Order.OrderStatus.Completed;
        //                _unitOfWork.OrderRepository.Update(order);
        //                break;
        //        }
        //        await _unitOfWork.CommitAsync();

        //        response.Success = true;
        //        response.Message = "Order updated succesfully.";
        //        response.Data = _mapper.Map<OrderResponse>(order);
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Success = false;
        //        response.Message = "Error updating status";
        //        response.Errors.Add(ex.Message);
        //    }

        //    return response;
        //}

        public async Task<BaseResponse> CancelOrder(int id)
        {
            var response = new BaseResponse();
            try
            {
                var order = await _unitOfWork.OrderRepository
                                            .Query(o => o.Id == id)
                                            .Include(o => o.OrderDetails)
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
                foreach (var productOrder in order.OrderDetails)
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

        public async Task<BaseResponse> ProcessPayment(int id)
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

                if (order.Status != Order.OrderStatus.Pending)
                {
                    response.Success = false;
                    response.Message = "Invalid status";
                    return response;
                }

                var amount = order.TotalPrice;

                if (amount <= 0)
                {
                    response.Success = false;
                    response.Message = "No remaining amount to be paid";
                    return response;
                }

                var orderProducts = await _unitOfWork.OrderDetailRepository.Queryable()
                                        .Where(po => po.OrderId == id)
                                        .ToListAsync();

                var commissionRate = await _unitOfWork.SettingRepository.Queryable()
                                            .Select(s => s.Commission)
                                            .FirstOrDefaultAsync();

                foreach (var orderProduct in orderProducts)
                {
                    var providerId = await _unitOfWork.ProductRepository.Queryable()
                                            .Where(p => p.Id == orderProduct.ProductId)
                                            .Select(p => p.AccountId)
                                            .FirstOrDefaultAsync();

                    if (providerId == 0)
                    {
                        response.Success = false;
                        response.Message = "Invalid provider";
                        return response;
                    }

                    var unitPrice = orderProduct.UnitPrice ?? 0;

                    if (unitPrice <= 0)
                    {
                        response.Success = false;
                        response.Message = $"Invalid price for product {orderProduct.ProductName}";
                        return response;
                    }

                    bool paymentSuccess = await _paymentService.OrderPay(
                        order.AccountId, providerId, order.Id, unitPrice, commissionRate);

                    if (!paymentSuccess)
                    {
                        response.Success = false;
                        response.Message = $"Failed to process payment for product {orderProduct.ProductName}";
                        return response;
                    }
                }

                order.Status = Order.OrderStatus.Paid;
                _unitOfWork.OrderRepository.Update(order);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Process payment successfully";
                response.Data = order;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error process payment";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        #region
        private string GenerateOrderCode()
        {
            return "ORD" + DateTime.Now.Ticks;
        }
        #endregion
    }
}
