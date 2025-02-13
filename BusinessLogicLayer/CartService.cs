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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;

namespace BusinessLogicLayer
{
    public class CartService: ICartService
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
                // Log detailed error information
                Console.WriteLine("Error: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                }

                return new BaseResponse
                {
                    Success = false,
                    Message = "Error creating cart.",
                    Errors = new List<string> { ex.Message, ex.InnerException?.Message }
                };
            }
        }
    }
}
