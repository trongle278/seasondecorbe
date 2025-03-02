using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;
using DataAccessObject.Models;

namespace BusinessLogicLayer.Services
{
    public class AccountManagementService : IAccountManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PasswordHasher<Account> _passwordHasher;

        public AccountManagementService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = new PasswordHasher<Account>();
        }

        public async Task<BaseResponse> GetAllAccountsAsync()
        {
            try
            {
                var accounts = await _unitOfWork.AccountRepository
                    .Query(x => !x.IsDisable)
                    .ToListAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Accounts retrieved successfully",
                    Data = accounts
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error retrieving accounts",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        // Lấy thông tin một tài khoản theo id
        public async Task<BaseResponse> GetAccountByIdAsync(int accountId)
        {
            try
            {
                var account = await _unitOfWork.AccountRepository
                    .Query(x => x.Id == accountId && !x.IsDisable)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Account not found"
                    };
                }

                return new BaseResponse
                {
                    Success = true,
                    Message = "Account retrieved successfully",
                    Data = account
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error retrieving account",
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
            try
            {
                var account = await _unitOfWork.AccountRepository
                    .Query(x => x.Id == accountId && !x.IsDisable)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Account not found"
                    };
                }

                // Cập nhật các thuộc tính của tài khoản theo yêu cầu từ request, nhưng không cập nhật mật khẩu
                account.FirstName = request.FirstName;
                account.LastName = request.LastName;
                account.Phone = request.Phone;
                account.DateOfBirth = request.DateOfBirth;
                account.Gender = request.Gender;
                // Các thuộc tính khác cần cập nhật, nếu có, có thể được thêm vào đây

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
                    Message = "Error updating account",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponse> BanAccountAsync(int accountId)
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

                // Đánh dấu tài khoản bị ban
                account.IsDisable = true;
                _unitOfWork.AccountRepository.Update(account);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Account banned successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error banning account",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        // Nếu cần mở khóa tài khoản, bạn có thể thêm phương thức này:
        public async Task<BaseResponse> UnbanAccountAsync(int accountId)
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

                // Mở khóa tài khoản
                account.IsDisable = false;
                _unitOfWork.AccountRepository.Update(account);
                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Account unbanned successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error unbanning account",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
