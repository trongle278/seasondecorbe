using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelResponse;
using CloudinaryDotNet;
using DataAccessObject.Models;
using Repository.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public WalletService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<bool> CreateWallet(int accountId)
        {
            try
            {
                var wallet = new Wallet
                {
                    AccountId = accountId,
                    Balance = 0, // Wallet mới, số dư bằng 0
                };

                await _unitOfWork.WalletRepository.InsertAsync(wallet);
                await _unitOfWork.WalletRepository.SaveAsync();
                return true;
            }
            catch (Exception ex) {
                throw; ;
            }
            
        }

        public async Task<bool> UpdateWallet(int walletId, decimal amount)
        {
            try
            {
               var result = _unitOfWork.WalletRepository.Queryable().Where(x => x.Id == walletId).FirstOrDefault();
                if (result != null)
                {
                    result.Balance = amount;
                     _unitOfWork.WalletRepository.Update(result);
                    await _unitOfWork.WalletRepository.SaveAsync();
                    return true;
                }else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw; ;
            }
        }

        public async Task<BaseResponse<List<WalletTransactionResponse>>> GetWalletTransactions(int walletId)
        {
            var response = new BaseResponse<List<WalletTransactionResponse>>();
            try
            {
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(walletId);
                if (wallet == null)
                {
                    response.Success = false;
                    response.Message = "Wallet does not exist";
                    return response;
                }

                var transactions = _unitOfWork.WalletTransactionRepository.Queryable()
                    .Where(wt => wt.WalletId == walletId)
                    .Include(wt => wt.PaymentTransaction)
                    .OrderByDescending(wt => wt.PaymentTransaction.TransactionDate)
                    .ToList();

                var transactionsResponse = _mapper.Map<List<WalletTransactionResponse>>(transactions);
                response.Success = true;
                response.Message = "Transaction list retrieved successfully";
                response.Data = transactionsResponse;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while retrieving transaction list";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        public async Task<BaseResponse<List<WalletTransactionResponse>>> GetTransactionsByType(int walletId, PaymentTransaction.EnumTransactionType type)
        {
            var response = new BaseResponse<List<WalletTransactionResponse>>();
            try
            {
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(walletId);
                if (wallet == null)
                {
                    response.Success = false;
                    response.Message = "Wallet does not exist";
                    return response;
                }

                var transactions = _unitOfWork.WalletTransactionRepository.Queryable()
                    .Where(wt => wt.WalletId == walletId)
                    .Include(wt => wt.PaymentTransaction)
                    .Where(wt => wt.PaymentTransaction.TransactionType == type)
                    .OrderByDescending(wt => wt.PaymentTransaction.TransactionDate)
                    .ToList();

                var transactionsResponse = _mapper.Map<List<WalletTransactionResponse>>(transactions);
                response.Success = true;
                response.Message = $"Transaction list of type {type} retrieved successfully";
                response.Data = transactionsResponse;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while retrieving transaction list";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        public async Task<BaseResponse<List<WalletTransactionResponse>>> GetTransactionsByStatus(int walletId, PaymentTransaction.EnumTransactionStatus status)
        {
            var response = new BaseResponse<List<WalletTransactionResponse>>();
            try
            {
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(walletId);
                if (wallet == null)
                {
                    response.Success = false;
                    response.Message = "Wallet does not exist";
                    return response;
                }

                var transactions = _unitOfWork.WalletTransactionRepository.Queryable()
                    .Where(wt => wt.WalletId == walletId)
                    .Include(wt => wt.PaymentTransaction)
                    .Where(wt => wt.PaymentTransaction.TransactionStatus == status)
                    .OrderByDescending(wt => wt.PaymentTransaction.TransactionDate)
                    .ToList();

                var transactionsResponse = _mapper.Map<List<WalletTransactionResponse>>(transactions);
                response.Success = true;
                response.Message = $"Transaction list with status {status} retrieved successfully";
                response.Data = transactionsResponse;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while retrieving transaction list";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        public async Task<BaseResponse<List<WalletTransactionResponse>>> GetTransactionsByDateRange(int walletId, DateTime startDate, DateTime endDate)
        {
            var response = new BaseResponse<List<WalletTransactionResponse>>();
            try
            {
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(walletId);
                if (wallet == null)
                {
                    response.Success = false;
                    response.Message = "Wallet does not exist";
                    return response;
                }

                var transactions = _unitOfWork.WalletTransactionRepository.Queryable()
                    .Where(wt => wt.WalletId == walletId)
                    .Include(wt => wt.PaymentTransaction)
                    .Where(wt => wt.PaymentTransaction.TransactionDate >= startDate &&
                                wt.PaymentTransaction.TransactionDate <= endDate)
                    .OrderByDescending(wt => wt.PaymentTransaction.TransactionDate)
                    .ToList();

                var transactionsResponse = _mapper.Map<List<WalletTransactionResponse>>(transactions);
                response.Success = true;
                response.Message = "Transaction list by date range retrieved successfully";
                response.Data = transactionsResponse;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while retrieving transaction list";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        public async Task<BaseResponse<WalletTransactionResponse>> GetTransactionDetail(int transactionId)
        {
            var response = new BaseResponse<WalletTransactionResponse>();
            try
            {
                var transaction = _unitOfWork.WalletTransactionRepository.Queryable()
                    .Include(wt => wt.PaymentTransaction)
                    .Include(wt => wt.Wallet)
                    .Include(wt => wt.PaymentTransaction.Booking)
                    .Include(wt => wt.PaymentTransaction.Order)
                    .FirstOrDefault(wt => wt.Id == transactionId);

                if (transaction == null)
                {
                    response.Success = false;
                    response.Message = "Transaction not found";
                    return response;
                }

                var transactionResponse = _mapper.Map<WalletTransactionResponse>(transaction);
                response.Success = true;
                response.Message = "Transaction details retrieved successfully";
                response.Data = transactionResponse;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while retrieving transaction details";
                response.Errors.Add(ex.Message);
                return response;
            }
        }
    }
}
