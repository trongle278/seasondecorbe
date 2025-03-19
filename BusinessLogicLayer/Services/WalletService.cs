using AutoMapper;
using BusinessLogicLayer.Interfaces;
using CloudinaryDotNet;
using DataAccessObject.Models;
using Repository.UnitOfWork;
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
    }
}
