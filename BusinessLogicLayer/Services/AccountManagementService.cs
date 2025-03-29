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
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse.Pagination;
using System.Linq.Expressions;
using AutoMapper;
using BusinessLogicLayer.ModelRequest.Cart;

namespace BusinessLogicLayer.Services
{
    public class AccountManagementService : IAccountManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PasswordHasher<Account> _passwordHasher;
        private readonly IMapper _mapper;
        private readonly ICartService _cartService;
        private readonly IWalletService _walletService;

        public AccountManagementService(IUnitOfWork unitOfWork, 
                                        IMapper mapper, 
                                        ICartService cartService, 
                                        IWalletService walletService)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = new PasswordHasher<Account>();
            _mapper = mapper;
            _cartService = cartService;
            _walletService = walletService;
        }

        public async Task<BaseResponse> GetAllAccountsAsync()
        {
            try
            {
                var accounts = await _unitOfWork.AccountRepository.GetAllAsync();

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
        public async Task<BaseResponse<PageResult<AccountDTO>>> GetFilterAccountsAsync(AccountFilterRequest request)
        {
            var response = new BaseResponse<PageResult<AccountDTO>>();
            try
            {
                // Filter
                Expression<Func<Account, bool>> filter = account =>
                    (!request.Gender.HasValue || account.Gender == request.Gender.Value) &&
                    //(string.IsNullOrEmpty(request.Status) || account.Status.Contains(request.Status)) &&
                    (!request.IsVerified.HasValue || account.IsVerified == request.IsVerified.Value) &&
                    (!request.IsDisable.HasValue || account.IsDisable == request.IsDisable.Value);

                // Sort
                Expression<Func<Account, object>> orderByExpression = request.SortBy switch
                {
                    "Email" => account => account.Email,
                    "FirstName" => account => account.FirstName,
                    "LastName" => account => account.LastName,
                    "Gender" => account => account.Gender,
                    _ => account => account.Id
                };

                // Get paginated data and filter
                (IEnumerable<Account> accounts, int totalCount) = await _unitOfWork.AccountRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending
                );

                var dtos = _mapper.Map<List<AccountDTO>>(accounts);

                var pageResult = new PageResult<AccountDTO>
                {
                    Data = dtos,
                    TotalCount = totalCount
                };

                response.Success = true;
                response.Message = "Accounts retrieved successfully";
                response.Data = pageResult;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving accounts";
                response.Errors = new List<string> { ex.Message };
            }
            return response;
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
                    IsVerified = true,
                    RoleId = 3, //Customer
                    Slug = GenerateDefaultSlug(),
                    IsDisable = false
                };

                account.Password = _passwordHasher.HashPassword(account, request.Password);
                await _unitOfWork.AccountRepository.InsertAsync(account);
                await _unitOfWork.CommitAsync();

                // Create a cart for the new account
                var cartResponse = await _cartService.CreateCartAsync(new CartRequest { AccountId = account.Id });
                if (!cartResponse.Success)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Account created but failed to create cart",
                        Errors = cartResponse.Errors
                    };
                }

                // Create a wallet for the new account
                var walletCreated = await _walletService.CreateWallet(account.Id);
                if (!walletCreated)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Message = "Account created but failed to create wallet",
                        Errors = new List<string> { "Wallet creation failed" }
                    };
                }

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

        public async Task<BaseResponse> ToggleAccountStatusAsync(int accountId)
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

                // Toggle trạng thái ban/unban
                account.IsDisable = !account.IsDisable;
                _unitOfWork.AccountRepository.Update(account);
                await _unitOfWork.CommitAsync();

                string statusMessage = account.IsDisable
                    ? "Account banned successfully"
                    : "Account unbanned successfully";

                return new BaseResponse
                {
                    Success = true,
                    Message = statusMessage
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Message = "Error toggling account status",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        #region
        private string GenerateDefaultSlug()
        {
            // Simple implementation: generate a 5-digit random number as a string
            var random = new Random();
            return random.Next(10000, 99999).ToString();
        }
        #endregion
    }
}
