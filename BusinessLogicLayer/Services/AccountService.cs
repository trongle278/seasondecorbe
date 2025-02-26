﻿using System;
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

namespace BusinessLogicLayer.Services
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

                // Lấy thông tin Provider dựa trên AccountId
                var providerRecord = await _unitOfWork.ProviderRepository
                    .Query(p => p.AccountId == account.Id)
                    .FirstOrDefaultAsync();
                bool isProvider = providerRecord != null ? providerRecord.IsProvider : false;

                // Map account sang AccountDTO và gán thêm trường isProvider
                var accountDto = _mapper.Map<AccountDTO>(account);
                accountDto.IsProvider = isProvider;  // Đảm bảo rằng AccountDTO có thuộc tính IsProvider

                return new AccountResponse
                {
                    Success = true,
                    Data = accountDto
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
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    Phone = request.Phone,
                    RoleId = 3, //Customer
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

            // Cập nhật các trường cần thiết
            account.FirstName = request.FirstName;
            account.LastName = request.LastName;
            account.DateOfBirth = request.DateOfBirth;
            account.Gender = request.Gender;
            account.Phone = request.Phone;

            _unitOfWork.AccountRepository.Update(account);
            await _unitOfWork.CommitAsync();

            return new BaseResponse
            {
                Success = true,
                Message = "Account updated successfully"
            };
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
