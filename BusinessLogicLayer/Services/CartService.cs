using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Cart;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Cart;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CartService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponse> CreateCartAsync(CartRequest request)
        {
            try
            {
                // Check if the account already has a cart
                var existingCart = await _unitOfWork.CartRepository
                    .Query(c => c.AccountId == request.AccountId)
                    .FirstOrDefaultAsync();

                if (existingCart != null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Cart already exists for this account."
                    };
                }

                // Create a new cart
                var newCart = new Cart
                {
                    AccountId = request.AccountId,
                    TotalItem = 0,
                    TotalPrice = 0,
                    VoucherId = null // or set a default voucher if needed
                };

                await _unitOfWork.CartRepository.InsertAsync(newCart);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Cart created successfully."
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error creating cart.",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> GetCart(int accountId)
        {
            var response = new BaseResponse();
            try
            {
                var cart = await _unitOfWork.CartRepository
                    .Query(c => c.AccountId == accountId)
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync();

                if (cart == null)
                {
                    response.Success = false;
                    response.Message = "Invalid cart";
                    return response;
                }
                response.Success = true;
                response.Message = "Cart retrieved successfully.";
                response.Data = _mapper.Map<Cart>(cart);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving cart";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> AddToCart(int accountId, int productId, int quantity)
        {
            var response = new BaseResponse();
            try
            {
                var cart = await _unitOfWork.CartRepository
                                            .Query(c => c.AccountId == accountId)
                                            .Include(c => c.CartItems)
                                                .ThenInclude(ci => ci.Product)
                                            .FirstOrDefaultAsync();

                if (cart == null)
                {
                    response.Success = false;
                    response.Message = "Invalid cart";
                    return response;
                }

                var product = await _unitOfWork.ProductRepository
                                                .Query(p => p.Id == productId)
                                                .Include(p => p.ProductImages)
                                                .FirstOrDefaultAsync();

                if (product == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product";
                    return response;
                }

                // Check existing product quantity
                if (product.Quantity < quantity)
                {
                    response.Success = false;
                    response.Message = "Not enough existing product";
                    return response;
                }

                if (product.Quantity < 0)
                {
                    response.Success = false;
                    response.Message = "Product quantity has to be > 0";
                    return response;
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

                double unitPrice = product.ProductPrice;

                // Add product to cart
                if (cartItem == null)
                {
                    cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = quantity * unitPrice,
                        ProductName = product.ProductName,
                        Image = product.ProductImages?.FirstOrDefault()?.ImageUrl
                    };

                    await _unitOfWork.CartItemRepository.InsertAsync(cartItem);
                }
                else
                {
                    if (product.Quantity < cartItem.Quantity + quantity)
                    {
                        response.Success = false;
                        response.Message = "Not enough existing product";
                        return response;
                    }

                    // Update cartItem
                    cartItem.Quantity += quantity;
                    cartItem.UnitPrice = cartItem.Quantity * unitPrice;
                    _unitOfWork.CartItemRepository.Update(cartItem);
                }

                // Update cart
                cart.TotalItem = cart.TotalItem - cart.TotalItem + cart.CartItems.Sum(ci => ci.Quantity);
                cart.TotalPrice = cart.TotalPrice - cart.TotalPrice + cart.CartItems.Sum(ci => ci.UnitPrice);

                _unitOfWork.CartRepository.Update(cart);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product added to cart successfully.";
                response.Data = _mapper.Map<CartResponse>(cart);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error adding product to cart";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> UpdateProductQuantity(int accountId, int productId, int quantity)
        {
            var response = new BaseResponse();
            try
            {
                var cart = await _unitOfWork.CartRepository.Query(c => c.AccountId == accountId).FirstOrDefaultAsync();

                if (cart == null)
                {
                    response.Success = false;
                    response.Message = "Invalid cart";
                    return response;
                }

                var cartItem = await _unitOfWork.CartItemRepository.Query(ci => ci.CartId == cart.Id && ci.ProductId == productId)
                                                            .FirstOrDefaultAsync();

                if (cartItem == null || quantity < 0)
                {
                    response.Success = false;
                    response.Message = "Invalid cartItem";
                    return response;
                }

                var product = await _unitOfWork.ProductRepository.GetByIdAsync(productId);

                if (product == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product";
                    return response;
                }

                // Check existing product before update cartItem
                if (product.Quantity < quantity)
                {
                    response.Success = false;
                    response.Message = "Not enough existing product";
                    return response;
                }

                if (product.Quantity < 0)
                {
                    response.Success = false;
                    response.Message = "Product quantity has to be > 0";
                    return response;
                }

                double unitPrice = product.ProductPrice;

                // Save old cartItem value before update
                int oldQuantity = cartItem.Quantity;
                double oldUnitPrice = cartItem.UnitPrice;

                // Update cartItem
                cartItem.Quantity = quantity;
                cartItem.UnitPrice = quantity * product.ProductPrice;

                // Update cart using old value
                cart.TotalItem += quantity - oldQuantity;
                cart.TotalPrice += (cartItem.UnitPrice - oldUnitPrice);

                _unitOfWork.CartItemRepository.Update(cartItem);

                _unitOfWork.CartRepository.Update(cart);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product in cart updated successfully.";
                response.Data = _mapper.Map<CartResponse>(cart);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating product in cart";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> RemoveProduct(int accountId, int productId)
        {
            var response = new BaseResponse();
            try
            {
                var cart = await _unitOfWork.CartRepository.Query(c => c.AccountId == accountId).FirstOrDefaultAsync();

                if (cart == null)
                {
                    response.Success = false;
                    response.Message = "Invalid cart";
                    return response;
                }

                var cartItem = await _unitOfWork.CartItemRepository.Query(ci => ci.CartId == cart.Id && ci.ProductId == productId)
                                                            .FirstOrDefaultAsync();

                if (cartItem == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product";
                    return response;
                }

                // Update cart
                cart.TotalItem -= cartItem.Quantity;
                cart.TotalPrice -= cartItem.UnitPrice;

                _unitOfWork.CartItemRepository.Delete(cartItem.Id);
                _unitOfWork.CartRepository.Update(cart);

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product in cart removed successfully.";
                response.Data = _mapper.Map<CartResponse>(cart);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error removing product in cart";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
