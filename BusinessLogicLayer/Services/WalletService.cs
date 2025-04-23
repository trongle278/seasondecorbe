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
using BusinessLogicLayer.ModelResponse.Payment;

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

        public async Task<BaseResponse<WalletResponse>> GetWalletByAccountId(int accountId)
        {
            var response = new BaseResponse<WalletResponse>();

            try
            {
                var wallet = await _unitOfWork.WalletRepository
                    .Queryable()
                    .FirstOrDefaultAsync(x => x.AccountId == accountId);

                if (wallet == null)
                {
                    response.Success = false;
                    response.Message = "Wallet not found.";
                    return response;
                }

                response.Success = true;
                response.Message = "Wallet retrieved successfully.";
                response.Data = new WalletResponse
                {
                    WalletId = wallet.Id,
                    Balance = wallet.Balance
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while retrieving the wallet.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }


        public async Task<BaseResponse<List<TransactionsResponse>>> GetAllTransactionsByAccountId(int accountId)
        {
            var response = new BaseResponse<List<TransactionsResponse>>();

            try
            {
                var wallet = await _unitOfWork.WalletRepository
                    .Queryable()
                    .FirstOrDefaultAsync(x => x.AccountId == accountId);

                if (wallet == null)
                {
                    response.Success = false;
                    response.Message = "Wallet not found.";
                    return response;
                }

                var transactions = await _unitOfWork.WalletTransactionRepository
                    .Queryable()
                    .Where(x => x.WalletId == wallet.Id)
                    .Include(x => x.PaymentTransaction)
                    .Select(x => new TransactionsResponse
                    {
                        Id = x.PaymentTransaction.Id,
                        BookingId = x.PaymentTransaction.BookingId,
                        OrderId = x.PaymentTransaction.OrderId,
                        Amount = (x.PaymentTransaction.TransactionType == PaymentTransaction.EnumTransactionType.TopUp ||
                                  x.PaymentTransaction.TransactionType == PaymentTransaction.EnumTransactionType.Refund ||
                                  x.PaymentTransaction.TransactionType == PaymentTransaction.EnumTransactionType.Revenue)
                                ? x.PaymentTransaction.Amount
                                : -x.PaymentTransaction.Amount,
                        TransactionDate = x.PaymentTransaction.TransactionDate,
                        TransactionType = x.PaymentTransaction.TransactionType.ToString(),
                        TransactionStatus = x.PaymentTransaction.TransactionStatus.ToString()
                    })
                    .ToListAsync();

                response.Success = true;
                response.Message = "Transactions retrieved successfully.";
                response.Data = transactions;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while retrieving transactions.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
