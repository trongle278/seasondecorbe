using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using CloudinaryDotNet;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IWalletService _walletService;

        public PaymentService(IUnitOfWork unitOfWork, IMapper mapper, IWalletService walletService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _walletService = walletService;
        }

        // amount = giá booking * comission (0.1)  setiing 
        public async Task<bool> Deposit(int customerId, int adminId, decimal amount, int bookingId)
        {
            using (var transaction = await _unitOfWork.BeginTransactionAsync()) // Bắt đầu giao dịch
            {
                try
                {
                    // Kiểm tra số tiền hợp lệ
                    if (amount <= 0)
                    {
                        throw new Exception("Số tiền gửi phải lớn hơn 0.");
                    }
                    var comission = _unitOfWork.SettingRepository.Queryable().First().Commission;

                    var deposit = amount * comission;

                    // Lấy thông tin ví của khách hàng và Admin
                    var cusAccount = _unitOfWork.AccountRepository.Queryable()
                        .Include(x => x.Wallet)
                        .FirstOrDefault(x => x.Id == customerId);

                    var adAccount = _unitOfWork.AccountRepository.Queryable()
                        .Include(x => x.Wallet)
                        .FirstOrDefault(x => x.Id == adminId);

                    if (cusAccount?.Wallet == null || adAccount?.Wallet == null)
                    {
                        throw new Exception("Ví khách hàng hoặc ví Admin không tồn tại.");
                    }

                    var cusWallet = cusAccount.Wallet;
                    var adWallet = adAccount.Wallet;

                    // Kiểm tra số dư khách hàng
                    if (cusWallet.Balance < deposit)
                    {
                        throw new Exception("Số dư không đủ.");
                    }

                    // Cập nhật số dư ví
                    await _walletService.UpdateWallet(cusWallet.Id, cusWallet.Balance - deposit);
                    await _walletService.UpdateWallet(adWallet.Id, adWallet.Balance + deposit);

                    // Tạo giao dịch
                    var newTransaction = new PaymentTransaction
                    {
                        Amount = deposit,
                        TransactionDate = DateTime.Now,
                        TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                        TransactionType = PaymentTransaction.EnumTransactionType.Revenue,
                        BookingId = bookingId
                    };

                    // Lưu giao dịch vào lịch sử của khách hàng và Admin
                    var newCusWalletTransaction = new WalletTransaction
                    {
                        PaymentTransaction = newTransaction,
                        WalletId = cusWallet.Id,
                    };

                    var newAdWalletTransaction = new WalletTransaction
                    {
                        PaymentTransaction = newTransaction,
                        WalletId = adWallet.Id,
                    };

                    await _unitOfWork.WalletTransactionRepository.InsertAsync(newCusWalletTransaction);
                    await _unitOfWork.WalletTransactionRepository.InsertAsync(newAdWalletTransaction);

                    // Commit giao dịch
                    await _unitOfWork.CommitAsync();
                    transaction.Commit();

                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        public async Task<bool> TopUp(int accountId, decimal amount)
        {
            using (var transaction = await _unitOfWork.BeginTransactionAsync()) // Bắt đầu giao dịch
            {
                try
                {
                    // Kiểm tra số tiền nạp hợp lệ
                    if (amount <= 0)
                    {
                        throw new Exception("Số tiền nạp phải lớn hơn 0.");
                    }

                    // Lấy thông tin ví
                    var account = _unitOfWork.AccountRepository.Queryable()
                        .Include(x => x.Wallet)
                        .FirstOrDefault(x => x.Id == accountId);

                    if (account?.Wallet == null)
                    {
                        throw new Exception("Ví của tài khoản không tồn tại.");
                    }

                    var wallet = account.Wallet;

                    amount = amount / 100;
                    // Cập nhật số dư ví
                    await _walletService.UpdateWallet(wallet.Id, wallet.Balance + amount);

                    // Tạo giao dịch nạp tiền
                    var newTransaction = new PaymentTransaction
                    {
                        Amount = amount,
                        TransactionDate = DateTime.Now,
                        TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                        TransactionType = PaymentTransaction.EnumTransactionType.TopUp,
                    };

                    // Tạo bản ghi trong lịch sử giao dịch của ví
                    var newWalletTransaction = new WalletTransaction
                    {
                        PaymentTransaction = newTransaction,
                        WalletId = wallet.Id,
                    };

                    await _unitOfWork.WalletTransactionRepository.InsertAsync(newWalletTransaction);

                    // Commit giao dịch
                    await _unitOfWork.CommitAsync();
                    transaction.Commit();

                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        public async Task<bool> Refund(int accountId, decimal amount, int bookingId, int adminId)
        {
            using (var transaction = await _unitOfWork.BeginTransactionAsync()) // Bắt đầu giao dịch
            {
                try
                {
                    // Lấy thông tin ví khách hàng và Admin
                    var cusWallet = _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefault(x => x.AccountId == accountId);
                    var adminWallet = _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefault(x => x.AccountId == adminId);

                    if (cusWallet == null || adminWallet == null)
                    {
                        throw new Exception("Ví khách hàng hoặc ví Admin không tồn tại.");
                    }

                    // Lấy số tiền đã giao dịch trước đó (nếu có)
                    var previousAmount = _unitOfWork.PaymentTractionRepository.Queryable()
                        .Where(x => x.BookingId == bookingId)
                        .Select(x => x.Amount)
                        .FirstOrDefault();

                    if (amount <= 0 || amount > previousAmount)
                    {
                        throw new Exception("Số tiền hoàn không hợp lệ.");
                    }

                    // Kiểm tra số dư Admin có đủ không
                    if (adminWallet.Balance < amount)
                    {
                        throw new Exception("Số dư ví Admin không đủ để hoàn tiền.");
                    }

                    // Cập nhật số dư ví
                    await _walletService.UpdateWallet(adminWallet.Id, adminWallet.Balance - amount);
                    await _walletService.UpdateWallet(cusWallet.Id, cusWallet.Balance + amount);

                    // Tạo giao dịch hoàn tiền
                    var newTransaction = new PaymentTransaction
                    {
                        Amount = amount,
                        TransactionDate = DateTime.Now,
                        TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                        TransactionType = PaymentTransaction.EnumTransactionType.Refund,
                        BookingId = bookingId,
                    };

                    // Lưu giao dịch vào lịch sử ví của khách hàng và Admin
                    var newWalletTransaction = new WalletTransaction
                    {
                        PaymentTransaction = newTransaction,
                        WalletId = cusWallet.Id,
                    };

                    var newAdminWalletTransaction = new WalletTransaction
                    {
                        PaymentTransaction = newTransaction,
                        WalletId = adminWallet.Id,
                    };

                    await _unitOfWork.WalletTransactionRepository.InsertAsync(newWalletTransaction);
                    await _unitOfWork.WalletTransactionRepository.InsertAsync(newAdminWalletTransaction);

                    // Commit giao dịch
                    await _unitOfWork.CommitAsync();
                    transaction.Commit();

                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        public async Task<bool> Pay(int accountId, decimal bookingAmount, int providerId, int bookingId)
        {
            using (var transaction = await _unitOfWork.BeginTransactionAsync()) // Bắt đầu giao dịch
            {
                try
                {
                    // Lấy thông tin ví khách hàng và nhà cung cấp
                    var cusWallet = _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefault(x => x.AccountId == accountId);
                    var providerWallet = _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefault(x => x.AccountId == providerId);

                    if (cusWallet == null || providerWallet == null)
                    {
                        throw new Exception("Ví khách hàng hoặc ví nhà cung cấp không tồn tại.");
                    }

                    // Lấy số tiền giao dịch trước đó (nếu có)
                    var amount = _unitOfWork.PaymentTractionRepository.Queryable()
                        .Where(x => x.BookingId == bookingId)
                        .Select(x => x.Amount)
                        .FirstOrDefault();

                    decimal finalAmount = bookingAmount - ((decimal?)amount ?? decimal.Zero);// Nếu `amount` là null, mặc định 0


                    if (finalAmount <= 0)
                    {
                        throw new Exception("Số tiền thanh toán không hợp lệ.");
                    }

                    // Kiểm tra số dư ví khách hàng có đủ không
                    if (cusWallet.Balance < finalAmount)
                    {
                        throw new Exception("Số dư ví khách hàng không đủ.");
                    }

                    // Cập nhật số dư ví
                    await _walletService.UpdateWallet(providerWallet.Id, providerWallet.Balance + finalAmount);
                    await _walletService.UpdateWallet(cusWallet.Id, cusWallet.Balance - finalAmount);

                    // Tạo giao dịch thanh toán
                    var newTransaction = new PaymentTransaction
                    {
                        Amount = finalAmount,
                        TransactionDate = DateTime.Now,
                        TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                        TransactionType = PaymentTransaction.EnumTransactionType.Pay,
                        BookingId = bookingId,
                    };

                    // Lưu giao dịch vào lịch sử ví của khách hàng và nhà cung cấp
                    var newWalletTransaction = new WalletTransaction
                    {
                        PaymentTransaction = newTransaction,
                        WalletId = cusWallet.Id,
                    };

                    var providerWalletTransaction = new WalletTransaction
                    {
                        PaymentTransaction = newTransaction,
                        WalletId = providerWallet.Id,
                    };

                    await _unitOfWork.WalletTransactionRepository.InsertAsync(newWalletTransaction);
                    await _unitOfWork.WalletTransactionRepository.InsertAsync(providerWalletTransaction);

                    // Commit giao dịch
                    await _unitOfWork.CommitAsync();
                    transaction.Commit();

                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

    }
}
