using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Interfaces
{
    public interface IWalletService
    {
        Task<Boolean> CreateWallet(int accountId);
        Task<Boolean> UpdateWallet(int walletId, decimal amount);
    }
}
