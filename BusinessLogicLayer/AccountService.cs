using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Repository.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using DataAccessObject.Models;
using AutoMapper;
using Microsoft.AspNetCore.Identity;

namespace BusinessLogicLayer
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly PasswordHasher<Account> _passwordHasher;

        public AccountService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _passwordHasher = new PasswordHasher<Account>();
        }

        public async Task<AccountResponse> GetAccountByIdAsync(int accountId)
        {
            try
            {
                var account = await _unitOfWork.AccountRepository
                    .Query(x => x.Id == accountId)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return new AccountResponse
                    {
                        Success = false,
                        Message = "Account not found"
                    };
                }

                return new AccountResponse
                {
                    Success = true,
                    Data = _mapper.Map<AccountDTO>(account)  // Sử dụng AutoMapper
                };
            }
            catch (Exception ex)
            {
                return new AccountResponse
                {
                    Success = false,
                    Message = "Error retrieving account",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<AccountListResponse> GetAllAccountsAsync()
        {
            try
            {
                var accounts = await _unitOfWork.AccountRepository
                    .Query(x => !x.IsDisable)
                    .ToListAsync();

                return new AccountListResponse
                {
                    Success = true,
                    Data = _mapper.Map<List<AccountDTO>>(accounts)  // Sử dụng AutoMapper
                };
            }
            catch (Exception ex)
            {
                return new AccountListResponse
                {
                    Success = false,
                    Message = "Error retrieving accounts",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> CreateAccountAsync(CreateAccountRequest request)
        {
            try
            {
                // Check if email already exists
                var existingAccount = await _unitOfWork.AccountRepository
                    .Query(x => x.Email == request.Email)
                    .FirstOrDefaultAsync();

                if (existingAccount != null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Email already exists",
                        Errors = new List<string> { "Email already exists" }
                    };
                }

                var account = new Account
                {
                    Email = request.Email,
                    Password = request.Password, // Consider hashing password
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    DateOfBirth =request.DateOfBirth,
                    Gender = request.Gender,
                    Phone = request.Phone,
                    Address = request.Address,
                    Avatar = request.Avatar,
                    RoleId = 2, //Customer
                    IsDisable = false
                };

                account.Password = _passwordHasher.HashPassword(account, request.Password);
                await _unitOfWork.AccountRepository.InsertAsync(account);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Account created successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error creating account",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> UpdateAccountAsync(int accountId, UpdateAccountRequest request)
        {
            try
            {
                var account = await _unitOfWork.AccountRepository
                    .Query(x => x.Id == accountId)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Account not found",
                        Errors = new List<string> { "Account not found" }
                    };
                }

                // Update properties
                account.FirstName = request.FirstName;
                account.LastName = request.LastName;
                account.DateOfBirth = request.DateOfBirth;
                account.Gender = request.Gender;
                account.Phone = request.Phone;
                account.Address = request.Address;
                account.Avatar = request.Avatar;

                _unitOfWork.AccountRepository.Update(account);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Account updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "An error occurred while updating account",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> DeleteAccountAsync(int accountId)
        {
            try
            {
                var account = await _unitOfWork.AccountRepository
                    .Query(x => x.Id == accountId)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Account not found"
                    };
                }

                // Soft delete
                account.IsDisable = true;
                _unitOfWork.AccountRepository.Update(account);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Account deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error deleting account",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
